using System;
using System.Collections.Generic;
using System.Text;

namespace SimplePlanes2PartEditor
{
    internal static class SimpleJson
    {
        public static Dictionary<string, string> ReadFlatObject(string json)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int index = 0;

            if (string.IsNullOrEmpty(json))
            {
                return values;
            }

            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != '{')
            {
                return values;
            }

            index++;
            while (index < json.Length)
            {
                string key;
                string value;

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == '}')
                {
                    break;
                }

                if (!TryReadString(json, ref index, out key))
                {
                    break;
                }

                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index] != ':')
                {
                    break;
                }

                index++;
                SkipWhitespace(json, ref index);
                if (!TryReadValue(json, ref index, out value))
                {
                    break;
                }

                values[key] = value;
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                }
            }

            return values;
        }

        public static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static bool TryReadValue(string json, ref int index, out string value)
        {
            if (index < json.Length && json[index] == '"')
            {
                return TryReadString(json, ref index, out value);
            }

            int startIndex = index;
            while (index < json.Length && json[index] != ',' && json[index] != '}')
            {
                index++;
            }

            value = json.Substring(startIndex, index - startIndex).Trim();
            return true;
        }

        private static bool TryReadString(string json, ref int index, out string value)
        {
            StringBuilder builder = new StringBuilder();
            value = string.Empty;

            if (index >= json.Length || json[index] != '"')
            {
                return false;
            }

            index++;
            while (index < json.Length)
            {
                char character = json[index++];
                if (character == '"')
                {
                    value = builder.ToString();
                    return true;
                }

                if (character == '\\' && index < json.Length)
                {
                    char escaped = json[index++];
                    switch (escaped)
                    {
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case '\\':
                            builder.Append('\\');
                            break;
                        case '"':
                            builder.Append('"');
                            break;
                        default:
                            builder.Append(escaped);
                            break;
                    }
                }
                else
                {
                    builder.Append(character);
                }
            }

            return false;
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }
}
