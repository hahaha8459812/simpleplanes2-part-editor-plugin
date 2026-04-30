using System;
using System.Reflection;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class PartRuntimeRefreshService
    {
        private const string DesignerTypeName = "Assets.Scripts.Design.Designer";
        private Type _designerType;
        private PropertyInfo _designerInstanceProperty;
        private FieldInfo _designerInstanceField;
        private MethodInfo _setAircraftStructureChangedMethod;

        public bool TryRefreshAfterApply(InspectableMember member)
        {
            object target;
            bool refreshed = false;

            if (member == null)
            {
                return false;
            }

            target = member.TargetObject;
            if (HasPartDataShape(target))
            {
                refreshed |= TryRefreshPartDataRuntime(target);
            }
            else if (string.Equals(target.GetType().Name, "JFuselageData", StringComparison.Ordinal))
            {
                refreshed |= TryRefreshModifierRuntime(target, member);
                refreshed |= TryInvokeNoArgMethod(target, "OnShapeDataChanged");
                refreshed |= TryInvokeNoArgMethod(target, "OnMeshRegenerated");
            }
            else
            {
                refreshed |= TryRefreshModifierRuntime(target, member);
            }

            refreshed |= TryMarkDesignerStructureChanged();

            return refreshed;
        }

        public bool TryRefreshTargetAfterApply(object target, System.Collections.Generic.IEnumerable<string> propertyNames, string value)
        {
            bool refreshed = false;

            if (target == null)
            {
                return false;
            }

            if (HasPartDataShape(target))
            {
                refreshed |= TryRefreshPartDataRuntime(target);
            }
            else if (string.Equals(target.GetType().Name, "JFuselageData", StringComparison.Ordinal))
            {
                refreshed |= TryRefreshModifierRuntime(target, propertyNames, value);
                refreshed |= TryInvokeNoArgMethod(target, "OnShapeDataChanged");
                refreshed |= TryInvokeNoArgMethod(target, "OnMeshRegenerated");
            }
            else
            {
                refreshed |= TryRefreshModifierRuntime(target, propertyNames, value);
            }

            refreshed |= TryMarkDesignerStructureChanged();
            return refreshed;
        }

        private static bool TryRefreshModifierRuntime(object target, InspectableMember member)
        {
            if (member == null)
            {
                return false;
            }

            return TryRefreshModifierRuntime(target, member.GetRuntimeRefreshMemberNames(), member.Value);
        }

        private static bool TryRefreshModifierRuntime(object target, System.Collections.Generic.IEnumerable<string> propertyNames, string value)
        {
            bool refreshed = false;

            if (target == null)
            {
                return false;
            }

            refreshed |= TryNotifyModifierPropertyChanged(target, propertyNames, value);
            refreshed |= TryRecalculateModifierMass(target);
            return refreshed;
        }

        private static bool TryNotifyModifierPropertyChanged(object target, System.Collections.Generic.IEnumerable<string> propertyNames, string value)
        {
            MethodInfo method;
            bool notified = false;

            if (target == null || propertyNames == null)
            {
                return false;
            }

            method = target.GetType().GetMethod(
                "OnGenericDesignerPropertyChanged",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(string) },
                null);

            if (method == null)
            {
                return false;
            }

            foreach (string propertyName in propertyNames)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    continue;
                }

                try
                {
                    method.Invoke(target, new object[] { propertyName, value ?? string.Empty });
                    notified = true;
                }
                catch
                {
                }
            }

            return notified;
        }

        private static bool TryRecalculateModifierMass(object target)
        {
            MethodInfo method;

            if (target == null)
            {
                return false;
            }

            method = target.GetType().GetMethod(
                "RecalculateMass",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool) },
                null);

            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(target, new object[] { true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryRecalculateLoadedMass(object partData)
        {
            MethodInfo method;

            if (partData == null)
            {
                return false;
            }

            method = partData.GetType().GetMethod(
                "RecalculateLoadedMass",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool) },
                null);

            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(partData, new object[] { true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryRefreshPartDataRuntime(object partData)
        {
            object partScript;
            bool refreshed = false;

            if (partData == null)
            {
                return false;
            }

            refreshed |= TryRecalculateLoadedMass(partData);

            partScript = GetInstanceMemberValue(partData, "PartScript");
            if (partScript == null)
            {
                return refreshed;
            }

            refreshed |= TrySyncPartScriptValues(partData, partScript);
            refreshed |= TrySyncTransformValues(partData, partScript);
            refreshed |= TryRefreshPrimaryColliders(partScript);
            refreshed |= TryRefreshPartEditorColliders(partData, partScript);
            refreshed |= TryInvokeNoArgMethod(partScript, "RefreshAttachPointVisibility");
            return refreshed;
        }

        private static bool TrySyncPartScriptValues(object partData, object partScript)
        {
            object health;
            float maxHealth;

            if (partData == null || partScript == null)
            {
                return false;
            }

            health = GetInstanceMemberValue(partData, "Health");
            if (!TryConvertToFiniteSingle(health, out maxHealth))
            {
                return false;
            }

            return TrySetInstanceMemberValue(partScript, "MaxHealth", Math.Max(maxHealth, 1f));
        }

        private static bool TrySyncTransformValues(object partData, object partScript)
        {
            Component aircraftComponent;
            Component partComponent;
            object aircraftScript;
            object position;
            object rotation;
            object partScale;
            bool refreshed = false;

            if (partData == null || partScript == null)
            {
                return false;
            }

            partComponent = partScript as Component;
            if (partComponent == null || partComponent.transform == null)
            {
                return false;
            }

            aircraftScript = GetInstanceMemberValue(partScript, "Aircraft");
            aircraftComponent = aircraftScript as Component;

            position = GetInstanceMemberValue(partData, "Position");
            if (position is Vector3 && IsFiniteVector((Vector3)position))
            {
                partComponent.transform.position = GetWorldPosition((Vector3)position, aircraftComponent);
                refreshed = true;
            }

            rotation = GetInstanceMemberValue(partData, "Rotation");
            if (rotation is Vector3 && IsFiniteVector((Vector3)rotation))
            {
                partComponent.transform.rotation = Quaternion.Euler((Vector3)rotation);
                refreshed = true;
            }

            partScale = GetInstanceMemberValue(partData, "PartScale");
            refreshed |= TrySyncPartScale(partComponent.transform, partScale);
            return refreshed;
        }

        private static Vector3 GetWorldPosition(Vector3 localPosition, Component aircraftComponent)
        {
            if (aircraftComponent == null || aircraftComponent.transform == null)
            {
                return localPosition;
            }

            return aircraftComponent.transform.position + localPosition;
        }

        private static bool TrySyncPartScale(Transform partTransform, object partScale)
        {
            Vector3 localScale;

            if (partTransform == null)
            {
                return false;
            }

            localScale = partScale is Vector3 ? (Vector3)partScale : Vector3.one;
            if (!IsFiniteVector(localScale))
            {
                return false;
            }

            partTransform.localScale = localScale;
            return true;
        }

        private static bool TryRefreshPrimaryColliders(object partScript)
        {
            object primaryCollider;
            bool refreshed = false;

            if (partScript == null)
            {
                return false;
            }

            primaryCollider = TryInvokeNoArgMethodWithResult(partScript, "GetPrimaryPartCollider");
            if (primaryCollider == null)
            {
                return false;
            }

            refreshed |= TrySetInstanceMemberValue(partScript, "PrimaryPartCollider", primaryCollider);
            if (GetInstanceMemberValue(partScript, "PrimaryPlacementCollider") == null)
            {
                refreshed |= TrySetInstanceMemberValue(partScript, "PrimaryPlacementCollider", primaryCollider);
            }

            return refreshed;
        }

        private static bool TryRefreshPartEditorColliders(object partData, object partScript)
        {
            object aircraftScript;
            object aircraftData;
            object assembly;
            MethodInfo method;

            if (partData == null)
            {
                return false;
            }

            aircraftScript = GetInstanceMemberValue(partScript, "Aircraft");
            aircraftData = GetInstanceMemberValue(aircraftScript, "Aircraft");
            assembly = GetInstanceMemberValue(aircraftData, "Assembly");
            if (partScript == null || assembly == null)
            {
                return false;
            }

            method = FindOneParameterMethod(assembly.GetType(), "CreateEditorCollidersForPartScript", partScript.GetType());
            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(assembly, new[] { partScript });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryMarkDesignerStructureChanged()
        {
            object designerInstance;

            if (!EnsureDesignerMethodCached())
            {
                return false;
            }

            designerInstance = GetStaticCachedMemberValue(_designerInstanceProperty, _designerInstanceField);
            if (designerInstance == null)
            {
                return false;
            }

            try
            {
                _setAircraftStructureChangedMethod.Invoke(designerInstance, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool EnsureDesignerMethodCached()
        {
            BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (_setAircraftStructureChangedMethod != null)
            {
                return true;
            }

            _designerType = FindType(DesignerTypeName);
            if (_designerType == null)
            {
                return false;
            }

            _designerInstanceProperty = _designerType.GetProperty("Instance", staticFlags);
            _designerInstanceField = _designerType.GetField("Instance", staticFlags);
            _setAircraftStructureChangedMethod = _designerType.GetMethod("SetAircraftStructureChanged", instanceFlags, null, Type.EmptyTypes, null);
            return _setAircraftStructureChangedMethod != null;
        }

        private static bool HasPartDataShape(object target)
        {
            if (target == null)
            {
                return false;
            }

            return target.GetType().GetMethod("GenerateXml", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null &&
                   target.GetType().GetMethod("RecalculateLoadedMass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null) != null;
        }

        private static object GetStaticCachedMemberValue(PropertyInfo property, FieldInfo field)
        {
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            return field == null ? null : field.GetValue(null);
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

        private static bool TrySetInstanceMemberValue(object target, string name, object value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property;
            FieldInfo field;

            if (target == null)
            {
                return false;
            }

            try
            {
                property = target.GetType().GetProperty(name, flags);
                if (property != null && property.GetSetMethod(true) != null && property.GetIndexParameters().Length == 0)
                {
                    property.SetValue(target, value, null);
                    return true;
                }

                field = target.GetType().GetField("<" + name + ">k__BackingField", flags) ?? target.GetType().GetField(name, flags);
                if (field != null && !field.IsLiteral)
                {
                    field.SetValue(target, value);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryInvokeNoArgMethod(object target, string methodName)
        {
            return TryInvokeNoArgMethodWithResult(target, methodName) != null;
        }

        private static object TryInvokeNoArgMethodWithResult(object target, string methodName)
        {
            MethodInfo method;

            if (target == null)
            {
                return null;
            }

            method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method == null)
            {
                return null;
            }

            try
            {
                return method.Invoke(target, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool TryConvertToFiniteSingle(object value, out float result)
        {
            try
            {
                if (value == null)
                {
                    result = 0f;
                    return false;
                }

                result = Convert.ToSingle(value);
                return IsFinite(result);
            }
            catch
            {
                result = 0f;
                return false;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static MethodInfo FindOneParameterMethod(Type type, string methodName, Type argumentType)
        {
            MethodInfo[] methods;
            int methodIndex;

            if (type == null || argumentType == null)
            {
                return null;
            }

            methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (methodIndex = 0; methodIndex < methods.Length; methodIndex++)
            {
                ParameterInfo[] parameters;
                if (methods[methodIndex].Name != methodName)
                {
                    continue;
                }

                parameters = methods[methodIndex].GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(argumentType))
                {
                    return methods[methodIndex];
                }
            }

            return null;
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
