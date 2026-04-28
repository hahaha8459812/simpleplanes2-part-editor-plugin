using System;
using System.Reflection;

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
            object partData;
            bool refreshed = false;

            if (member == null)
            {
                return false;
            }

            target = member.TargetObject;
            partData = GetPartDataFromTarget(target);

            refreshed |= TryNotifyModifierPropertyChanged(target, member.GetRuntimeRefreshMemberNames(), member.Value);
            refreshed |= TryRecalculateModifierMass(target);
            refreshed |= TryRecalculateLoadedMass(partData);
            refreshed |= TryMarkDesignerStructureChanged();

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

        private static object GetPartDataFromTarget(object target)
        {
            object partData;

            if (target == null)
            {
                return null;
            }

            if (HasPartDataShape(target))
            {
                return target;
            }

            partData = GetInstanceMemberValue(target, "Part");
            if (HasPartDataShape(partData))
            {
                return partData;
            }

            partData = GetInstanceMemberValue(target, "PartData");
            if (HasPartDataShape(partData))
            {
                return partData;
            }

            return null;
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
