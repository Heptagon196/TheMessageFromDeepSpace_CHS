using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepSpaceChinese;

internal sealed class ImeBindingDiscoveryState
{
    private bool _initialized;
    private int _sceneHandle;

    internal bool TryBegin(int sceneHandle)
    {
        if (_initialized && _sceneHandle == sceneHandle)
            return false;
        _initialized = true;
        _sceneHandle = sceneHandle;
        return true;
    }
}

internal static class ImeCursorPositionEngine
{
    // Extracted from level0/sharedassets0.assets, mesh screen_right.001.
    // Rows are V=0..1 and columns are U=0..1 in 0.125 increments. X and Z are
    // exactly linear in UV; only the curved surface height needs this table.
    private static readonly float[] MonitorSurfaceHeights =
    {
        0.14555036f, 0.07411730f, 0.02581024f, -0.00222230f, -0.01141930f, -0.00222230f, 0.02581024f, 0.07411718f, 0.14555025f,
        0.10200775f, 0.03329706f, -0.01335084f, -0.04048026f, -0.04938972f, -0.04048026f, -0.01335084f, 0.03329706f, 0.10200763f,
        0.07195818f, 0.00501192f, -0.04055059f, -0.06708574f, -0.07580554f, -0.06708574f, -0.04055059f, 0.00501192f, 0.07195818f,
        0.05431533f, -0.01163554f, -0.05658197f, -0.08277881f, -0.09139049f, -0.08277881f, -0.05658197f, -0.01163554f, 0.05431533f,
        0.04849589f, -0.01713300f, -0.06187952f, -0.08796644f, -0.09654307f, -0.08796644f, -0.06187952f, -0.01713300f, 0.04849589f,
        0.05431533f, -0.01163554f, -0.05658197f, -0.08277881f, -0.09139049f, -0.08277881f, -0.05658197f, -0.01163554f, 0.05431533f,
        0.07195818f, 0.00501192f, -0.04055059f, -0.06708574f, -0.07580554f, -0.06708574f, -0.04055059f, 0.00501192f, 0.07195818f,
        0.10200775f, 0.03329706f, -0.01335084f, -0.04048026f, -0.04938972f, -0.04048026f, -0.01335084f, 0.03329706f, 0.10200763f,
        0.14555036f, 0.07411730f, 0.02581024f, -0.00222230f, -0.01141930f, -0.00222230f, 0.02581024f, 0.07411718f, 0.14555025f,
    };

    private static WorldSpaceClicker _cachedClicker;
    private static readonly List<RenderTextureBinding> SceneBindings = new();
    private static readonly ImeBindingDiscoveryState BindingDiscoveryState = new();

    private sealed class RenderTextureBinding
    {
        internal Camera SourceCamera;
        internal Camera OutputCamera;
        internal Renderer DisplayRenderer;
        internal Material Material;
        internal string TextureProperty;
        internal Vector2 ShaderTiling;
        internal Vector2 ShaderOffset;
        internal string MeshName;
        internal Vector3[] Vertices;
        internal Vector2[] Uv;
        internal int[] Triangles;
    }

    internal static Vector2 ToWindowsScreen(Vector2 unityScreen, int screenWidth,
        int screenHeight)
    {
        float maxX = Math.Max(0, screenWidth - 1);
        float maxY = Math.Max(0, screenHeight - 1);
        return new Vector2(
            Mathf.Clamp(unityScreen.x, 0f, maxX),
            Mathf.Clamp(screenHeight - unityScreen.y, 0f, maxY));
    }

    internal static Vector2 MapMonitorWorldToUnityScreen(Vector2 worldPoint,
        Vector2 xBounds, Vector2 yBounds, int screenWidth, int screenHeight)
    {
        return new Vector2(
            Mathf.InverseLerp(xBounds.x, xBounds.y, worldPoint.x) * screenWidth,
            Mathf.InverseLerp(yBounds.x, yBounds.y, worldPoint.y) * screenHeight);
    }

    internal static bool TryBarycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c,
        out Vector3 weights)
    {
        float denominator = (b.y - c.y) * (a.x - c.x) +
                            (c.x - b.x) * (a.y - c.y);
        if (Math.Abs(denominator) < 0.000001f)
        {
            weights = default;
            return false;
        }

        float first = ((b.y - c.y) * (point.x - c.x) +
                       (c.x - b.x) * (point.y - c.y)) / denominator;
        float second = ((c.y - a.y) * (point.x - c.x) +
                        (a.x - c.x) * (point.y - c.y)) / denominator;
        float third = 1f - first - second;
        weights = new Vector3(first, second, third);
        const float tolerance = -0.001f;
        return first >= tolerance && second >= tolerance && third >= tolerance;
    }

    internal static bool TryCalculate(TMP_InputField input, out Vector2 unityScreen,
        out Vector2 windowsScreen)
    {
        unityScreen = default;
        windowsScreen = default;
        if (input == null || !input.isActiveAndEnabled || !input.isFocused)
            return false;

        TMP_Text text = input.textComponent;
        if (text == null || text.rectTransform == null)
            return false;

        // Normal UGUI fields already position the IME correctly inside TMP_InputField.
        if (text is TextMeshProUGUI)
            return false;

        Vector2 localCaret = GetLocalCaretPosition(input, text);
        Vector3 worldCaret = text.rectTransform.TransformPoint(
            new Vector3(localCaret.x, localCaret.y, 0f));
        float firstLineLocalY = GetFirstLineLocalY(text, localCaret.y);
        Vector3 firstLineWorld = text.rectTransform.TransformPoint(
            new Vector3(localCaret.x, firstLineLocalY, 0f));

        bool renderTextureMapped = TryMapThroughRenderTexture(text, worldCaret,
            firstLineWorld, out unityScreen);
        if (!renderTextureMapped && !TryMapThroughWorldClicker(worldCaret, out unityScreen))
            return false;

        windowsScreen = ToWindowsScreen(unityScreen, Screen.width, Screen.height);
        return true;
    }

    private static Vector2 GetLocalCaretPosition(TMP_InputField input, TMP_Text text)
    {
        TMP_TextInfo textInfo = text.textInfo;
        int count = textInfo?.characterCount ?? 0;
        if (count > 0 && textInfo.characterInfo != null)
        {
            int caret = Mathf.Clamp(input.caretPosition, 0, count);
            if (caret < count)
            {
                TMP_CharacterInfo current = textInfo.characterInfo[caret];
                return new Vector2(current.origin, current.descender);
            }

            TMP_CharacterInfo previous = textInfo.characterInfo[count - 1];
            return new Vector2(previous.xAdvance, previous.descender);
        }

        // Empty fields do not always generate a character quad. Anchor the candidate
        // window at the lower-left of the real text viewport instead of screen (0, 0).
        Rect rect = text.rectTransform.rect;
        float inset = Math.Max(2f, text.fontSize * 0.08f);
        return new Vector2(rect.xMin + inset, rect.center.y - text.fontSize * 0.5f);
    }

    private static float GetFirstLineLocalY(TMP_Text text, float fallback)
    {
        TMP_TextInfo textInfo = text?.textInfo;
        if (textInfo == null || textInfo.lineInfo == null || textInfo.lineCount <= 0)
            return fallback;
        float descender = textInfo.lineInfo[0].descender;
        return IsFinite(descender) ? descender : fallback;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool TryMapThroughRenderTexture(TMP_Text text, Vector3 worldPoint,
        Vector3 firstLineWorldPoint, out Vector2 unityScreen)
    {
        unityScreen = default;
        int textLayer = text.gameObject.layer;
        EnsureSceneBindings();
        var failures = new List<string>();
        foreach (RenderTextureBinding binding in SceneBindings)
        {
            if (binding?.SourceCamera == null ||
                (binding.SourceCamera.cullingMask & (1 << textLayer)) == 0)
                continue;
            if (TryMapWithBinding(binding, textLayer, worldPoint, firstLineWorldPoint,
                    out unityScreen, out string failure))
                return true;
            failures.Add($"{binding.SourceCamera.targetTexture?.name ?? "<null>"}:{failure}");
        }
        return false;
    }

    private static void EnsureSceneBindings()
    {
        int sceneHandle = SceneManager.GetActiveScene().handle;
        if (!BindingDiscoveryState.TryBegin(sceneHandle))
            return;

        SceneBindings.Clear();
        Camera[] cameras = Camera.allCameras;
        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        var details = new List<string>();
        foreach (Camera sourceCamera in cameras)
        {
            if (sourceCamera == null || sourceCamera.targetTexture == null)
                continue;
            if (TryCreateSceneScreenBinding(sourceCamera, cameras, renderers,
                    allMaterials, out RenderTextureBinding binding, out string detail))
            {
                SceneBindings.Add(binding);
                details.Add(detail);
            }
            else if (sourceCamera.targetTexture.name == "RT IO Monitor" ||
                     sourceCamera.targetTexture.name == "RT Info Monitor")
            {
                details.Add(detail);
            }
        }
    }

    private static bool TryCreateSceneScreenBinding(Camera sourceCamera, Camera[] cameras,
        Renderer[] renderers, Material[] allMaterials, out RenderTextureBinding binding,
        out string detail)
    {
        binding = null;
        detail = "unsupported-target";
        string targetName = sourceCamera?.targetTexture?.name ?? string.Empty;
        string displayPath;
        string materialName;
        if (targetName == "RT IO Monitor")
        {
            displayPath = "Right Monitor/Screen";
            materialName = "M_RightScreen";
        }
        else if (targetName == "RT Info Monitor")
        {
            displayPath = "Left Monitor/Screen";
            materialName = "M_LeftScreen";
        }
        else
        {
            return false;
        }

        Renderer renderer = renderers.FirstOrDefault(candidate =>
            candidate != null && candidate.gameObject.scene.IsValid() &&
            ObjectPath(candidate.transform) == displayPath);
        if (renderer == null)
        {
            detail = $"target={targetName} renderer={displayPath}:missing";
            return false;
        }

        Material material = allMaterials.FirstOrDefault(candidate =>
            candidate != null && NormalizeMaterialName(candidate.name) == materialName &&
            TexturePropertyFor(candidate, sourceCamera.targetTexture) != null);
        if (material == null)
        {
            detail = $"target={targetName} renderer={displayPath} material={materialName}:missing";
            return false;
        }
        string property = TexturePropertyFor(material, sourceCamera.targetTexture);
        Vector4 shaderTilingValue = material.HasProperty("_tiling")
            ? material.GetVector("_tiling")
            : new Vector4(1f, 1f, 0f, 0f);
        Vector4 shaderOffsetValue = material.HasProperty("_offset")
            ? material.GetVector("_offset")
            : Vector4.zero;
        Camera outputCamera = cameras.FirstOrDefault(candidate =>
            candidate != null && candidate.targetTexture == null &&
            (candidate.cullingMask & (1 << renderer.gameObject.layer)) != 0);
        if (outputCamera == null)
        {
            detail = $"target={targetName} renderer={displayPath} output:missing";
            return false;
        }

        Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
        if (mesh == null)
        {
            detail = $"target={targetName} renderer={displayPath} mesh:missing";
            return false;
        }
        Vector3[] vertices;
        Vector2[] uv;
        int[] triangles;
        try
        {
            // Reading these Unity properties allocates arrays. Capture them once when
            // the scene binding is discovered; never allocate mesh data per frame.
            vertices = mesh.vertices;
            uv = mesh.uv;
            triangles = mesh.triangles;
        }
        catch (Exception ex)
        {
            detail = $"target={targetName} renderer={displayPath} mesh-read=" +
                     $"{ex.GetType().Name}:{ex.Message}";
            return false;
        }

        binding = new RenderTextureBinding
        {
            SourceCamera = sourceCamera,
            OutputCamera = outputCamera,
            DisplayRenderer = renderer,
            Material = material,
            TextureProperty = property,
            ShaderTiling = new Vector2(shaderTilingValue.x, shaderTilingValue.y),
            ShaderOffset = new Vector2(shaderOffsetValue.x, shaderOffsetValue.y),
            MeshName = mesh.name,
            Vertices = vertices,
            Uv = uv,
            Triangles = triangles,
        };
        detail = $"target={targetName} renderer={displayPath} material={materialName} " +
                 $"mesh={mesh.name} vertices={vertices.Length} uv={uv.Length} " +
                 $"triangles={triangles.Length} " +
                 $"shaderTiling=({shaderTilingValue.x:F3},{shaderTilingValue.y:F3}) " +
                 $"shaderOffset=({shaderOffsetValue.x:F3},{shaderOffsetValue.y:F3}) " +
                 $"output={outputCamera.name}";
        return true;
    }

    private static string TexturePropertyFor(Material material, Texture target)
    {
        if (material == null || target == null)
            return null;
        foreach (string property in material.GetTexturePropertyNames())
        {
            if (material.GetTexture(property) == target)
                return property;
        }
        return null;
    }

    private static string NormalizeMaterialName(string name)
    {
        const string instanceSuffix = " (Instance)";
        return name != null && name.EndsWith(instanceSuffix, StringComparison.Ordinal)
            ? name.Substring(0, name.Length - instanceSuffix.Length)
            : name ?? string.Empty;
    }

    private static bool TryMapWithBinding(RenderTextureBinding binding, int textLayer,
        Vector3 worldPoint, Vector3 firstLineWorldPoint, out Vector2 unityScreen,
        out string failure)
    {
        unityScreen = default;
        failure = "invalid-binding";
        if (binding?.SourceCamera == null || binding.OutputCamera == null ||
            binding.DisplayRenderer == null || binding.Material == null ||
            binding.SourceCamera.targetTexture == null ||
            binding.Material.GetTexture(binding.TextureProperty) !=
            binding.SourceCamera.targetTexture ||
            (binding.SourceCamera.cullingMask & (1 << textLayer)) == 0)
            return false;

        Vector3 viewport = binding.SourceCamera.WorldToViewportPoint(worldPoint);
        if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
            viewport.y < 0f || viewport.y > 1f)
        {
            failure = $"viewport=({viewport.x:F4},{viewport.y:F4},{viewport.z:F4})";
            return false;
        }
        Vector3 firstLineViewport =
            binding.SourceCamera.WorldToViewportPoint(firstLineWorldPoint);
        if (firstLineViewport.z <= 0f)
            firstLineViewport = viewport;

        Vector2 scale = binding.Material.GetTextureScale(binding.TextureProperty);
        Vector2 offset = binding.Material.GetTextureOffset(binding.TextureProperty);
        if (Math.Abs(scale.x) < 0.000001f || Math.Abs(scale.y) < 0.000001f)
        {
            failure = $"texture-transform scale=({scale.x:F4},{scale.y:F4}) " +
                      $"offset=({offset.x:F4},{offset.y:F4})";
            return false;
        }
        Vector2 textureUv = new Vector2(
            (viewport.x - offset.x) / scale.x,
            (viewport.y - offset.y) / scale.y);
        float firstLineTextureY =
            (firstLineViewport.y - offset.y) / scale.y;
        Vector2 transformedUv = ApplyMonitorImeUv(textureUv,
            firstLineTextureY, binding.ShaderTiling, binding.ShaderOffset);
        Vector2 meshUv = new Vector2(
            Mathf.Clamp01(transformedUv.x), Mathf.Clamp01(transformedUv.y));
        Vector3 displayWorld;
        try
        {
            if (!TryMapUvToWorld(binding, meshUv, out displayWorld))
            {
                failure = $"uv-miss uv=({meshUv.x:F4},{meshUv.y:F4}) " +
                          $"mesh={binding.MeshName ?? "<null>"}";
                return false;
            }
        }
        catch (Exception ex)
        {
            failure = $"uv-exception {ex.GetType().Name}: {ex.Message}";
            return false;
        }

        Vector3 screen = binding.OutputCamera.WorldToScreenPoint(displayWorld);
        if (screen.z <= 0f || !IsFinite(screen.x) || !IsFinite(screen.y) ||
            screen.x < 0f || screen.x > Screen.width ||
            screen.y < 0f || screen.y > Screen.height)
        {
            failure = $"screen=({screen.x:F2},{screen.y:F2},{screen.z:F4})";
            return false;
        }
        unityScreen = new Vector2(screen.x, screen.y);
        failure = "none";
        return true;
    }

    private static bool TryMapUvToWorld(RenderTextureBinding binding, Vector2 targetUv,
        out Vector3 worldPoint)
    {
        worldPoint = default;
        if (binding?.DisplayRenderer == null)
            return false;

        if (binding.MeshName == "screen_right.001" &&
            TryKnownMonitorSurface(targetUv, out Vector3 knownPoint))
        {
            worldPoint = binding.DisplayRenderer.transform.TransformPoint(knownPoint);
            return true;
        }

        Vector3[] vertices = binding.Vertices;
        Vector2[] uv = binding.Uv;
        int[] triangles = binding.Triangles;
        if (vertices == null || uv == null || triangles == null ||
            uv.Length != vertices.Length)
            return false;

        if (binding.MeshName == "screen_right.001" &&
            TryBilinearUvGrid(targetUv, uv, vertices, out Vector3 gridPoint))
        {
            worldPoint = binding.DisplayRenderer.transform.TransformPoint(gridPoint);
            return true;
        }

        for (int index = 0; index + 2 < triangles.Length; index += 3)
        {
            int a = triangles[index];
            int b = triangles[index + 1];
            int c = triangles[index + 2];
            if (a < 0 || b < 0 || c < 0 || a >= uv.Length || b >= uv.Length ||
                c >= uv.Length || !TryBarycentric(targetUv, uv[a], uv[b], uv[c],
                    out Vector3 weights))
                continue;
            Vector3 local = vertices[a] * weights.x + vertices[b] * weights.y +
                            vertices[c] * weights.z;
            worldPoint = binding.DisplayRenderer.transform.TransformPoint(local);
            return true;
        }
        return false;
    }

    internal static bool TryKnownMonitorSurface(Vector2 targetUv, out Vector3 localPoint)
    {
        localPoint = default;
        if (targetUv.x < 0f || targetUv.x > 1f ||
            targetUv.y < 0f || targetUv.y > 1f)
            return false;

        float gridX = targetUv.x * 8f;
        float gridY = targetUv.y * 8f;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(gridX), 0, 8);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(gridY), 0, 8);
        int x1 = Math.Min(x0 + 1, 8);
        int y1 = Math.Min(y0 + 1, 8);
        float tx = gridX - x0;
        float ty = gridY - y0;
        float bottom = Mathf.Lerp(MonitorSurfaceHeights[y0 * 9 + x0],
            MonitorSurfaceHeights[y0 * 9 + x1], tx);
        float top = Mathf.Lerp(MonitorSurfaceHeights[y1 * 9 + x0],
            MonitorSurfaceHeights[y1 * 9 + x1], tx);
        localPoint = new Vector3(
            1.25f - 2.5f * targetUv.x,
            Mathf.Lerp(bottom, top, ty),
            1.25f - 2.5f * targetUv.y);
        return true;
    }

    internal static Vector2 ApplyMonitorImeUv(Vector2 uv, float firstLineY,
        Vector2 tiling, Vector2 offset)
    {
        return new Vector2(uv.x * tiling.x + offset.x,
            firstLineY + (uv.y - firstLineY) * tiling.y);
    }

    internal static bool TryBilinearUvGrid(Vector2 targetUv, Vector2[] uv,
        Vector3[] vertices, out Vector3 localPoint)
    {
        localPoint = default;
        if (uv == null || vertices == null || uv.Length == 0 ||
            uv.Length != vertices.Length)
            return false;

        float lowerU = float.NegativeInfinity;
        float upperU = float.PositiveInfinity;
        float lowerV = float.NegativeInfinity;
        float upperV = float.PositiveInfinity;
        foreach (Vector2 coordinate in uv)
        {
            if (coordinate.x <= targetUv.x && coordinate.x > lowerU)
                lowerU = coordinate.x;
            if (coordinate.x >= targetUv.x && coordinate.x < upperU)
                upperU = coordinate.x;
            if (coordinate.y <= targetUv.y && coordinate.y > lowerV)
                lowerV = coordinate.y;
            if (coordinate.y >= targetUv.y && coordinate.y < upperV)
                upperV = coordinate.y;
        }
        if (!IsFinite(lowerU) || !IsFinite(upperU) ||
            !IsFinite(lowerV) || !IsFinite(upperV))
            return false;

        int lowerLeft = FindUvVertex(uv, lowerU, lowerV);
        int lowerRight = FindUvVertex(uv, upperU, lowerV);
        int upperLeft = FindUvVertex(uv, lowerU, upperV);
        int upperRight = FindUvVertex(uv, upperU, upperV);
        if (lowerLeft < 0 || lowerRight < 0 || upperLeft < 0 || upperRight < 0)
            return false;

        float horizontal = Math.Abs(upperU - lowerU) < 0.000001f
            ? 0f
            : Mathf.InverseLerp(lowerU, upperU, targetUv.x);
        float vertical = Math.Abs(upperV - lowerV) < 0.000001f
            ? 0f
            : Mathf.InverseLerp(lowerV, upperV, targetUv.y);
        Vector3 bottom = Vector3.Lerp(vertices[lowerLeft], vertices[lowerRight], horizontal);
        Vector3 top = Vector3.Lerp(vertices[upperLeft], vertices[upperRight], horizontal);
        localPoint = Vector3.Lerp(bottom, top, vertical);
        return true;
    }

    private static int FindUvVertex(Vector2[] uv, float x, float y)
    {
        for (int index = 0; index < uv.Length; index++)
        {
            if (Math.Abs(uv[index].x - x) < 0.00001f &&
                Math.Abs(uv[index].y - y) < 0.00001f)
                return index;
        }
        return -1;
    }

    private static bool TryMapThroughWorldClicker(Vector3 worldPoint, out Vector2 unityScreen)
    {
        unityScreen = default;
        if (TryMapWithClicker(_cachedClicker, worldPoint, out unityScreen))
            return true;

        foreach (WorldSpaceClicker clicker in Resources.FindObjectsOfTypeAll<WorldSpaceClicker>())
        {
            if (!TryMapWithClicker(clicker, worldPoint, out unityScreen))
                continue;
            _cachedClicker = clicker;
            return true;
        }
        return false;
    }

    private static bool TryMapWithClicker(WorldSpaceClicker clicker, Vector3 worldPoint,
        out Vector2 unityScreen)
    {
        unityScreen = default;
        if (clicker == null || !clicker.gameObject.scene.IsValid() ||
            !clicker.gameObject.activeInHierarchy)
            return false;

        Vector2 xBounds = clicker.CursorXBounds;
        Vector2 yBounds = clicker.CursorYBounds;
        if (!IsBetween(worldPoint.x, xBounds.x, xBounds.y) ||
            !IsBetween(worldPoint.y, yBounds.x, yBounds.y))
            return false;

        unityScreen = MapMonitorWorldToUnityScreen(
            new Vector2(worldPoint.x, worldPoint.y), xBounds, yBounds,
            Screen.width, Screen.height);
        return IsFinite(unityScreen.x) && IsFinite(unityScreen.y);
    }

    private static bool IsBetween(float value, float endpointA, float endpointB)
    {
        float min = Math.Min(endpointA, endpointB);
        float max = Math.Max(endpointA, endpointB);
        return value >= min && value <= max;
    }

    private static string ObjectPath(Transform transform)
    {
        if (transform == null)
            return "<null>";
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
