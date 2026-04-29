using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class RuntimeDiagnosticSnapshot
    {
        private RuntimeDiagnosticSnapshot(List<RuntimeDiagnosticItem> items)
        {
            Items = items;
            HasProblems = HasUnhealthyItem(items);
        }

        public List<RuntimeDiagnosticItem> Items { get; private set; }

        public bool HasProblems { get; private set; }

        public static RuntimeDiagnosticSnapshot FromPartData(object partData, object selectedPart)
        {
            List<RuntimeDiagnosticItem> items = new List<RuntimeDiagnosticItem>();
            object partScript = GetInstanceMemberValue(partData, "PartScript");
            Component component = partScript as Component;
            GameObject gameObject = component == null ? null : component.gameObject;
            Transform transform = gameObject == null ? null : gameObject.transform;
            object reversePartData = GetInstanceMemberValue(partScript, "Part");
            object primaryPartCollider = GetInstanceMemberValue(partScript, "PrimaryPartCollider");
            object primaryPlacementCollider = GetInstanceMemberValue(partScript, "PrimaryPlacementCollider");
            object editorColliders = GetInstanceMemberValue(partScript, "EditorColliders");
            object partDataHealth = GetInstanceMemberValue(partData, "Health");
            object partScriptMaxHealth = GetInstanceMemberValue(partScript, "MaxHealth");
            object partScriptDamage = GetInstanceMemberValue(partScript, "PartDamage");
            object partScriptDamageLevel = GetInstanceMemberValue(partScript, "PartDamageLevel");

            AddObjectState(items, "runtime.partData", partData);
            AddObjectState(items, "runtime.partScript", partScript);
            AddObjectState(items, "runtime.gameObject", gameObject);
            AddBooleanState(items, "runtime.selectedPartLinked", selectedPart != null && object.ReferenceEquals(selectedPart, partScript));
            AddBooleanState(items, "runtime.reversePartLinked", partData != null && object.ReferenceEquals(reversePartData, partData));
            AddBooleanState(items, "runtime.activeInHierarchy", gameObject != null && gameObject.activeInHierarchy);
            AddBooleanState(items, "runtime.sceneValid", gameObject != null && gameObject.scene.IsValid());
            AddObjectState(items, "runtime.transformParent", transform == null ? null : transform.parent);
            AddVectorState(items, "runtime.position", transform == null ? (Vector3?)null : transform.position, false);
            AddVectorState(items, "runtime.localScale", transform == null ? (Vector3?)null : transform.localScale, true);
            AddObjectState(items, "runtime.primaryPartCollider", primaryPartCollider);
            AddObjectState(items, "runtime.primaryPlacementCollider", primaryPlacementCollider);
            AddCountState(items, "runtime.editorColliders", editorColliders);
            AddValueState(items, "runtime.partDataHealth", partDataHealth, partDataHealth != null);
            AddValueState(items, "runtime.partScriptMaxHealth", partScriptMaxHealth, partScriptMaxHealth != null);
            AddValueState(items, "runtime.partScriptDamage", partScriptDamage, partScriptDamage != null);
            AddValueState(items, "runtime.partScriptDamageLevel", partScriptDamageLevel, partScriptDamageLevel != null);
            AddBooleanState(items, "runtime.healthLinked", AreNumericValuesEqual(partDataHealth, partScriptMaxHealth));

            return new RuntimeDiagnosticSnapshot(items);
        }

        private static void AddObjectState(List<RuntimeDiagnosticItem> items, string labelKey, object value)
        {
            items.Add(new RuntimeDiagnosticItem(labelKey, value == null ? "Missing" : "OK", value != null));
        }

        private static void AddBooleanState(List<RuntimeDiagnosticItem> items, string labelKey, bool isHealthy)
        {
            items.Add(new RuntimeDiagnosticItem(labelKey, isHealthy ? "true" : "false", isHealthy));
        }

        private static void AddVectorState(List<RuntimeDiagnosticItem> items, string labelKey, Vector3? value, bool requireNonZero)
        {
            bool isHealthy;

            if (!value.HasValue)
            {
                items.Add(new RuntimeDiagnosticItem(labelKey, "Missing", false));
                return;
            }

            isHealthy = IsFiniteVector(value.Value);
            if (requireNonZero)
            {
                isHealthy = isHealthy && value.Value.sqrMagnitude > 0.000001f;
            }

            items.Add(new RuntimeDiagnosticItem(labelKey, FormatVector(value.Value), isHealthy));
        }

        private static void AddCountState(List<RuntimeDiagnosticItem> items, string labelKey, object value)
        {
            int count;

            if (value == null)
            {
                items.Add(new RuntimeDiagnosticItem(labelKey, "Missing", false));
                return;
            }

            count = CountEnumerableItems(value as IEnumerable);
            items.Add(new RuntimeDiagnosticItem(labelKey, count.ToString(CultureInfo.InvariantCulture), count > 0));
        }

        private static void AddValueState(List<RuntimeDiagnosticItem> items, string labelKey, object value, bool isHealthy)
        {
            items.Add(new RuntimeDiagnosticItem(labelKey, FormatValue(value), isHealthy));
        }

        private static bool HasUnhealthyItem(List<RuntimeDiagnosticItem> items)
        {
            int itemIndex;

            for (itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                if (!items[itemIndex].IsHealthy)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountEnumerableItems(IEnumerable enumerable)
        {
            int count = 0;

            if (enumerable == null)
            {
                return 0;
            }

            foreach (object ignored in enumerable)
            {
                count++;
            }

            return count;
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.####", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.####", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "Missing";
            }

            if (value is float)
            {
                return ((float)value).ToString("0.####", CultureInfo.InvariantCulture);
            }

            if (value is double)
            {
                return ((double)value).ToString("0.####", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static bool AreNumericValuesEqual(object left, object right)
        {
            double leftValue;
            double rightValue;

            if (!TryConvertToDouble(left, out leftValue) || !TryConvertToDouble(right, out rightValue))
            {
                return false;
            }

            return Math.Abs(leftValue - rightValue) < 0.0001;
        }

        private static bool TryConvertToDouble(object value, out double number)
        {
            number = 0d;
            if (value == null)
            {
                return false;
            }

            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return !double.IsNaN(number) && !double.IsInfinity(number);
            }
            catch
            {
                return false;
            }
        }

        private static object GetInstanceMemberValue(object target, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property;
            FieldInfo field;

            if (target == null)
            {
                return null;
            }

            try
            {
                property = target.GetType().GetProperty(name, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(target, null);
                }

                field = target.GetType().GetField(name, flags);
                return field == null ? null : field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }
    }
}
