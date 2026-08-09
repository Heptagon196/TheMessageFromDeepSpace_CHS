using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DeepSpaceChinese;

internal static class ReflectionPath
{
    private static readonly Regex SegmentPattern =
        new(@"^([^\[]+)(?:\[(\d+)\])?$", RegexOptions.Compiled);

    private readonly struct Segment
    {
        public readonly string FieldName;
        public readonly int? Index;

        public Segment(string fieldName, int? index)
        {
            FieldName = fieldName;
            Index = index;
        }
    }

    public static bool TryGetValue(object root, string path, out object value)
    {
        value = root;
        if (root == null || !TryParse(path, out List<Segment> segments))
            return false;
        foreach (Segment segment in segments)
        {
            if (!TryReadSegment(value, segment, out value))
                return false;
        }
        return true;
    }

    public static bool TrySetValue(object root, string path, object value)
    {
        if (root == null || !TryParse(path, out List<Segment> segments) || segments.Count == 0)
            return false;
        return TrySetRecursive(root, segments, 0, value, out _);
    }

    private static bool TrySetRecursive(object container, IReadOnlyList<Segment> segments,
        int position, object newValue, out object changedContainer)
    {
        changedContainer = container;
        if (container == null)
            return false;
        Segment segment = segments[position];
        FieldInfo field = FindField(container.GetType(), segment.FieldName);
        if (field == null)
            return false;
        object fieldValue = field.GetValue(container);

        if (segment.Index.HasValue)
        {
            if (fieldValue is not IList list || segment.Index.Value < 0 ||
                segment.Index.Value >= list.Count)
                return false;
            int index = segment.Index.Value;
            if (position == segments.Count - 1)
                list[index] = newValue;
            else
            {
                object element = list[index];
                if (!TrySetRecursive(element, segments, position + 1, newValue,
                        out object changedElement))
                    return false;
                list[index] = changedElement;
            }
            field.SetValue(container, fieldValue);
        }
        else if (position == segments.Count - 1)
        {
            field.SetValue(container, newValue);
        }
        else
        {
            if (!TrySetRecursive(fieldValue, segments, position + 1, newValue,
                    out object changedField))
                return false;
            field.SetValue(container, changedField);
        }
        changedContainer = container;
        return true;
    }

    private static bool TryReadSegment(object container, Segment segment, out object value)
    {
        value = null;
        if (container == null)
            return false;
        FieldInfo field = FindField(container.GetType(), segment.FieldName);
        if (field == null)
            return false;
        value = field.GetValue(container);
        if (!segment.Index.HasValue)
            return true;
        if (value is not IList list || segment.Index.Value < 0 || segment.Index.Value >= list.Count)
            return false;
        value = list[segment.Index.Value];
        return true;
    }

    private static FieldInfo FindField(Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        return null;
    }

    private static bool TryParse(string path, out List<Segment> segments)
    {
        segments = new List<Segment>();
        foreach (string raw in (path ?? string.Empty).Split('.'))
        {
            Match match = SegmentPattern.Match(raw);
            if (!match.Success)
                return false;
            int? index = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : null;
            segments.Add(new Segment(match.Groups[1].Value, index));
        }
        return segments.Count > 0;
    }
}
