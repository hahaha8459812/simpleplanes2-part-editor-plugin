using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class ReflectionMemberScanner
    {
        private readonly int _maxMembersPerGroup;
        private readonly InspectableMemberDescriptionProvider _descriptionProvider;

        public ReflectionMemberScanner(int maxMembersPerGroup, bool showRuntimeCacheMembers, InspectableMemberDescriptionProvider descriptionProvider)
        {
            _maxMembersPerGroup = Math.Max(20, maxMembersPerGroup);
            ShowRuntimeCacheMembers = showRuntimeCacheMembers;
            _descriptionProvider = descriptionProvider;
        }

        public bool ShowRuntimeCacheMembers { get; set; }

        public List<InspectableMember> ScanDisplayableMembers(object target)
        {
            List<InspectableMember> members = new List<InspectableMember>();
            HashSet<string> seenNames = new HashSet<string>(StringComparer.Ordinal);

            if (target == null)
            {
                return members;
            }

            AddProperties(target, members, seenNames);
            AddFields(target, members, seenNames, ShowRuntimeCacheMembers);
            members.Sort(CompareMembers);

            if (members.Count > _maxMembersPerGroup)
            {
                members.RemoveRange(_maxMembersPerGroup, members.Count - _maxMembersPerGroup);
            }

            return members;
        }

        private static int CompareMembers(InspectableMember left, InspectableMember right)
        {
            int editableCompare = right.CanWrite.CompareTo(left.CanWrite);
            if (editableCompare != 0)
            {
                return editableCompare;
            }

            int accessCompare = string.Compare(left.Access, right.Access, StringComparison.OrdinalIgnoreCase);
            if (accessCompare != 0)
            {
                return accessCompare;
            }

            return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        }

        private void AddProperties(object target, List<InspectableMember> members, HashSet<string> seenNames)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo[] properties = target.GetType().GetProperties(flags);

            foreach (PropertyInfo property in properties)
            {
                object value;
                FieldInfo backingField;
                string attributes;
                InspectableMember member;
                if (!CanReadProperty(property) || !IsDisplayableType(property.PropertyType) || !seenNames.Add("P:" + property.Name))
                {
                    continue;
                }

                backingField = FindBackingField(target.GetType(), property.Name);
                value = TryGetValue(() => property.GetValue(target, null));
                attributes = GetAttributeNames(property);
                member = new InspectableMember(
                    target,
                    property,
                    backingField,
                    property.Name,
                    GetFriendlyTypeName(property.PropertyType),
                    GetPropertyAccess(property, backingField),
                    ValueFormatter.FormatValue(value, property.PropertyType),
                    attributes);
                member.SetDescription(_descriptionProvider.GetDescription(target.GetType(), property));
                members.Add(member);
            }
        }

        private void AddFields(object target, List<InspectableMember> members, HashSet<string> seenNames, bool showRuntimeCacheMembers)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = target.GetType().GetFields(flags);

            foreach (FieldInfo field in fields)
            {
                object value;
                string attributes;
                InspectableMember member;
                if (ShouldSkipField(field, showRuntimeCacheMembers) || field.IsStatic || !IsDisplayableType(field.FieldType) || !seenNames.Add("F:" + field.Name))
                {
                    continue;
                }

                value = TryGetValue(() => field.GetValue(target));
                attributes = GetAttributeNames(field);
                member = new InspectableMember(
                    target,
                    field,
                    field.Name,
                    GetFriendlyTypeName(field.FieldType),
                    field.IsPublic ? "public field" : "private field",
                    ValueFormatter.FormatValue(value, field.FieldType),
                    attributes);
                member.SetDescription(_descriptionProvider.GetDescription(target.GetType(), field));
                members.Add(member);
            }
        }

        private static bool CanReadProperty(PropertyInfo property)
        {
            MethodInfo getter = property.GetGetMethod(true);
            return getter != null &&
                   !getter.IsStatic &&
                   property.GetIndexParameters().Length == 0 &&
                   !ShouldSkipMemberName(property.Name);
        }

        private static bool ShouldSkipMemberName(string name)
        {
            return string.IsNullOrEmpty(name) ||
                   name.StartsWith("__", StringComparison.Ordinal);
        }

        private static bool ShouldSkipField(FieldInfo field, bool showRuntimeCacheMembers)
        {
            if (field == null || string.IsNullOrEmpty(field.Name) || field.Name.StartsWith("__", StringComparison.Ordinal))
            {
                return true;
            }

            if (!showRuntimeCacheMembers && IsRuntimeCacheFieldName(field.Name))
            {
                return true;
            }

            // Auto-property backing fields are represented by their property row
            // when possible, so keep the list readable and avoid duplicate rows.
            return field.Name.StartsWith("<", StringComparison.Ordinal) && field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal);
        }

        private static bool IsRuntimeCacheFieldName(string fieldName)
        {
            return string.Equals(fieldName, "_loadedMassCached", StringComparison.Ordinal) ||
                   string.Equals(fieldName, "_recalculateLoadedMass", StringComparison.Ordinal) ||
                   fieldName.IndexOf("cache", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   fieldName.IndexOf("cached", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   fieldName.IndexOf("recalculate", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsDisplayableType(Type type)
        {
            Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;

            if (ValueConverter.IsEditableType(type))
            {
                return true;
            }

            if (effectiveType.IsEnum || effectiveType.IsPrimitive || effectiveType == typeof(string) || effectiveType == typeof(decimal))
            {
                return true;
            }

            if (effectiveType == typeof(Vector2) || effectiveType == typeof(Vector3) || effectiveType == typeof(Vector4) || effectiveType == typeof(Color))
            {
                return true;
            }

            if (typeof(Delegate).IsAssignableFrom(effectiveType) || typeof(UnityEngine.Object).IsAssignableFrom(effectiveType))
            {
                return false;
            }

            if (typeof(IEnumerable).IsAssignableFrom(effectiveType) && effectiveType != typeof(string))
            {
                return true;
            }

            return false;
        }

        private static object TryGetValue(Func<object> readValue)
        {
            try
            {
                return readValue();
            }
            catch (Exception exception)
            {
                return "<read failed: " + exception.GetType().Name + ">";
            }
        }

        private static string GetFriendlyTypeName(Type type)
        {
            Type effectiveType = Nullable.GetUnderlyingType(type) ?? type;
            if (!effectiveType.IsGenericType)
            {
                return effectiveType.Name;
            }

            return effectiveType.Name.Substring(0, effectiveType.Name.IndexOf('\x60'));
        }

        private static string GetPropertyAccess(PropertyInfo property, FieldInfo backingField)
        {
            MethodInfo getter = property.GetGetMethod(true);
            MethodInfo setter = property.GetSetMethod(true);
            string readAccess = getter != null && getter.IsPublic ? "public get" : "private get";
            string writeAccess = setter == null
                ? (backingField == null ? "readonly" : "backing field")
                : (setter.IsPublic ? "public set" : "private set");
            return readAccess + " / " + writeAccess;
        }

        private static FieldInfo FindBackingField(Type type, string propertyName)
        {
            string backingFieldName = "<" + propertyName + ">k__BackingField";
            return type.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static string GetAttributeNames(MemberInfo member)
        {
            object[] attributes = member.GetCustomAttributes(false);
            List<string> names = new List<string>();

            foreach (object attribute in attributes)
            {
                string name = attribute.GetType().Name;
                if (name.EndsWith("Attribute", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - "Attribute".Length);
                }

                names.Add(name);
            }

            return names.Count == 0 ? string.Empty : string.Join(", ", names.ToArray());
        }

    }
}
