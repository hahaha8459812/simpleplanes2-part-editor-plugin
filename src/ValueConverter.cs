using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal static class ValueConverter
    {
        public static bool IsEditableType(Type type)
        {
            Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

            if (effectiveType.IsEnum || effectiveType == typeof(string) || effectiveType == typeof(bool))
            {
                return true;
            }

            if (effectiveType == typeof(byte) || effectiveType == typeof(sbyte) ||
                effectiveType == typeof(short) || effectiveType == typeof(ushort) ||
                effectiveType == typeof(int) || effectiveType == typeof(uint) ||
                effectiveType == typeof(long) || effectiveType == typeof(ulong) ||
                effectiveType == typeof(float) || effectiveType == typeof(double) ||
                effectiveType == typeof(decimal))
            {
                return true;
            }

            if (effectiveType == typeof(Vector2) || effectiveType == typeof(Vector3) || effectiveType == typeof(Vector4) || effectiveType == typeof(Color))
            {
                return true;
            }

            if (effectiveType.IsArray)
            {
                return IsEditableScalar(effectiveType.GetElementType());
            }

            if (typeof(IList).IsAssignableFrom(effectiveType) && effectiveType.IsGenericType)
            {
                return IsEditableScalar(effectiveType.GetGenericArguments()[0]);
            }

            return false;
        }

        public static bool TryConvert(string text, Type targetType, object currentValue, out object value, out string error)
        {
            Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            value = null;
            error = string.Empty;

            if (Nullable.GetUnderlyingType(targetType) != null && string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            try
            {
                if (effectiveType == typeof(string))
                {
                    value = text ?? string.Empty;
                    return true;
                }

                if (effectiveType == typeof(bool))
                {
                    bool boolValue;
                    if (bool.TryParse(text, out boolValue))
                    {
                        value = boolValue;
                        return true;
                    }

                    if (text == "1" || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase))
                    {
                        value = true;
                        return true;
                    }

                    if (text == "0" || string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
                    {
                        value = false;
                        return true;
                    }

                    error = "Expected true/false";
                    return false;
                }

                if (effectiveType.IsEnum)
                {
                    value = Enum.Parse(effectiveType, text, true);
                    return true;
                }

                if (TryConvertNumber(text, effectiveType, out value))
                {
                    return true;
                }

                if (effectiveType == typeof(Vector2))
                {
                    float[] values;
                    if (TryParseFloatList(text, 2, out values, out error))
                    {
                        value = new Vector2(values[0], values[1]);
                        return true;
                    }

                    return false;
                }

                if (effectiveType == typeof(Vector3))
                {
                    float[] values;
                    if (TryParseFloatList(text, 3, out values, out error))
                    {
                        value = new Vector3(values[0], values[1], values[2]);
                        return true;
                    }

                    return false;
                }

                if (effectiveType == typeof(Vector4))
                {
                    float[] values;
                    if (TryParseFloatList(text, 4, out values, out error))
                    {
                        value = new Vector4(values[0], values[1], values[2], values[3]);
                        return true;
                    }

                    return false;
                }

                if (effectiveType == typeof(Color))
                {
                    float[] values;
                    if (TryParseFloatList(text, 4, out values, out error))
                    {
                        value = new Color(values[0], values[1], values[2], values[3]);
                        return true;
                    }

                    return false;
                }

                if (effectiveType.IsArray)
                {
                    return TryConvertArray(text, effectiveType.GetElementType(), out value, out error);
                }

                if (typeof(IList).IsAssignableFrom(effectiveType) && effectiveType.IsGenericType)
                {
                    return TryConvertList(text, effectiveType, currentValue, out value, out error);
                }
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            error = "Unsupported type";
            return false;
        }

        private static bool IsEditableScalar(Type type)
        {
            Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;
            return effectiveType.IsEnum || effectiveType == typeof(string) || effectiveType == typeof(bool) ||
                   effectiveType == typeof(byte) || effectiveType == typeof(sbyte) ||
                   effectiveType == typeof(short) || effectiveType == typeof(ushort) ||
                   effectiveType == typeof(int) || effectiveType == typeof(uint) ||
                   effectiveType == typeof(long) || effectiveType == typeof(ulong) ||
                   effectiveType == typeof(float) || effectiveType == typeof(double) ||
                   effectiveType == typeof(decimal);
        }

        private static bool TryConvertNumber(string text, Type type, out object value)
        {
            NumberStyles styles = NumberStyles.Float;
            CultureInfo culture = CultureInfo.InvariantCulture;
            value = null;

            if (type == typeof(byte)) { byte parsed; if (byte.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(sbyte)) { sbyte parsed; if (sbyte.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(short)) { short parsed; if (short.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(ushort)) { ushort parsed; if (ushort.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(int)) { int parsed; if (int.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(uint)) { uint parsed; if (uint.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(long)) { long parsed; if (long.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(ulong)) { ulong parsed; if (ulong.TryParse(text, NumberStyles.Integer, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(float)) { float parsed; if (float.TryParse(text, styles, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(double)) { double parsed; if (double.TryParse(text, styles, culture, out parsed)) { value = parsed; return true; } return false; }
            if (type == typeof(decimal)) { decimal parsed; if (decimal.TryParse(text, styles, culture, out parsed)) { value = parsed; return true; } return false; }

            return false;
        }

        private static bool TryParseFloatList(string text, int expectedCount, out float[] values, out string error)
        {
            string[] parts = SplitList(text);
            values = new float[expectedCount];
            error = string.Empty;

            if (parts.Length != expectedCount)
            {
                error = "Expected " + expectedCount + " comma-separated values";
                return false;
            }

            for (int index = 0; index < parts.Length; index++)
            {
                if (!float.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index]))
                {
                    error = "Invalid number: " + parts[index];
                    return false;
                }
            }

            return true;
        }

        private static bool TryConvertArray(string text, Type elementType, out object value, out string error)
        {
            string[] parts = SplitList(text);
            Array array = Array.CreateInstance(elementType, parts.Length);
            value = null;
            error = string.Empty;

            for (int index = 0; index < parts.Length; index++)
            {
                object item;
                if (!TryConvert(parts[index], elementType, null, out item, out error))
                {
                    return false;
                }

                array.SetValue(item, index);
            }

            value = array;
            return true;
        }

        private static bool TryConvertList(string text, Type listType, object currentValue, out object value, out string error)
        {
            Type elementType = listType.GetGenericArguments()[0];
            string[] parts = SplitList(text);
            IList list = currentValue as IList;
            value = null;
            error = string.Empty;

            if (list == null || list.IsReadOnly)
            {
                try
                {
                    list = (IList)Activator.CreateInstance(listType);
                }
                catch
                {
                    error = "List cannot be created";
                    return false;
                }
            }
            else
            {
                list.Clear();
            }

            foreach (string part in parts)
            {
                object item;
                if (!TryConvert(part, elementType, null, out item, out error))
                {
                    return false;
                }

                list.Add(item);
            }

            value = list;
            return true;
        }

        private static string[] SplitList(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new string[0];
            }

            string[] parts = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < parts.Length; index++)
            {
                parts[index] = parts[index].Trim();
            }

            return parts;
        }
    }
}
