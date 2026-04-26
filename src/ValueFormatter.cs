using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal static class ValueFormatter
    {
        public static string FormatValue(object value, Type declaredType)
        {
            IEnumerable enumerable;

            if (value == null)
            {
                return string.Empty;
            }

            if (value is float)
            {
                return ((float)value).ToString("0.####", CultureInfo.InvariantCulture);
            }

            if (value is double)
            {
                return ((double)value).ToString("0.####", CultureInfo.InvariantCulture);
            }

            if (value is decimal)
            {
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);
            }

            if (value is bool)
            {
                return ((bool)value).ToString().ToLowerInvariant();
            }

            if (value is Vector2)
            {
                Vector2 vector = (Vector2)value;
                return FormatVector(vector.x, vector.y);
            }

            if (value is Vector3)
            {
                Vector3 vector = (Vector3)value;
                return FormatVector(vector.x, vector.y, vector.z);
            }

            if (value is Vector4)
            {
                Vector4 vector = (Vector4)value;
                return FormatVector(vector.x, vector.y, vector.z, vector.w);
            }

            if (value is Color)
            {
                Color color = (Color)value;
                return FormatVector(color.r, color.g, color.b, color.a);
            }

            enumerable = value as IEnumerable;
            if (enumerable != null && !(value is string))
            {
                return FormatEnumerable(enumerable);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string FormatEnumerable(IEnumerable enumerable)
        {
            List<string> values = new List<string>();
            int count = 0;

            foreach (object value in enumerable)
            {
                if (count >= 40)
                {
                    values.Add("...");
                    break;
                }

                values.Add(value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture));
                count++;
            }

            return string.Join(",", values.ToArray());
        }

        private static string FormatVector(params float[] values)
        {
            string[] formattedValues = new string[values.Length];
            for (int index = 0; index < values.Length; index++)
            {
                formattedValues[index] = values[index].ToString("0.####", CultureInfo.InvariantCulture);
            }

            return string.Join(",", formattedValues);
        }
    }
}
