using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace SimplePlanes2PartEditor
{
    internal sealed class JFuselageShapeEditor
    {
        private static readonly JFuselageShapeRow[] Rows =
        {
            new JFuselageShapeRow("SectionA.Size", "SectionA.Size", "float2", "这一端截面的宽和高。A、B 不同，机身就会开始收缩或展开。"),
            new JFuselageShapeRow("SectionA.CornerRadii", "SectionA.CornerRadii", "float4", "四个角的圆角大小。0 是硬角，数值越大越圆。"),
            new JFuselageShapeRow("SectionA.CornerStretch", "SectionA.CornerStretch", "bool4", "四个角要不要跟着截面比例拉伸。关掉更规整，打开更流线。"),
            new JFuselageShapeRow("SectionA.CornerSamples", "SectionA.CornerSamples", "int4", "四个角的细分数。2 会更像折面，数值越大越圆滑。"),
            new JFuselageShapeRow("SectionA.EdgeCurvature", "SectionA.EdgeCurvature", "float4", "四条边的弯曲强度。0 基本是直边，1 接近最大弯曲。"),
            new JFuselageShapeRow("SectionA.EdgeSamples", "SectionA.EdgeSamples", "int4", "四条边的细分数。和弯曲强度一起决定边缘是硬还是顺。"),
            new JFuselageShapeRow("SectionA.Thickness", "SectionA.Thickness", "float", "空心机身的壁厚。实心样式可能不会明显用到它。"),
            new JFuselageShapeRow("SectionA.Trapezium", "SectionA.Trapezium", "float", "截面的梯形偏移量。0 表示不做梯形变化。"),
            new JFuselageShapeRow("SectionA.Smoothing", "SectionA.Smoothing", "bool", "这一端是否参与平滑过渡。"),
            new JFuselageShapeRow("SectionA.Cutting", "SectionA.Cutting", "decimal?4", "四个方向的切割值。空值可以留空，例如 ,,0.16,。"),
            new JFuselageShapeRow("SectionB.Size", "SectionB.Size", "float2", "另一端截面的宽和高。A、B 不同，机身就会开始收缩或展开。"),
            new JFuselageShapeRow("SectionB.CornerRadii", "SectionB.CornerRadii", "float4", "四个角的圆角大小。0 是硬角，数值越大越圆。"),
            new JFuselageShapeRow("SectionB.CornerStretch", "SectionB.CornerStretch", "bool4", "四个角要不要跟着截面比例拉伸。关掉更规整，打开更流线。"),
            new JFuselageShapeRow("SectionB.CornerSamples", "SectionB.CornerSamples", "int4", "四个角的细分数。2 会更像折面，数值越大越圆滑。"),
            new JFuselageShapeRow("SectionB.EdgeCurvature", "SectionB.EdgeCurvature", "float4", "四条边的弯曲强度。0 基本是直边，1 接近最大弯曲。"),
            new JFuselageShapeRow("SectionB.EdgeSamples", "SectionB.EdgeSamples", "int4", "四条边的细分数。和弯曲强度一起决定边缘是硬还是顺。"),
            new JFuselageShapeRow("SectionB.Thickness", "SectionB.Thickness", "float", "空心机身的壁厚。实心样式可能不会明显用到它。"),
            new JFuselageShapeRow("SectionB.Trapezium", "SectionB.Trapezium", "float", "截面的梯形偏移量。0 表示不做梯形变化。"),
            new JFuselageShapeRow("SectionB.Smoothing", "SectionB.Smoothing", "bool", "这一端是否参与平滑过渡。"),
            new JFuselageShapeRow("SectionB.Cutting", "SectionB.Cutting", "decimal?4", "四个方向的切割值。空值可以留空，例如 ,,0.16,。")
        };

        public bool IsSupported(object target)
        {
            return target != null && string.Equals(target.GetType().Name, "JFuselageData", StringComparison.Ordinal);
        }

        public IEnumerable<JFuselageShapeRow> GetRows()
        {
            return Rows;
        }

        public bool TryReadValue(object target, string key, out string value)
        {
            value = string.Empty;
            try
            {
                if (!IsSupported(target) || string.IsNullOrEmpty(key))
                {
                    return false;
                }

                if (key.EndsWith(".Cutting", StringComparison.Ordinal))
                {
                    value = FormatCutting(GetCutting(target, GetSectionIndex(key)));
                    return true;
                }

                if (key.EndsWith(".CornerStretch", StringComparison.Ordinal))
                {
                    value = FormatCornerStretch(GetCornerStretch(target, GetSectionName(key)));
                    return true;
                }

                if (key.EndsWith(".Smoothing", StringComparison.Ordinal))
                {
                    value = GetSmoothing(target, GetSectionIndex(key)).ToString().ToLowerInvariant();
                    return true;
                }

                object section = GetSection(target, GetSectionName(key));
                string memberName = GetSectionMemberName(key);
                FieldInfo field = section.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                {
                    return false;
                }

                value = FormatValue(field.GetValue(section));
                return true;
            }
            catch
            {
                value = string.Empty;
                return false;
            }
        }

        public bool TryApplyValue(object target, string key, string text, out string error)
        {
            error = string.Empty;

            try
            {
                if (!IsSupported(target))
                {
                    error = "Not a JFuselageData object";
                    return false;
                }

                if (key.EndsWith(".Cutting", StringComparison.Ordinal))
                {
                    return TryApplyCutting(target, GetSectionIndex(key), text, out error);
                }

                if (key.EndsWith(".CornerStretch", StringComparison.Ordinal))
                {
                    return TryApplyCornerStretch(target, GetSectionName(key), text, out error);
                }

                if (key.EndsWith(".Smoothing", StringComparison.Ordinal))
                {
                    return TryApplySmoothing(target, GetSectionIndex(key), text, out error);
                }

                return TryApplySectionMember(target, key, text, out error);
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        public IEnumerable<string> GetRuntimeRefreshMemberNames(string key)
        {
            List<string> names = new List<string>();
            string sectionName = GetSectionName(key);

            AddUnique(names, sectionName);
            AddUnique(names, "_" + char.ToLowerInvariant(sectionName[0]) + sectionName.Substring(1));

            if (key.EndsWith(".Cutting", StringComparison.Ordinal))
            {
                AddUnique(names, GetSectionIndex(key) == 0 ? "_cuttingA" : "_cuttingB");
            }
            else if (key.EndsWith(".CornerStretch", StringComparison.Ordinal))
            {
                AddUnique(names, "CornersStretch");
            }
            else if (key.EndsWith(".Smoothing", StringComparison.Ordinal))
            {
                AddUnique(names, GetSectionIndex(key) == 0 ? "_smoothingA" : "_smoothingB");
            }
            else
            {
                AddUnique(names, GetSectionMemberName(key));
            }

            return names;
        }

        private static bool TryApplySectionMember(object target, string key, string text, out string error)
        {
            object section = GetSection(target, GetSectionName(key));
            string memberName = GetSectionMemberName(key);
            FieldInfo field = section.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object convertedValue;

            if (field == null)
            {
                error = "Missing section member: " + memberName;
                return false;
            }

            if (!TryParseValue(text, field.FieldType, out convertedValue, out error))
            {
                return false;
            }

            field.SetValue(section, convertedValue);
            SetSection(target, GetSectionName(key), section);
            InvokeNoArgMethod(target, "RaiseChange");
            InvokeNoArgMethod(target, "OnShapeDataChanged");
            InvokeNoArgMethod(target, "OnMeshRegenerated");
            return true;
        }

        private static bool TryApplyCutting(object target, int sectionIndex, string text, out string error)
        {
            object cutting = GetCutting(target, sectionIndex);
            if (!TrySetNullableDecimal4(cutting, text, out error))
            {
                return false;
            }

            MethodInfo setCutting = target.GetType().GetMethod("SetCutting", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (setCutting == null)
            {
                error = "Missing SetCutting";
                return false;
            }

            setCutting.Invoke(target, new[] { (object)sectionIndex, cutting });
            InvokeNoArgMethod(target, "RaiseChange");
            InvokeNoArgMethod(target, "OnShapeDataChanged");
            InvokeNoArgMethod(target, "OnMeshRegenerated");
            return true;
        }

        private static bool TryApplyCornerStretch(object target, string sectionName, string text, out string error)
        {
            object section = GetSection(target, sectionName);
            FieldInfo field = section.GetType().GetField("CornersStretch", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object convertedValue;

            if (field == null)
            {
                error = "Missing section member: CornersStretch";
                return false;
            }

            if (!TryParseCornerStretch(field.FieldType, text, out convertedValue, out error))
            {
                return false;
            }

            field.SetValue(section, convertedValue);
            SetSection(target, sectionName, section);
            InvokeNoArgMethod(target, "RaiseChange");
            InvokeNoArgMethod(target, "OnShapeDataChanged");
            InvokeNoArgMethod(target, "OnMeshRegenerated");
            error = string.Empty;
            return true;
        }

        private static bool TryApplySmoothing(object target, int sectionIndex, string text, out string error)
        {
            bool smoothing;
            MethodInfo setSmoothing = target.GetType().GetMethod("SetSmoothing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (!TryParseBool(text, out smoothing))
            {
                error = "Expected true/false";
                return false;
            }

            if (setSmoothing == null)
            {
                error = "Missing SetSmoothing";
                return false;
            }

            setSmoothing.Invoke(target, new[] { (object)sectionIndex, smoothing });
            InvokeNoArgMethod(target, "RaiseChange");
            InvokeNoArgMethod(target, "OnShapeDataChanged");
            InvokeNoArgMethod(target, "OnMeshRegenerated");
            error = string.Empty;
            return true;
        }

        private static object GetSection(object target, string sectionName)
        {
            PropertyInfo property = target.GetType().GetProperty(sectionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(target, null);
            }

            FieldInfo field = target.GetType().GetField("_" + char.ToLowerInvariant(sectionName[0]) + sectionName.Substring(1), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            throw new InvalidOperationException("Missing section: " + sectionName);
        }

        private static void SetSection(object target, string sectionName, object section)
        {
            PropertyInfo property = target.GetType().GetProperty(sectionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter != null)
            {
                property.SetValue(target, section, null);
                return;
            }

            FieldInfo field = target.GetType().GetField("_" + char.ToLowerInvariant(sectionName[0]) + sectionName.Substring(1), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Missing writable section: " + sectionName);
            }

            field.SetValue(target, section);
        }

        private static object GetCutting(object target, int sectionIndex)
        {
            MethodInfo method = target.GetType().GetMethod("GetCutting", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                return method.Invoke(target, new[] { (object)sectionIndex });
            }

            FieldInfo field = target.GetType().GetField(sectionIndex == 0 ? "_cuttingA" : "_cuttingB", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Missing cutting field");
            }

            return field.GetValue(target);
        }

        private static bool GetSmoothing(object target, int sectionIndex)
        {
            MethodInfo method = target.GetType().GetMethod("GetSmoothing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                return (bool)method.Invoke(target, new[] { (object)sectionIndex });
            }

            FieldInfo field = target.GetType().GetField(sectionIndex == 0 ? "_smoothingA" : "_smoothingB", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException("Missing smoothing field");
            }

            return (bool)field.GetValue(target);
        }

        private static object GetCornerStretch(object target, string sectionName)
        {
            object section = GetSection(target, sectionName);
            FieldInfo field = section.GetType().GetField("CornersStretch", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new InvalidOperationException("Missing section member: CornersStretch");
            }

            return field.GetValue(section);
        }

        private static bool TryParseValue(string text, Type valueType, out object value, out string error)
        {
            string typeName = valueType.FullName ?? valueType.Name;
            value = null;
            error = string.Empty;

            if (typeName == "Unity.Mathematics.float2")
            {
                return TryParseStructFields(valueType, text, new[] { "x", "y" }, false, out value, out error);
            }

            if (typeName == "Unity.Mathematics.float4")
            {
                return TryParseStructFields(valueType, text, new[] { "x", "y", "z", "w" }, false, out value, out error);
            }

            if (typeName == "Unity.Mathematics.int4")
            {
                return TryParseStructFields(valueType, text, new[] { "x", "y", "z", "w" }, true, out value, out error);
            }

            if (valueType == typeof(float))
            {
                float parsed;
                if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    value = parsed;
                    return true;
                }

                error = "Invalid float";
                return false;
            }

            error = "Unsupported type: " + valueType.Name;
            return false;
        }

        private static bool TryParseCornerStretch(Type stretchType, string text, out object value, out string error)
        {
            string[] parts = SplitListKeepEmpty(text);
            value = null;
            error = string.Empty;

            if (parts.Length != 4)
            {
                error = "Expected 4 comma-separated values";
                return false;
            }

            object instance = Activator.CreateInstance(stretchType);
            string[] fieldNames = { "x", "y", "z", "w" };

            for (int index = 0; index < fieldNames.Length; index++)
            {
                FieldInfo field = stretchType.GetField(fieldNames[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                {
                    error = "Missing corner stretch field: " + fieldNames[index];
                    return false;
                }

                bool enabled;
                if (!TryParseBool(parts[index], out enabled))
                {
                    error = "Expected true/false at value " + (index + 1);
                    return false;
                }

                field.SetValue(instance, enabled ? 1f : 0f);
            }

            value = instance;
            return true;
        }

        private static bool TryParseStructFields(Type structType, string text, string[] fieldNames, bool integer, out object value, out string error)
        {
            string[] parts = SplitListKeepEmpty(text);
            value = null;
            error = string.Empty;

            if (parts.Length != fieldNames.Length)
            {
                error = "Expected " + fieldNames.Length + " comma-separated values";
                return false;
            }

            object instance = Activator.CreateInstance(structType);
            for (int index = 0; index < fieldNames.Length; index++)
            {
                FieldInfo field = structType.GetField(fieldNames[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                {
                    error = "Missing vector field: " + fieldNames[index];
                    return false;
                }

                if (integer)
                {
                    int parsed;
                    if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        error = "Invalid integer: " + parts[index];
                        return false;
                    }

                    field.SetValue(instance, parsed);
                }
                else
                {
                    float parsed;
                    if (!float.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    {
                        error = "Invalid number: " + parts[index];
                        return false;
                    }

                    field.SetValue(instance, parsed);
                }
            }

            value = instance;
            return true;
        }

        private static bool TrySetNullableDecimal4(object cutting, string text, out string error)
        {
            string[] parts = SplitListKeepEmpty(text);
            string[] fieldNames = { "x", "y", "z", "w" };
            Type cuttingType = cutting.GetType();

            error = string.Empty;
            if (parts.Length != 4)
            {
                error = "Expected 4 comma-separated values. Empty values are allowed.";
                return false;
            }

            for (int index = 0; index < fieldNames.Length; index++)
            {
                FieldInfo field = cuttingType.GetField(fieldNames[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                {
                    error = "Missing cutting field: " + fieldNames[index];
                    return false;
                }

                if (string.IsNullOrWhiteSpace(parts[index]))
                {
                    field.SetValue(cutting, null);
                    continue;
                }

                decimal parsed;
                if (!decimal.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    error = "Invalid decimal: " + parts[index];
                    return false;
                }

                field.SetValue(cutting, parsed);
            }

            return true;
        }

        private static string FormatCornerStretch(object value)
        {
            string[] fieldNames = { "x", "y", "z", "w" };
            string[] parts = new string[fieldNames.Length];
            Type type = value.GetType();

            for (int index = 0; index < fieldNames.Length; index++)
            {
                FieldInfo field = type.GetField(fieldNames[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object fieldValue = field == null ? null : field.GetValue(value);
                float stretch = fieldValue == null ? 0f : Convert.ToSingle(fieldValue, CultureInfo.InvariantCulture);
                parts[index] = stretch > 0.5f ? "true" : "false";
            }

            return string.Join(",", parts);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            Type type = value.GetType();
            string typeName = type.FullName ?? type.Name;
            if (typeName == "Unity.Mathematics.float4")
            {
                return FormatStructFields(value, new[] { "x", "y", "z", "w" });
            }

            if (typeName == "Unity.Mathematics.float2")
            {
                return FormatStructFields(value, new[] { "x", "y" });
            }

            if (typeName == "Unity.Mathematics.int4")
            {
                return FormatStructFields(value, new[] { "x", "y", "z", "w" });
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string FormatStructFields(object value, string[] fieldNames)
        {
            string[] parts = new string[fieldNames.Length];
            Type type = value.GetType();
            for (int index = 0; index < fieldNames.Length; index++)
            {
                FieldInfo field = type.GetField(fieldNames[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object fieldValue = field == null ? null : field.GetValue(value);
                parts[index] = fieldValue == null ? string.Empty : Convert.ToString(fieldValue, CultureInfo.InvariantCulture);
            }

            return string.Join(",", parts);
        }

        private static string FormatCutting(object cutting)
        {
            string[] fieldNames = { "x", "y", "z", "w" };
            string[] parts = new string[fieldNames.Length];
            Type type = cutting.GetType();

            for (int index = 0; index < fieldNames.Length; index++)
            {
                FieldInfo field = type.GetField(fieldNames[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object fieldValue = field == null ? null : field.GetValue(cutting);
                parts[index] = fieldValue == null ? string.Empty : Convert.ToString(fieldValue, CultureInfo.InvariantCulture);
            }

            return string.Join(",", parts);
        }

        private static string GetSectionName(string key)
        {
            return key != null && key.StartsWith("SectionB.", StringComparison.Ordinal) ? "SectionB" : "SectionA";
        }

        private static int GetSectionIndex(string key)
        {
            return key != null && key.StartsWith("SectionB.", StringComparison.Ordinal) ? 1 : 0;
        }

        private static string GetSectionMemberName(string key)
        {
            int separator = key == null ? -1 : key.IndexOf('.');
            return separator < 0 ? string.Empty : key.Substring(separator + 1);
        }

        private static string[] SplitListKeepEmpty(string text)
        {
            string[] parts = (text ?? string.Empty).Split(',');
            for (int index = 0; index < parts.Length; index++)
            {
                parts[index] = parts[index].Trim();
            }

            return parts;
        }

        private static bool TryParseBool(string text, out bool value)
        {
            if (bool.TryParse(text, out value))
            {
                return true;
            }

            if (string.Equals(text, "1", StringComparison.Ordinal) || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (string.Equals(text, "0", StringComparison.Ordinal) || string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            value = false;
            return false;
        }

        private static void InvokeNoArgMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method == null)
            {
                return;
            }

            method.Invoke(target, null);
        }

        private static void AddUnique(List<string> names, string name)
        {
            if (!string.IsNullOrEmpty(name) && !names.Contains(name))
            {
                names.Add(name);
            }
        }
    }

    internal sealed class JFuselageShapeRow
    {
        public JFuselageShapeRow(string key, string label, string typeName, string description)
        {
            Key = key;
            Label = label;
            TypeName = typeName;
            Description = description;
        }

        public string Key { get; private set; }

        public string Label { get; private set; }

        public string TypeName { get; private set; }

        public string Description { get; private set; }

        public bool Matches(string searchTerm)
        {
            return string.IsNullOrEmpty(searchTerm) ||
                   Label.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   TypeName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   Description.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

