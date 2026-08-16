using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace DeepSpaceChinese;

internal sealed class ReferenceCaptureRuntime
{
    private const string CaptureArgument = "--dsc-reference-capture=";
    private const string CaptureBase64Argument = "--dsc-reference-capture-base64=";
    private const string PageArgument = "--dsc-reference-page=";
    private const string PageBase64Argument = "--dsc-reference-page-base64=";
    private const string FilterBase64Argument = "--dsc-reference-filter-base64=";
    private const string BatchArgument = "--dsc-reference-batch";
    private const string FullScrollArgument = "--dsc-reference-full-scroll";
    private const int MaximumScrollCaptures = 24;
    private static readonly FieldInfo FullInfoHeightField =
        typeof(ReferenceSubWindow).GetField("fullInfoHeight",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo WindowHeightField =
        typeof(ReferenceSubWindow).GetField("windowHeight",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ScrollBarField =
        typeof(ReferenceSubWindow).GetField("scrollbar",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ScrollAreaField =
        typeof(ReferenceSubWindow).GetField("scrollArea",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo MovingPartField =
        typeof(ScrollArea).GetField("movingPart",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ReferenceInfoDisplayField =
        typeof(ReferenceWindow).GetField("infoDisplay",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo PageViewportField =
        typeof(ReferenceSubWindow).GetField("viewport",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo CurrentInfoWindowField =
        typeof(InfoDisplay).GetField("currWindow",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo CurrentReferencePageField =
        typeof(ReferenceWindow).GetField("currSubWindow",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo InfoDisplayReferenceWindowField =
        typeof(InfoDisplay).GetField("referenceWindow",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo PageReferenceWindowField =
        typeof(ReferenceSubWindow).GetField("refWindow",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo PageParentWindowField =
        typeof(ReferenceSubWindow).GetField("parentWindow",
            BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly DeepSpaceChinesePlugin _host;
    private readonly ManualLogSource _log;
    private readonly ReferencePageLayoutRuntime _layout;
    private readonly string _outputDirectory;
    private readonly bool _quitWhenComplete;
    private readonly bool _batch;
    private readonly bool _fullScroll;
    private readonly string _pageName;
    private readonly string[] _batchPageFilters;
    // All reference pages are rendered to the same physical information monitor.
    // Re-scoring every page can eventually choose another monitor whose boot logo happens
    // to overlap the projected glyph boxes. Lock the first pixel-verified camera for the
    // whole run and only verify that it still contains the requested page afterwards.
    private Camera _lockedCaptureCamera;

    private ReferenceCaptureRuntime(DeepSpaceChinesePlugin host, ManualLogSource log,
        ReferencePageLayoutRuntime layout, string outputDirectory, bool quitWhenComplete,
        bool batch, bool fullScroll, string pageName, string[] batchPageFilters)
    {
        _host = host;
        _log = log;
        _layout = layout;
        _outputDirectory = outputDirectory;
        _quitWhenComplete = quitWhenComplete;
        _batch = batch;
        _fullScroll = fullScroll;
        _pageName = pageName;
        _batchPageFilters = batchPageFilters ?? Array.Empty<string>();
    }

    internal static ReferenceCaptureRuntime TryCreate(DeepSpaceChinesePlugin host,
        ManualLogSource log, ReferencePageLayoutRuntime layout)
    {
        string outputDirectory = null;
        string pageName = null;
        string batchPageFilter = null;
        bool batch = false;
        bool fullScroll = false;
        bool quit = true;
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith(CaptureArgument, StringComparison.OrdinalIgnoreCase))
                outputDirectory = argument.Substring(CaptureArgument.Length).Trim('"');
            else if (argument.StartsWith(CaptureBase64Argument,
                         StringComparison.OrdinalIgnoreCase))
            {
                string encoded = argument.Substring(CaptureBase64Argument.Length);
                outputDirectory = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            else if (argument.StartsWith(PageArgument, StringComparison.OrdinalIgnoreCase))
                pageName = argument.Substring(PageArgument.Length).Trim('"');
            else if (argument.StartsWith(PageBase64Argument,
                         StringComparison.OrdinalIgnoreCase))
            {
                string encoded = argument.Substring(PageBase64Argument.Length);
                pageName = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            else if (argument.StartsWith(FilterBase64Argument,
                         StringComparison.OrdinalIgnoreCase))
            {
                string encoded = argument.Substring(FilterBase64Argument.Length);
                batchPageFilter = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            else if (string.Equals(argument, BatchArgument,
                         StringComparison.OrdinalIgnoreCase))
                batch = true;
            else if (string.Equals(argument, FullScrollArgument,
                         StringComparison.OrdinalIgnoreCase))
                fullScroll = true;
            else if (string.Equals(argument, "--dsc-reference-no-quit",
                         StringComparison.OrdinalIgnoreCase))
                quit = false;
        }
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return null;
        outputDirectory = Path.GetFullPath(outputDirectory);
        string[] batchPageFilters = (batchPageFilter ?? string.Empty)
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new ReferenceCaptureRuntime(host, log, layout, outputDirectory, quit,
            batch, fullScroll, pageName, batchPageFilters);
    }

    internal void Start() => _host.StartCoroutine(CaptureSafely());

    private IEnumerator CaptureSafely()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(CaptureAll());
        while (stack.Count > 0)
        {
            bool hasNext;
            object current;
            try
            {
                IEnumerator capture = stack.Peek();
                hasNext = capture.MoveNext();
                current = hasNext ? capture.Current : null;
                if (!hasNext)
                {
                    (capture as IDisposable)?.Dispose();
                    stack.Pop();
                    continue;
                }
            }
            catch (Exception exception)
            {
                string message = $"参考页截图发生未处理异常：{exception}";
                try
                {
                    Directory.CreateDirectory(_outputDirectory);
                    WriteJson(Path.Combine(_outputDirectory, "status.json"), new
                    {
                        state = "failed",
                        error = message,
                    });
                }
                catch
                {
                    // 保留原始异常，避免错误报告失败掩盖真正原因。
                }
                _log?.LogError(message);
                QuitIfRequested(2);
                yield break;
            }
            // Unity normally drives yielded child enumerators itself, which bypasses the
            // outer try/catch. Flatten them here so an exception in page stabilization or
            // scrolling always produces a failed status file instead of a hung capture.
            if (current is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return current;
        }
    }

    private IEnumerator CaptureAll()
    {
        Directory.CreateDirectory(_outputDirectory);
        var run = new CaptureRun
        {
            StartedUtc = DateTime.UtcNow,
            ScreenWidth = Screen.width,
            ScreenHeight = Screen.height,
            UnityVersion = Application.unityVersion,
            Pages = new List<CapturePage>(),
        };
        string statusPath = Path.Combine(_outputDirectory, "status.json");
        WriteJson(statusPath, new { state = "waiting", started_utc = run.StartedUtc });
        _log?.LogMessage($"参考页自动截图开始：{_outputDirectory}");

        float deadline = Time.realtimeSinceStartup + 45f;
        ReferenceSubWindow[] allPages = Array.Empty<ReferenceSubWindow>();
        ReferenceWindow referenceWindow = null;
        InfoDisplay infoDisplay = null;
        bool discoveryLogged = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            InfoDisplay[] displays = FindSceneObjects<InfoDisplay>().ToArray();
            // ReferenceWindow.infoDisplay 在部分场景里为空，不能把双向绑定当作发现条件。
            // 直接以 InfoDisplay 持有的 referenceWindow 为准，并优先已完成 Start 的实例。
            infoDisplay = displays
                .Where(display => InfoDisplayReferenceWindowField?.GetValue(display)
                                  is ReferenceWindow)
                .OrderByDescending(DisplayScreenScore)
                .ThenByDescending(display =>
                    CurrentInfoWindowField?.GetValue(display) is InfoWindow)
                .ThenByDescending(display => display.gameObject.activeInHierarchy)
                .FirstOrDefault();
            referenceWindow = infoDisplay == null
                ? null
                : InfoDisplayReferenceWindowField?.GetValue(infoDisplay) as ReferenceWindow;
            allPages = FindSceneObjects<ReferenceSubWindow>()
                .Where(page => ReferenceEquals(
                    PageReferenceWindowField?.GetValue(page), referenceWindow))
                .OrderBy(page => HierarchyPath(page.transform), StringComparer.Ordinal)
                .ToArray();
            if (referenceWindow != null && allPages.Length > 0)
                break;
            if (!discoveryLogged && Time.realtimeSinceStartup >= 5f)
            {
                discoveryLogged = true;
                _log?.LogWarning("参考页截图仍在等待场景绑定：" +
                    BuildDisplayDiagnostic(displays));
            }
            yield return null;
        }

        if (referenceWindow == null || allPages.Length == 0)
        {
            string diagnostic = BuildDisplayDiagnostic(FindSceneObjects<InfoDisplay>());
            string message = "45 秒内未找到当前活动 InfoDisplay 所绑定的参考页。" + diagnostic;
            run.Error = message;
            WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
            WriteJson(statusPath, new { state = "failed", error = message });
            _log?.LogError(message);
            QuitIfRequested(2);
            yield break;
        }

        if (infoDisplay == null)
        {
            string message = "ReferenceWindow 尚未绑定 InfoDisplay，游戏主界面未加载完成。";
            run.Error = message;
            WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
            WriteJson(statusPath, new { state = "failed", error = message });
            _log?.LogError(message);
            QuitIfRequested(2);
            yield break;
        }

        // InfoDisplay.Start 会在启动动画后设置默认窗口；过早打开参考页会被它重新覆盖。
        float mainUiDeadline = Time.realtimeSinceStartup + 45f;
        float readySince = -1f;
        while (Time.realtimeSinceStartup < mainUiDeadline)
        {
            bool initialized = CurrentInfoWindowField?.GetValue(infoDisplay) is InfoWindow;
            if (initialized)
            {
                if (readySince < 0f)
                    readySince = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - readySince >= 10f)
                    break;
            }
            else
            {
                readySince = -1f;
            }
            yield return null;
        }
        if (readySince < 0f)
        {
            string message = $"主监视器 45 秒内未稳定（当前分辨率 {Screen.width}x{Screen.height}）。";
            run.Error = message;
            WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
            WriteJson(statusPath, new { state = "failed", error = message });
            _log?.LogError(message);
            QuitIfRequested(2);
            yield break;
        }

        // Several complete InfoDisplay/ReferenceWindow sets exist in the scene. Their
        // initialization order is not their rendered-screen order. Re-select after the
        // opening animation using the camera that actually renders RT Info Monitor.
        InfoDisplay visibleDisplay = FindSceneObjects<InfoDisplay>()
            .Where(display => InfoDisplayReferenceWindowField?.GetValue(display)
                              is ReferenceWindow)
            .OrderByDescending(DisplayScreenScore)
            .ThenByDescending(display => display.gameObject.activeInHierarchy)
            .FirstOrDefault();
        if (visibleDisplay != null && !ReferenceEquals(visibleDisplay, infoDisplay))
        {
            infoDisplay = visibleDisplay;
            referenceWindow = InfoDisplayReferenceWindowField?.GetValue(infoDisplay)
                as ReferenceWindow;
            _log?.LogMessage("参考页自动截图：已按摄像机可见性切换到 " +
                             HierarchyPath(infoDisplay.transform));
        }

        _host.SetDisplayModeForReferenceCapture(DisplayMode.TranslationOnly);
        yield return WaitRealtime(1f);

        // 启动动画和本地化会重写部分参考页对象的名称与绑定。必须在主界面稳定后
        // 重新枚举，不能使用启动初期缓存的对象顺序，也不能预先 Close 全部页面。
        allPages = FindSceneObjects<ReferenceSubWindow>()
            .Where(page => ReferenceEquals(
                PageReferenceWindowField?.GetValue(page), referenceWindow))
            .OrderBy(page => HierarchyPath(page.transform), StringComparer.Ordinal)
            .ToArray();
        ReferenceSubWindow[] candidates;
        if (_batch)
        {
            candidates = _batchPageFilters.Length == 0
                ? allPages
                : allPages.Where(page => _batchPageFilters.Any(filter =>
                    string.Equals(HierarchyPath(page.transform), filter,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(page.transform.name, filter,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(page.name, filter,
                        StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
            if (_batchPageFilters.Length > 0 && candidates.Length != _batchPageFilters.Length)
            {
                string found = string.Join("、", candidates.Select(page =>
                    HierarchyPath(page.transform)));
                string message = $"参考页短名单应匹配 {_batchPageFilters.Length} 页，" +
                                 $"实际匹配 {candidates.Length} 页：{found}";
                run.Error = message;
                WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
                WriteJson(statusPath, new { state = "failed", error = message });
                _log?.LogError(message);
                QuitIfRequested(2);
                yield break;
            }
        }
        else
        {
            string requested = string.IsNullOrWhiteSpace(_pageName) ? "Blackhole" : _pageName;
            // ReferenceSubWindow declares its own name field; on some pages that logical
            // name does not match the actual hierarchy object (for example Neutron versus
            // Neutron Star). Prefer the stable hierarchy identity before the logical label.
            ReferenceSubWindow smokePage = allPages.FirstOrDefault(page =>
                string.Equals(HierarchyPath(page.transform), requested,
                    StringComparison.OrdinalIgnoreCase));
            smokePage ??= allPages.FirstOrDefault(page =>
                string.Equals(page.transform.name, requested,
                    StringComparison.OrdinalIgnoreCase));
            smokePage ??= allPages.FirstOrDefault(page =>
                string.Equals(page.name, requested, StringComparison.OrdinalIgnoreCase));
            if (smokePage == null)
            {
                string message = $"主界面稳定后找不到烟雾测试参考页：{requested}";
                run.Error = message;
                WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
                WriteJson(statusPath, new { state = "failed", error = message });
                _log?.LogError(message);
                QuitIfRequested(2);
                yield break;
            }
            candidates = new[] { smokePage };
        }
        run.DiscoveredPageCount = allPages.Length;
        _log?.LogMessage(_batch
            ? $"参考页自动截图：主界面稳定后发现 {allPages.Length} 页；" +
              (_batchPageFilters.Length == 0
                  ? "仅在页面打开后按风险规则筛选。"
                  : $"短名单匹配 {candidates.Length} 页。")
            : $"参考页烟雾测试：只验证 {HierarchyPath(candidates[0].transform)} 的顶部和底部。");

        infoDisplay.OpenReference();
        float wipeDoneAt = Time.realtimeSinceStartup + 1.5f;
        while (Time.realtimeSinceStartup < wipeDoneAt)
            yield return null;

        for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
        {
            ReferenceSubWindow page = candidates[candidateIndex];
            if (page == null)
                continue;

            var stopwatch = Stopwatch.StartNew();
            page.Open();
            // 至少等待一秒，并要求文本、字号和坐标连续稳定；不再按固定 5 帧抢拍。
            yield return WaitForPageStable(page, 1f, 5f);
            stopwatch.Stop();

            if (!IsReferencePageVisible(infoDisplay, referenceWindow, page))
            {
                string message = $"参考页 {page.name} 未在前台显示，拒绝生成无效截图。" +
                                 BuildVisibilityDiagnostic(infoDisplay, referenceWindow, page);
                run.Error = message;
                WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
                WriteJson(statusPath, new { state = "failed", error = message });
                _log?.LogError(message);
                QuitIfRequested(2);
                yield break;
            }

            // Most pages instantiate or activate their actual content only from Open(). Risk
            // classification before this point silently misses images and copy buttons.
            PageRisk risk = InspectRisk(page);
            if (_batch && !risk.ShouldCapture)
            {
                page.Close();
                continue;
            }

            var capturePage = new CapturePage
            {
                Index = run.Pages.Count,
                Name = page.name,
                Path = HierarchyPath(page.transform),
                OpenAndStabilizeMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                HasCopyButtons = risk.CopyButtonCount > 0,
                HasGraphics = risk.GraphicCount > 0,
                RiskReasons = risk.Reasons,
                FontSizes = ContentComponents<TMP_Text>(page, false)
                    .Where(text => text != null && text.gameObject.activeInHierarchy)
                    .Select(text => Math.Round(text.fontSize, 4))
                    .Distinct()
                    .OrderBy(value => value)
                    .ToList(),
                Captures = new List<CaptureFrame>(),
            };
            run.Pages.Add(capturePage);

            ScrollBar3D scrollBar = ScrollBarField?.GetValue(page) as ScrollBar3D;
            ScrollArea scrollArea = ScrollAreaField?.GetValue(page) as ScrollArea;
            Transform movingPart = scrollArea != null &&
                                   MovingPartField?.GetValue(scrollArea) is GameObject moving
                ? moving.transform
                : null;
            float fullHeight = ReadFloat(FullInfoHeightField, page);
            float windowHeight = ReadFloat(WindowHeightField, page);
            float originalFullHeight = _layout?.OriginalFullInfoHeightFor(page) ?? 0f;
            int captureCount = scrollBar == null
                ? 1
                : ScrollCaptureCount(Math.Max(fullHeight, originalFullHeight), windowHeight);
            if (!_batch && !_fullScroll && captureCount > 1)
                captureCount = 2;
            var languages = new[]
            {
                (Mode: DisplayMode.TranslationOnly, Code: "zh"),
                (Mode: DisplayMode.OriginalOnly, Code: "en"),
            };
            foreach ((DisplayMode mode, string languageCode) in languages)
            {
                WriteJson(statusPath, new
                {
                    state = "capturing",
                    page = capturePage.Name,
                    page_path = capturePage.Path,
                    language = languageCode,
                    stage = "switching-language",
                });
                _log?.LogMessage($"参考页自动截图：{capturePage.Path} 切换到 {languageCode}。");
                _host.SetDisplayModeForReferenceCapture(mode);
                _log?.LogMessage($"参考页自动截图：{capturePage.Path} 已切换到 {languageCode}，准备打开页面。");
                // F8 may leave the reference window on the other monitor. Reopen the parent
                // before every language so paired captures use the same display and viewport.
                infoDisplay.OpenReference();
                yield return null;
                page.Open();
                yield return null;
                _host.SetDisplayModeForReferenceCapture(mode);
                yield return WaitForPageStable(page, 1f, 5f);
                // The previous language finishes its capture at the bottom of long pages.
                // Camera validation samples the page's leading text objects, so validating
                // before restoring the viewport to the top incorrectly reports a healthy
                // monitor as black. It also makes paired frame zero start at different
                // positions. Normalize and settle the viewport before any pixel test.
                scrollBar?.ForceScrollTo(1f);
                yield return WaitForScrollStable(scrollBar, movingPart, 0.35f, 3f);
                // PageSignature observes TMP content/layout, not the monitor wipe shader.
                // On a long run the text can be stable while RT Info Monitor is still
                // completely black. Give that visual transition time to finish before
                // probing the render target.
                yield return WaitRealtime(0.5f);
                // Camera names, layers and target-texture names are shared by several
                // in-world monitors. Select the target whose rendered pixels actually
                // contain this page's TMP glyphs; geometry-only guesses eventually drift
                // to the unrelated METEOR OS launcher in long runs.
                Camera captureCamera = null;
                float cameraDeadline = Time.realtimeSinceStartup + 6f;
                float reopenAt = Time.realtimeSinceStartup + 2f;
                int reopenCount = 0;
                while (Time.realtimeSinceStartup < cameraDeadline)
                {
                    yield return new WaitForEndOfFrame();
                    captureCamera = LockedCaptureCamera(ContentRoot(page), infoDisplay);
                    if (captureCamera?.targetTexture != null)
                        break;

                    // A language switch can finish rewriting TMP text before the physical
                    // monitor has attached the ReferenceWindow again. Re-run the lifecycle
                    // at most once. Reopening every half second restarts the monitor wipe
                    // before it can finish and deterministically traps long batches on a
                    // black RenderTexture.
                    if (reopenCount > 0 || Time.realtimeSinceStartup < reopenAt)
                    {
                        yield return null;
                        continue;
                    }
                    reopenCount++;
                    infoDisplay.OpenReference();
                    yield return null;
                    page.Open();
                    yield return null;
                    _host.SetDisplayModeForReferenceCapture(mode);
                }
                if (captureCamera?.targetTexture == null)
                {
                    string cameraDiagnostic = BuildCaptureCameraDiagnostic(ContentRoot(page));
                    throw new InvalidOperationException(
                        $"参考页 {capturePage.Name} 在 6 秒和 {reopenCount} 次重新打开后，" +
                        "仍找不到包含当前页文字像素的 RenderTexture 摄像机。" +
                        BuildVisibilityDiagnostic(infoDisplay, referenceWindow, page) +
                        " " + cameraDiagnostic);
                }
                for (int scrollIndex = 0; scrollIndex < captureCount; scrollIndex++)
                {
                    float normalized = captureCount == 1
                        ? 1f
                        : 1f - scrollIndex / (float)(captureCount - 1);
                    scrollBar?.ForceScrollTo(normalized);
                    yield return WaitForScrollStable(scrollBar, movingPart, 0.35f, 3f);
                    yield return new WaitForEndOfFrame();

                    string stem = $"{capturePage.Index:D3}_{SafeFileName(capturePage.Name)}_" +
                                  $"{languageCode}_{scrollIndex:D2}";
                    string pngPath = Path.Combine(_outputDirectory, stem + ".png");
                    WriteJson(statusPath, new
                    {
                        state = "capturing",
                        page = capturePage.Name,
                        page_path = capturePage.Path,
                        language = languageCode,
                        stage = "screenshot",
                        scroll_index = scrollIndex,
                        scroll_count = captureCount,
                    });
                    CaptureRenderTexture(captureCamera.targetTexture, pngPath);
                    CaptureFrame frame = CaptureLayout(page, languageCode, normalized,
                        scrollBar?.NormalizedScroll ?? float.NaN,
                        movingPart?.localPosition.y ?? float.NaN, pngPath, captureCamera);
                    capturePage.Captures.Add(frame);
                    yield return null;
                }
            }
            _host.SetDisplayModeForReferenceCapture(DisplayMode.TranslationOnly);
            page.Open();
            yield return WaitForPageStable(page, 0.5f, 3f);
            page.Close();
            WriteJson(Path.Combine(_outputDirectory, "manifest.partial.json"), run);
        }

        run.CompletedUtc = DateTime.UtcNow;
        run.SelectedPageCount = run.Pages.Count;
        WriteJson(Path.Combine(_outputDirectory, "manifest.json"), run);
        WriteJson(statusPath, new
        {
            state = "complete",
            page_count = run.Pages.Count,
            capture_count = run.Pages.Sum(page => page.Captures.Count),
            completed_utc = run.CompletedUtc,
        });
        _log?.LogMessage($"参考页自动截图完成：{run.Pages.Count} 页，" +
                         $"{run.Pages.Sum(page => page.Captures.Count)} 张。");
        QuitIfRequested(0);
    }

    private CaptureFrame CaptureLayout(ReferenceSubWindow page, string language,
        float requestedScroll, float actualScroll, float movingPartY, string pngPath,
        Camera camera)
    {
        Transform contentRoot = ContentRoot(page);
        CapturePoint rootPoint = ScreenPoint(contentRoot?.position ?? page.transform.position,
            camera);
        var elements = new List<CaptureElement>();
        foreach (TMP_Text text in ContentComponents<TMP_Text>(page, false))
        {
            if (text == null || !text.gameObject.activeInHierarchy)
                continue;
            text.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
            List<CaptureLine> lines = CaptureLines(text, camera, rootPoint);
            elements.Add(new CaptureElement
            {
                InstanceId = text.GetInstanceID(),
                Kind = "text",
                Name = text.name,
                Path = HierarchyPath(text.transform),
                Text = text.text,
                FontSize = text.fontSize,
                LineCount = text.textInfo?.lineCount ?? 0,
                Lines = lines,
                ScreenRect = RelativeRect(
                    ScreenRectForLocalBounds(text.transform, text.textBounds, camera), rootPoint),
            });
        }
        foreach (Renderer renderer in ContentComponents<Renderer>(page, false))
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy ||
                renderer.GetComponent<TMP_Text>() != null)
                continue;
            elements.Add(new CaptureElement
            {
                InstanceId = renderer.GetInstanceID(),
                Kind = "graphic",
                Name = renderer.name,
                Path = HierarchyPath(renderer.transform),
                ScreenRect = RelativeRect(ScreenRectForWorldBounds(renderer.bounds, camera),
                    rootPoint),
            });
        }
        foreach (ClipboardCopyButton button in ContentComponents<ClipboardCopyButton>(page, false))
        {
            if (button == null || !button.gameObject.activeInHierarchy)
                continue;
            var captured = new CaptureElement
            {
                InstanceId = button.GetInstanceID(),
                Kind = "copy-button",
                Name = button.name,
                Path = HierarchyPath(button.transform),
                CopyValue = button.stringToCopy,
                ScreenPoint = RelativePoint(ScreenPoint(button.transform.position, camera),
                    rootPoint),
            };
            if (_layout != null && _layout.TryGetCopyButtonAnchor(button,
                    out TMP_Text anchor, out int logicalLine, out int wrapIndex))
            {
                captured.AnchorTextInstanceId = anchor.GetInstanceID();
                captured.AnchorLogicalLine = logicalLine;
                captured.AnchorWrapIndex = wrapIndex;
            }
            elements.Add(captured);
        }
        return new CaptureFrame
        {
            Language = language,
            RequestedScroll = requestedScroll,
            ActualScroll = actualScroll,
            MovingPartY = movingPartY,
            RootScreenPoint = rootPoint,
            Screenshot = Path.GetFileName(pngPath),
            Elements = elements,
        };
    }

    private static PageRisk InspectRisk(ReferenceSubWindow page, bool includeInactive = false)
    {
        if (page == null)
            return new PageRisk();
        int copyButtons = ContentComponents<ClipboardCopyButton>(page, includeInactive)
            .Count(button => button != null &&
                             (includeInactive || button.gameObject.activeInHierarchy));
        int graphics = ContentComponents<Renderer>(page, includeInactive).Count(renderer =>
            renderer != null && (includeInactive || renderer.gameObject.activeInHierarchy) &&
            renderer.GetComponent<TMP_Text>() == null &&
            renderer.GetComponent<ClipboardCopyButton>() == null);
        float[] fontSizes = ContentComponents<TMP_Text>(page, includeInactive)
            .Where(text => text != null &&
                           (includeInactive || text.gameObject.activeInHierarchy) &&
                           text.fontSize > 0f)
            .Select(text => text.fontSize)
            .ToArray();
        bool mixedSizes = fontSizes.Length >= 2 &&
                          fontSizes.Max() - fontSizes.Min() >= 0.08f;
        var reasons = new List<string>();
        if (graphics > 0) reasons.Add("image-or-graphic");
        if (copyButtons > 0) reasons.Add("copy-button");
        if (mixedSizes) reasons.Add("mixed-font-sizes");
        return new PageRisk
        {
            CopyButtonCount = copyButtons,
            GraphicCount = graphics,
            Reasons = reasons,
        };
    }

    private static IEnumerable<T> ContentComponents<T>(ReferenceSubWindow page,
        bool includeInactive) where T : Component
    {
        if (page == null)
            return Enumerable.Empty<T>();
        ScrollArea area = ScrollAreaField?.GetValue(page) as ScrollArea;
        Transform root = ContentRoot(page);
        IEnumerable<T> components = root.GetComponentsInChildren<T>(includeInactive)
            .Where(component => component != null);
        // ScrollArea is frequently a sibling owned through a field rather than a child of
        // ReferenceSubWindow, so GetComponentInParent cannot identify its page. The field is
        // already the ownership boundary. Only the transform fallback needs parent filtering.
        return area != null
            ? components
            : components.Where(component => IsOwnedBy(page, component.transform));
    }

    private static Transform ContentRoot(ReferenceSubWindow page)
    {
        if (page == null)
            return null;
        ScrollArea area = ScrollAreaField?.GetValue(page) as ScrollArea;
        GameObject moving = area != null
            ? MovingPartField?.GetValue(area) as GameObject
            : null;
        if (moving != null)
            return moving.transform;
        if (area != null)
            return area.transform;

        // “Periodic Table” is a launcher ReferenceSubWindow. Its visible panel is a
        // separately-instantiated PeriodicTableDisplay, so the launcher itself contains no
        // renderable descendants and yields an empty layer mask. Resolve the real panel as
        // the ownership root for risk inspection, camera selection and layout capture.
        if (string.Equals(page.name, "Periodic Table", StringComparison.OrdinalIgnoreCase))
        {
            PeriodicTableDisplay display = Resources.FindObjectsOfTypeAll<PeriodicTableDisplay>()
                .FirstOrDefault(candidate => candidate != null &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.gameObject.activeInHierarchy);
            if (display != null)
                return display.transform;
        }
        return page.transform;
    }

    private static IEnumerable<T> OwnedComponents<T>(ReferenceSubWindow page,
        bool includeInactive) where T : Component =>
        page.GetComponentsInChildren<T>(includeInactive).Where(component =>
            component != null && IsOwnedBy(page, component.transform));

    private static bool IsOwnedBy(ReferenceSubWindow page, Transform transform) =>
        ReferenceEquals(transform.GetComponentInParent<ReferenceSubWindow>(true), page);

    private static int ScrollCaptureCount(float fullHeight, float windowHeight)
    {
        if (fullHeight <= 0f || windowHeight <= 0f || fullHeight <= windowHeight * 1.02f)
            return 1;
        return Mathf.Clamp(Mathf.CeilToInt(fullHeight / (windowHeight * 0.82f)),
            2, MaximumScrollCaptures);
    }

    private static IEnumerator WaitForPageStable(ReferenceSubWindow page,
        float minimumSeconds, float maximumSeconds)
    {
        float started = Time.realtimeSinceStartup;
        string previous = null;
        int stableFrames = 0;
        while (Time.realtimeSinceStartup - started < maximumSeconds)
        {
            yield return null;
            string current = PageSignature(page);
            stableFrames = string.Equals(current, previous, StringComparison.Ordinal)
                ? stableFrames + 1
                : 0;
            previous = current;
            if (Time.realtimeSinceStartup - started >= minimumSeconds && stableFrames >= 4)
                yield break;
        }
    }

    private static IEnumerator WaitRealtime(float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    private static IEnumerator WaitForScrollStable(ScrollBar3D scrollBar,
        Transform movingPart, float minimumSeconds, float maximumSeconds)
    {
        float started = Time.realtimeSinceStartup;
        float previousY = float.NaN;
        int stableFrames = 0;
        while (Time.realtimeSinceStartup - started < maximumSeconds)
        {
            yield return null;
            float y = movingPart?.localPosition.y ?? scrollBar?.NormalizedScroll ?? 0f;
            stableFrames = !float.IsNaN(previousY) && Mathf.Abs(y - previousY) < 0.0001f
                ? stableFrames + 1
                : 0;
            previousY = y;
            if (Time.realtimeSinceStartup - started >= minimumSeconds && stableFrames >= 3)
                yield break;
        }
    }

    private static IEnumerator WaitForScreenshot(string path, float maximumSeconds)
    {
        float started = Time.realtimeSinceStartup;
        long previousLength = -1;
        int stableFrames = 0;
        while (Time.realtimeSinceStartup - started < maximumSeconds)
        {
            yield return null;
            long length = File.Exists(path) ? new FileInfo(path).Length : 0;
            stableFrames = length > 0 && length == previousLength ? stableFrames + 1 : 0;
            previousLength = length;
            if (stableFrames >= 2)
                yield break;
        }
    }

    private static string PageSignature(ReferenceSubWindow page)
    {
        var builder = new StringBuilder();
        foreach (TMP_Text text in ContentComponents<TMP_Text>(page, false)
                     .Where(text => text != null && text.gameObject.activeInHierarchy)
                     .OrderBy(text => text.GetInstanceID()))
        {
            Vector3 position = text.transform.position;
            builder.Append(text.GetInstanceID()).Append('|').Append(text.text).Append('|')
                .Append(text.fontSize.ToString("F3")).Append('|')
                .Append(position.x.ToString("F3")).Append('|')
                .Append(position.y.ToString("F3")).Append(';');
        }
        return builder.ToString();
    }

    private static string BuildDisplayDiagnostic(IEnumerable<InfoDisplay> displays) =>
        string.Join("；", displays.Select(display =>
        {
            ReferenceWindow reference = InfoDisplayReferenceWindowField?.GetValue(display)
                as ReferenceWindow;
            bool mutual = reference != null &&
                          ReferenceEquals(ReferenceInfoDisplayField?.GetValue(reference), display);
            return $"{HierarchyPath(display.transform)} active={display.gameObject.activeInHierarchy} " +
                   $"enabled={display.enabled} current=" +
                   $"{(CurrentInfoWindowField?.GetValue(display) as InfoWindow)?.name ?? "<null>"} " +
                   $"reference={reference?.name ?? "<null>"} mutual={mutual}";
        }));

    private static bool IsReferencePageVisible(InfoDisplay infoDisplay,
        ReferenceWindow window,
        ReferenceSubWindow page)
    {
        GameObject pageViewport = PageViewportField?.GetValue(page) as GameObject;
        Transform contentRoot = ContentRoot(page);
        return infoDisplay != null &&
               ReferenceEquals(CurrentInfoWindowField?.GetValue(infoDisplay), window) &&
               ReferenceEquals(CurrentReferencePageField?.GetValue(window), page) &&
               pageViewport != null && pageViewport.activeInHierarchy &&
               IsTransformOnScreen(contentRoot);
    }

    private static string BuildVisibilityDiagnostic(InfoDisplay infoDisplay,
        ReferenceWindow window, ReferenceSubWindow page)
    {
        InfoWindow currentWindow = CurrentInfoWindowField?.GetValue(infoDisplay) as InfoWindow;
        ReferenceSubWindow currentPage = CurrentReferencePageField?.GetValue(window)
            as ReferenceSubWindow;
        GameObject pageViewport = PageViewportField?.GetValue(page) as GameObject;
        ReferenceSubWindow parent = PageParentWindowField?.GetValue(page)
            as ReferenceSubWindow;
        return $" currentWindow={currentWindow?.name ?? "<null>"}" +
               $" currentPage={currentPage?.name ?? "<null>"}" +
               $" viewportActive={pageViewport?.activeInHierarchy.ToString() ?? "<null>"}" +
               $" contentScreen={ScreenDiagnostic(ContentRoot(page))}" +
               $" parent={parent?.name ?? "<null>"}";
    }

    private static float DisplayScreenScore(InfoDisplay display)
    {
        if (display == null)
            return float.MinValue;
        Camera camera = FindRenderingCamera(display.transform);
        if (camera == null)
            return -100000f;
        Vector3 point = camera.WorldToViewportPoint(display.transform.position);
        float dx = point.x - 0.5f;
        float dy = point.y - 0.5f;
        float score = 1000f - Mathf.Sqrt(dx * dx + dy * dy);
        if (string.Equals(camera.targetTexture?.name, "RT Info Monitor",
                StringComparison.OrdinalIgnoreCase))
            score += 10000f;
        if (display.gameObject.activeInHierarchy)
            score += 10f;
        return score;
    }

    private static bool IsTransformOnScreen(Transform transform)
    {
        if (transform == null)
            return false;
        Camera camera = FindRenderingCamera(transform);
        if (camera == null)
            return false;
        Vector3 point = camera.WorldToViewportPoint(transform.position);
        return point.z > 0f && point.x >= -0.15f && point.x <= 1.15f &&
               point.y >= -0.15f && point.y <= 1.15f;
    }

    private static string ScreenDiagnostic(Transform transform)
    {
        if (transform == null)
            return "<null>";
        Camera camera = FindRenderingCamera(transform);
        if (camera == null)
            return "<no-camera>";
        Vector3 point = camera.WorldToScreenPoint(transform.position);
        return $"({point.x:F1},{point.y:F1},{point.z:F1})";
    }

    private static Camera FindRenderingCamera(Transform transform)
    {
        if (transform == null)
            return null;

        // A number of reference pages use a content root whose pivot lies well outside
        // the visible monitor even though its child labels and diagrams are on-screen.
        // Testing only the root position therefore rejects the correct RenderTexture
        // camera. Score cameras against visible descendant pivots/rect centres instead.
        Transform[] descendants = transform.GetComponentsInChildren<Transform>(false);
        int layerMask = 0;
        var samplePositions = new List<Vector3>(Math.Min(descendants.Length * 2, 256));
        foreach (Transform descendant in descendants.Take(128))
        {
            if (descendant == null || !descendant.gameObject.activeInHierarchy)
                continue;
            layerMask |= 1 << descendant.gameObject.layer;
            samplePositions.Add(descendant.position);
            if (descendant is RectTransform rect)
                samplePositions.Add(rect.TransformPoint(rect.rect.center));
        }
        if (samplePositions.Count == 0)
            samplePositions.Add(transform.position);

        Camera camera = Resources.FindObjectsOfTypeAll<Camera>()
            .Where(camera => camera != null && camera.gameObject.scene.IsValid() &&
                             camera.gameObject.activeInHierarchy && camera.enabled &&
                             camera.targetTexture != null &&
                             (camera.cullingMask & layerMask) != 0)
            .Select(camera => new
            {
                Camera = camera,
                Points = samplePositions.Select(camera.WorldToViewportPoint).ToArray(),
            })
            .Select(candidate => new
            {
                candidate.Camera,
                Visible = candidate.Points.Count(point => point.z > 0f &&
                    point.x >= -0.05f && point.x <= 1.05f &&
                    point.y >= -0.05f && point.y <= 1.05f),
                CentreDistance = candidate.Points
                    .Where(point => point.z > 0f)
                    .Select(point => Math.Abs(point.x - 0.5f) +
                                     Math.Abs(point.y - 0.5f))
                    .DefaultIfEmpty(float.MaxValue)
                    .Min(),
            })
            .Where(candidate => candidate.Visible > 0)
            // Visibility is authoritative. Several monitor cameras share the same UI
            // layer and one of them can still see the page root while rendering an
            // unrelated boot/logo screen. Prefer the camera containing the most actual
            // page descendants; the render-target name is only a tie-breaker.
            .OrderByDescending(candidate => candidate.Visible)
            .ThenByDescending(candidate => string.Equals(
                candidate.Camera.targetTexture.name, "RT Info Monitor",
                StringComparison.OrdinalIgnoreCase))
            .ThenBy(candidate => candidate.CentreDistance)
            .Select(candidate => candidate.Camera)
            .FirstOrDefault();
        if (camera != null)
            return camera;

        // Special reference launchers can render their visible panel outside the launcher
        // hierarchy. Once the reference window has established the current page, the Info
        // Monitor RenderTexture is still the authoritative output and is safer than failing
        // or selecting an unrelated active RenderTexture.
        return Resources.FindObjectsOfTypeAll<Camera>()
            .FirstOrDefault(candidate => candidate != null &&
                candidate.gameObject.scene.IsValid() &&
                candidate.gameObject.activeInHierarchy && candidate.enabled &&
                candidate.targetTexture != null && string.Equals(
                    candidate.targetTexture.name, "RT Info Monitor",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static Camera FindCaptureCamera(Transform transform)
    {
        if (transform == null)
            return null;

        TMP_Text[] texts = CaptureTexts(transform);

        int layerMask = transform.GetComponentsInChildren<Transform>(false)
            .Where(item => item != null && item.gameObject.activeInHierarchy)
            .Aggregate(0, (mask, item) => mask | (1 << item.gameObject.layer));
        var candidate = Resources.FindObjectsOfTypeAll<Camera>()
            .Where(camera => camera != null && camera.gameObject.scene.IsValid() &&
                             camera.gameObject.activeInHierarchy && camera.enabled &&
                             camera.targetTexture != null &&
                             (camera.cullingMask & layerMask) != 0)
            .Select(camera => new
            {
                Camera = camera,
                Score = RenderedTextHitScore(camera, texts),
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => string.Equals(
                item.Camera.targetTexture.name, "RT Info Monitor",
                StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        return candidate != null && candidate.Score > 0 ? candidate.Camera : null;
    }

    private Camera LockedCaptureCamera(Transform contentRoot, InfoDisplay infoDisplay)
    {
        TMP_Text[] texts = CaptureTexts(contentRoot);
        if (_lockedCaptureCamera != null && _lockedCaptureCamera.gameObject != null &&
            _lockedCaptureCamera.gameObject.scene.IsValid() &&
            _lockedCaptureCamera.targetTexture != null &&
            RenderedTextHitScore(_lockedCaptureCamera, texts) > 0)
            return _lockedCaptureCamera;

        Camera previous = _lockedCaptureCamera;
        // F8 can move the reference window to another physical monitor. Keep using the
        // cached camera only while it still contains pixels from the current page; once it
        // does not, reacquire the camera instead of silently saving a black frame. Pages
        // with visible text must pass the pixel test -- the hierarchy-only fallback is safe
        // only for a genuinely textless page.
        _lockedCaptureCamera = FindCaptureCamera(contentRoot);
        if (_lockedCaptureCamera == null && texts.Length == 0)
            _lockedCaptureCamera = FindRenderingCamera(infoDisplay?.transform);
        if (_lockedCaptureCamera != null)
            _log?.LogMessage("参考页自动截图：" +
                             (previous == null ? "锁定" : "重新锁定") +
                             "参考监视器摄像机 " +
                             $"{HierarchyPath(_lockedCaptureCamera.transform)} -> " +
                             $"{_lockedCaptureCamera.targetTexture?.name ?? "<null>"} " +
                             $"#{_lockedCaptureCamera.targetTexture?.GetInstanceID() ?? 0}。");
        return _lockedCaptureCamera;
    }

    private static TMP_Text[] CaptureTexts(Transform transform)
    {
        if (transform == null)
            return Array.Empty<TMP_Text>();
        TMP_Text[] texts = transform.GetComponentsInChildren<TMP_Text>(false)
            .Where(text => text != null && text.gameObject.activeInHierarchy &&
                           !string.IsNullOrWhiteSpace(text.text))
            .Take(128)
            .ToArray();
        foreach (TMP_Text text in texts)
            text.ForceMeshUpdate(ignoreActiveState: false, forceTextReparsing: true);
        return texts;
    }

    private static int RenderedTextHitScore(Camera camera, IEnumerable<TMP_Text> texts)
    {
        TextRenderProbe probe = ProbeRenderedText(camera, texts);
        // A single bright launcher/logo pixel can overlap a projected glyph box by
        // accident. That used to accept the blank METEOR OS monitor as a reference
        // page after a long capture run. Real reference pages have substantially more
        // foreground coverage and several independently rendered glyphs, so require
        // both signals before a camera may be locked.
        return probe.BrightPixelCount >= 1000 && probe.Score >= 2
            ? probe.Score
            : 0;
    }

    private static string BuildCaptureCameraDiagnostic(Transform transform)
    {
        if (transform == null)
            return "content=<null>";
        TMP_Text[] texts = CaptureTexts(transform);
        int layerMask = transform.GetComponentsInChildren<Transform>(false)
            .Where(item => item != null && item.gameObject.activeInHierarchy)
            .Aggregate(0, (mask, item) => mask | (1 << item.gameObject.layer));
        string textSummary = $"texts={texts.Length} chars={texts.Sum(text => text.textInfo.characterCount)}";
        string cameras = string.Join(" | ", Resources.FindObjectsOfTypeAll<Camera>()
            .Where(camera => camera != null && camera.gameObject.scene.IsValid() &&
                             camera.gameObject.activeInHierarchy && camera.enabled &&
                             camera.targetTexture != null &&
                             (camera.cullingMask & layerMask) != 0)
            .Select(camera =>
            {
                TextRenderProbe probe = ProbeRenderedText(camera, texts);
                return $"camera={HierarchyPath(camera.transform)} " +
                       $"rt={camera.targetTexture.name}#{camera.targetTexture.GetInstanceID()} " +
                       $"size={camera.targetTexture.width}x{camera.targetTexture.height} " +
                       $"bright={probe.BrightPixelCount} visible={probe.VisibleCharacterCount} " +
                       $"onscreen={probe.OnscreenCharacterCount} " +
                       $"sampled={probe.SampledCharacterCount} hits={probe.Score}";
            }));
        return textSummary + " candidates=[" + cameras + "]";
    }

    private sealed class TextRenderProbe
    {
        public int Score;
        public int BrightPixelCount;
        public int VisibleCharacterCount;
        public int OnscreenCharacterCount;
        public int SampledCharacterCount;
    }

    private static TextRenderProbe ProbeRenderedText(Camera camera,
        IEnumerable<TMP_Text> texts)
    {
        var probe = new TextRenderProbe();
        if (camera?.targetTexture == null)
            return probe;
        const int sampleWidth = 202;
        const int sampleHeight = 151;
        RenderTexture sample = RenderTexture.GetTemporary(sampleWidth, sampleHeight, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        var texture = new Texture2D(sampleWidth, sampleHeight, TextureFormat.RGB24, false);
        try
        {
            // The coroutine reaches this probe only after WaitForEndOfFrame, so the target
            // already contains Unity's authoritative camera output. Calling Camera.Render
            // again under Unity 6 RenderGraph can clear an SRP RenderTexture without
            // replaying the in-world TMP UI, producing a deterministic black target after
            // enough language/page transitions.
            Graphics.Blit(camera.targetTexture, sample);
            RenderTexture.active = sample;
            texture.ReadPixels(new Rect(0f, 0f, sampleWidth, sampleHeight), 0, 0, false);
            texture.Apply(false, false);
            Color32[] pixels = texture.GetPixels32();
            int sampledCharacters = 0;
            probe.BrightPixelCount = pixels.Count(pixel =>
                Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) >= 48);
            foreach (TMP_Text text in texts)
            {
                TMP_TextInfo info = text.textInfo;
                for (int index = 0; index < info.characterCount; index++)
                {
                    TMP_CharacterInfo character = info.characterInfo[index];
                    if (!character.isVisible || char.IsWhiteSpace(character.character))
                        continue;
                    probe.VisibleCharacterCount++;
                    Vector3 bottomLeft = camera.WorldToViewportPoint(
                        text.transform.TransformPoint(character.bottomLeft));
                    Vector3 topRight = camera.WorldToViewportPoint(
                        text.transform.TransformPoint(character.topRight));
                    if (bottomLeft.z <= 0f || topRight.z <= 0f)
                        continue;
                    float leftViewport = Math.Min(bottomLeft.x, topRight.x);
                    float rightViewport = Math.Max(bottomLeft.x, topRight.x);
                    float bottomViewport = Math.Min(bottomLeft.y, topRight.y);
                    float topViewport = Math.Max(bottomLeft.y, topRight.y);
                    // Never clamp a completely off-screen glyph onto a texture edge. That
                    // made unrelated bright boot logos look like successful text hits.
                    if (rightViewport <= 0f || leftViewport >= 1f ||
                        topViewport <= 0f || bottomViewport >= 1f)
                        continue;
                    probe.OnscreenCharacterCount++;
                    if (sampledCharacters >= 96)
                        continue;
                    // Off-screen characters must not consume the sample budget. Long pages
                    // often store many inactive-view rows before the currently visible row;
                    // counting those first made every camera score zero despite visible text.
                    sampledCharacters++;
                    probe.SampledCharacterCount = sampledCharacters;
                    int left = Mathf.Clamp(Mathf.FloorToInt(
                        leftViewport * sampleWidth), 0, sampleWidth - 1);
                    int right = Mathf.Clamp(Mathf.CeilToInt(
                        rightViewport * sampleWidth), 0, sampleWidth - 1);
                    int bottom = Mathf.Clamp(Mathf.FloorToInt(
                        bottomViewport * sampleHeight), 0, sampleHeight - 1);
                    int top = Mathf.Clamp(Mathf.CeilToInt(
                        topViewport * sampleHeight), 0, sampleHeight - 1);
                    bool hit = false;
                    for (int y = bottom; y <= top && !hit; y++)
                    {
                        for (int x = left; x <= right; x++)
                        {
                            Color32 pixel = pixels[y * sampleWidth + x];
                            if (Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) < 48)
                                continue;
                            hit = true;
                            break;
                        }
                    }
                    if (hit)
                        probe.Score++;
                }
            }
            return probe;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(sample);
            UnityEngine.Object.Destroy(texture);
        }
    }

    private static void CaptureRenderTexture(RenderTexture source, string path)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        RenderTexture previous = RenderTexture.active;
        var texture = new Texture2D(source.width, source.height,
            TextureFormat.RGB24, mipChain: false);
        try
        {
            RenderTexture.active = source;
            texture.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0, false);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(texture);
        }
    }

    private static float ReadFloat(FieldInfo field, object instance)
    {
        if (field?.GetValue(instance) is float value)
            return value;
        return 0f;
    }

    private static CaptureRect ScreenRectForLocalBounds(Transform transform,
        Bounds bounds, Camera camera)
    {
        var points = new[]
        {
            transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z)),
            transform.TransformPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)),
            transform.TransformPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z)),
            transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z)),
        };
        return ScreenRect(points, camera);
    }

    private static CaptureRect ScreenRectForWorldBounds(Bounds bounds, Camera camera)
    {
        var points = new List<Vector3>(8);
        for (int x = 0; x < 2; x++)
        for (int y = 0; y < 2; y++)
        for (int z = 0; z < 2; z++)
            points.Add(new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                y == 0 ? bounds.min.y : bounds.max.y,
                z == 0 ? bounds.min.z : bounds.max.z));
        return ScreenRect(points, camera);
    }

    private static CaptureRect ScreenRect(IEnumerable<Vector3> worldPoints, Camera camera)
    {
        if (camera == null)
            return null;
        Vector3[] points = worldPoints.Select(camera.WorldToScreenPoint).ToArray();
        if (points.Length == 0)
            return null;
        return new CaptureRect
        {
            Left = points.Min(point => point.x),
            Right = points.Max(point => point.x),
            Bottom = points.Min(point => point.y),
            Top = points.Max(point => point.y),
        };
    }

    private static CapturePoint ScreenPoint(Vector3 world, Camera camera)
    {
        if (camera == null)
            return null;
        Vector3 point = camera.WorldToScreenPoint(world);
        return new CapturePoint { X = point.x, Y = point.y };
    }

    private static CaptureRect RelativeRect(CaptureRect rect, CapturePoint origin)
    {
        if (rect == null || origin == null)
            return rect;
        rect.Left -= origin.X;
        rect.Right -= origin.X;
        rect.Bottom -= origin.Y;
        rect.Top -= origin.Y;
        return rect;
    }

    private static CapturePoint RelativePoint(CapturePoint point, CapturePoint origin)
    {
        if (point == null || origin == null)
            return point;
        point.X -= origin.X;
        point.Y -= origin.Y;
        return point;
    }

    private static List<CaptureLine> CaptureLines(TMP_Text text, Camera camera,
        CapturePoint origin)
    {
        var lines = new List<CaptureLine>();
        TMP_TextInfo info = text?.textInfo;
        if (info == null)
            return lines;
        int logicalLine = 0;
        int nextCharacter = 0;
        for (int lineIndex = 0; lineIndex < info.lineCount; lineIndex++)
        {
            TMP_LineInfo line = info.lineInfo[lineIndex];
            while (nextCharacter < line.firstCharacterIndex &&
                   nextCharacter < info.characterCount)
            {
                if (info.characterInfo[nextCharacter].character == '\n')
                    logicalLine++;
                nextCharacter++;
            }
            var bounds = new Bounds();
            bounds.SetMinMax(
                new Vector3(line.lineExtents.min.x, line.descender, 0f),
                new Vector3(line.lineExtents.max.x, line.ascender, 0f));
            lines.Add(new CaptureLine
            {
                Index = lineIndex,
                LogicalLine = logicalLine,
                ScreenRect = RelativeRect(
                    ScreenRectForLocalBounds(text.transform, bounds, camera), origin),
            });
        }
        return lines;
    }

    private static IEnumerable<T> FindSceneObjects<T>() where T : Component =>
        Resources.FindObjectsOfTypeAll<T>().Where(value =>
            value != null && value.gameObject.scene.IsValid());

    private static string HierarchyPath(Transform transform)
    {
        var parts = new Stack<string>();
        while (transform != null)
        {
            parts.Push(transform.name);
            transform = transform.parent;
        }
        return string.Join("/", parts.ToArray());
    }

    private static string SafeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string safe = new string((value ?? "page")
            .Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "page" : safe;
    }

    private static void WriteJson(string path, object value) =>
        File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));

    private void QuitIfRequested(int exitCode)
    {
        if (_quitWhenComplete)
            Application.Quit(exitCode);
    }

    private sealed class CaptureRun
    {
        [JsonProperty("started_utc")] public DateTime StartedUtc;
        [JsonProperty("completed_utc")] public DateTime CompletedUtc;
        [JsonProperty("unity_version")] public string UnityVersion;
        [JsonProperty("screen_width")] public int ScreenWidth;
        [JsonProperty("screen_height")] public int ScreenHeight;
        [JsonProperty("discovered_page_count")] public int DiscoveredPageCount;
        [JsonProperty("selected_page_count")] public int SelectedPageCount;
        [JsonProperty("error")] public string Error;
        [JsonProperty("pages")] public List<CapturePage> Pages;
    }

    private sealed class CapturePage
    {
        [JsonProperty("index")] public int Index;
        [JsonProperty("name")] public string Name;
        [JsonProperty("path")] public string Path;
        [JsonProperty("open_and_stabilize_ms")] public double OpenAndStabilizeMilliseconds;
        [JsonProperty("has_copy_buttons")] public bool HasCopyButtons;
        [JsonProperty("has_graphics")] public bool HasGraphics;
        [JsonProperty("risk_reasons")] public List<string> RiskReasons;
        [JsonProperty("font_sizes")] public List<double> FontSizes;
        [JsonProperty("captures")] public List<CaptureFrame> Captures;
    }

    private sealed class CaptureFrame
    {
        [JsonProperty("language")] public string Language;
        [JsonProperty("requested_scroll")] public float RequestedScroll;
        [JsonProperty("actual_scroll")] public float ActualScroll;
        [JsonProperty("moving_part_y")] public float MovingPartY;
        [JsonProperty("root_screen_point")] public CapturePoint RootScreenPoint;
        [JsonProperty("screenshot")] public string Screenshot;
        [JsonProperty("elements")] public List<CaptureElement> Elements;
    }

    private sealed class PageRisk
    {
        internal int CopyButtonCount;
        internal int GraphicCount;
        internal List<string> Reasons = new();
        internal bool ShouldCapture => Reasons.Count > 0;
    }

    private sealed class CaptureElement
    {
        [JsonProperty("instance_id")] public int InstanceId;
        [JsonProperty("kind")] public string Kind;
        [JsonProperty("name")] public string Name;
        [JsonProperty("path")] public string Path;
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)] public string Text;
        [JsonProperty("copy_value", NullValueHandling = NullValueHandling.Ignore)] public string CopyValue;
        [JsonProperty("font_size", NullValueHandling = NullValueHandling.Ignore)] public float? FontSize;
        [JsonProperty("line_count", NullValueHandling = NullValueHandling.Ignore)] public int? LineCount;
        [JsonProperty("lines", NullValueHandling = NullValueHandling.Ignore)] public List<CaptureLine> Lines;
        [JsonProperty("screen_rect", NullValueHandling = NullValueHandling.Ignore)] public CaptureRect ScreenRect;
        [JsonProperty("screen_point", NullValueHandling = NullValueHandling.Ignore)] public CapturePoint ScreenPoint;
        [JsonProperty("anchor_text_instance_id", NullValueHandling = NullValueHandling.Ignore)] public int? AnchorTextInstanceId;
        [JsonProperty("anchor_logical_line", NullValueHandling = NullValueHandling.Ignore)] public int? AnchorLogicalLine;
        [JsonProperty("anchor_wrap_index", NullValueHandling = NullValueHandling.Ignore)] public int? AnchorWrapIndex;
    }

    private sealed class CaptureRect
    {
        [JsonProperty("left")] public float Left;
        [JsonProperty("right")] public float Right;
        [JsonProperty("bottom")] public float Bottom;
        [JsonProperty("top")] public float Top;
    }

    private sealed class CaptureLine
    {
        [JsonProperty("index")] public int Index;
        [JsonProperty("logical_line")] public int LogicalLine;
        [JsonProperty("screen_rect")] public CaptureRect ScreenRect;
    }

    private sealed class CapturePoint
    {
        [JsonProperty("x")] public float X;
        [JsonProperty("y")] public float Y;
    }
}
