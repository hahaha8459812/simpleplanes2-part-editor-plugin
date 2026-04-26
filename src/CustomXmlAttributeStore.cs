using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class CustomXmlAttribute
    {
        public CustomXmlAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }
    }

    internal static class CustomXmlAttributeStore
    {
        private static readonly Dictionary<int, Dictionary<string, string>> AttributesByPart = new Dictionary<int, Dictionary<string, string>>();

        public static List<CustomXmlAttribute> GetAttributes(object partData)
        {
            Dictionary<string, string> attributes;
            List<CustomXmlAttribute> result = new List<CustomXmlAttribute>();

            if (partData == null || !AttributesByPart.TryGetValue(GetKey(partData), out attributes))
            {
                return result;
            }

            foreach (KeyValuePair<string, string> item in attributes)
            {
                result.Add(new CustomXmlAttribute(item.Key, item.Value));
            }

            result.Sort((left, right) => string.Compare(left.Name, right.Name, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static bool SetAttribute(object partData, string name, string value, out string error)
        {
            Dictionary<string, string> attributes;
            error = string.Empty;

            if (partData == null)
            {
                error = "No part selected";
                return false;
            }

            if (!IsValidXmlName(name))
            {
                error = "Invalid XML attribute name";
                return false;
            }

            if (!AttributesByPart.TryGetValue(GetKey(partData), out attributes))
            {
                attributes = new Dictionary<string, string>();
                AttributesByPart[GetKey(partData)] = attributes;
            }

            attributes[name.Trim()] = value ?? string.Empty;
            return true;
        }

        public static void RemoveAttribute(object partData, string name)
        {
            Dictionary<string, string> attributes;
            if (partData == null || string.IsNullOrEmpty(name) || !AttributesByPart.TryGetValue(GetKey(partData), out attributes))
            {
                return;
            }

            attributes.Remove(name);
        }

        public static void ApplyToXml(object partData, XElement element)
        {
            Dictionary<string, string> attributes;
            if (partData == null || element == null || !AttributesByPart.TryGetValue(GetKey(partData), out attributes))
            {
                return;
            }

            foreach (KeyValuePair<string, string> attribute in attributes)
            {
                element.SetAttributeValue(attribute.Key, attribute.Value);
            }
        }

        private static int GetKey(object partData)
        {
            Object unityObject = partData as Object;
            return unityObject != null ? unityObject.GetInstanceID() : RuntimeHelpers.GetHashCode(partData);
        }

        private static bool IsValidXmlName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            try
            {
                XName.Get(name.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
