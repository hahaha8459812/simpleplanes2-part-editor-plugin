using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class DesignerSelectionService
    {
        private const string DesignerTypeName = "Assets.Scripts.Design.Designer";
        private const float DesignerResolveRetryDelaySeconds = 2f;
        private static readonly string[] LegacyPartTypePrefixes =
        {
            "AirBrake-",
            "ArrestingHook-",
            "BeaconLight-",
            "Block-",
            "Bomb-",
            "Button-",
            "Camera-",
            "Cannon-",
            "Car-",
            "CatapultConnector-",
            "Cockpit-",
            "Control-",
            "ControlBase-",
            "Countermeasure-",
            "Detacher-",
            "Engine-",
            "FlightComputer-",
            "FormationLight-",
            "FuelTank-",
            "Fuselage-",
            "Gauge-",
            "Grip-",
            "Gun-",
            "Gyroscope-",
            "HeliRotor-",
            "HeliTailRotor-",
            "Hemisphere-",
            "HingeRotator-",
            "Inlet-",
            "JointRotator-",
            "Label-",
            "Magnet-",
            "Missile-",
            "NoseCone-",
            "Parachute-",
            "Piston-",
            "Pylon-",
            "ReactionControlNozzle-",
            "Refuel-",
            "Rocket-",
            "RocketPod-",
            "Seat-",
            "Shock-",
            "SmallRotator-",
            "Switch-",
            "ThrustPort-",
            "Wheel-",
            "Winch-",
            "Wing-"
        };

        private static readonly string[] LegacyModifierTypeNames =
        {
            "EngineData",
            "FloatingPartData",
            "FuelTankData",
            "FuselageData",
            "WheelData",
            "WingData"
        };

        private readonly ReflectionMemberScanner _memberScanner;
        private Type _designerType;
        private PropertyInfo _designerInstanceProperty;
        private FieldInfo _designerInstanceField;
        private PropertyInfo _selectedPartProperty;
        private FieldInfo _selectedPartField;
        private PropertyInfo _partDataProperty;
        private FieldInfo _partDataField;
        private float _nextDesignerResolveTime;

        public DesignerSelectionService(ReflectionMemberScanner memberScanner)
        {
            _memberScanner = memberScanner;
        }

        public SelectionProbeResult ProbeSelection()
        {
            object designerInstance;
            object selectedPart;
            object partData;

            try
            {
                designerInstance = GetDesignerInstance();
                if (designerInstance == null)
                {
                    return SelectionProbeResult.FromNoDesigner();
                }

                selectedPart = GetSelectedPart(designerInstance);
                if (selectedPart == null)
                {
                    return SelectionProbeResult.FromStatus("label.noSelection");
                }

                partData = GetPartData(selectedPart);
                if (partData == null)
                {
                    return SelectionProbeResult.FromStatus("label.noPartData");
                }

                return SelectionProbeResult.FromSelectedPart(GetObjectId(selectedPart));
            }
            catch
            {
                InvalidateDesignerCache();
                return SelectionProbeResult.FromStatus("label.selectionReadFailed");
            }
        }

        public SelectionReadResult ReadSelection()
        {
            object designerInstance;
            object selectedPart;
            object partData;

            try
            {
                designerInstance = GetDesignerInstance();
                if (designerInstance == null)
                {
                    return SelectionReadResult.FromStatus("label.noDesigner");
                }

                selectedPart = GetSelectedPart(designerInstance);
                if (selectedPart == null)
                {
                    return SelectionReadResult.FromStatus("label.noSelection");
                }

                partData = GetPartData(selectedPart);
                if (partData == null)
                {
                    return SelectionReadResult.FromStatus("label.noPartData");
                }

                return SelectionReadResult.FromSnapshot(CreateSnapshot(selectedPart, partData));
            }
            catch
            {
                InvalidateDesignerCache();
                return SelectionReadResult.FromStatus("label.selectionReadFailed");
            }
        }

        private SelectedPartSnapshot CreateSnapshot(object selectedPart, object partData)
        {
            List<InspectableGroup> groups = new List<InspectableGroup>();
            object modifiers = GetInstanceMemberValue(partData, "Modifiers");
            string partName = ConvertToString(GetInstanceMemberValue(partData, "Name"));
            string partId = ConvertToString(GetInstanceMemberValue(partData, "Id"));
            object partType = GetInstanceMemberValue(partData, "PartType");
            string partTypeName = GetPartTypeName(partType);
            string partTypeId = GetPartTypeId(partType);
            string xmlText = GenerateXmlText(partData);

            groups.Add(new InspectableGroup("PartData", partData.GetType().FullName, partData, _memberScanner.ScanDisplayableMembers(partData)));
            AddModifierGroups(groups, modifiers);

            return new SelectedPartSnapshot(
                GetObjectId(selectedPart),
                partName,
                partId,
                partTypeName,
                partTypeId,
                GetCompatibilityLabelKey(partTypeId, modifiers),
                partData.GetType().FullName,
                partData,
                xmlText,
                groups);
        }

        private void AddModifierGroups(List<InspectableGroup> groups, object modifiers)
        {
            IEnumerable enumerable = modifiers as IEnumerable;
            int modifierIndex = 1;

            if (enumerable == null)
            {
                return;
            }

            foreach (object modifier in enumerable)
            {
                string modifierId;
                string title;

                if (modifier == null)
                {
                    continue;
                }

                modifierId = ConvertToString(GetInstanceMemberValue(modifier, "Id"));
                title = string.IsNullOrEmpty(modifierId)
                    ? modifier.GetType().Name
                    : modifier.GetType().Name + " (" + modifierId + ")";

                groups.Add(new InspectableGroup(
                    "Modifier " + modifierIndex + ": " + title,
                    modifier.GetType().FullName,
                    modifier,
                    _memberScanner.ScanDisplayableMembers(modifier)));
                modifierIndex++;
            }
        }

        private static string GetPartTypeName(object partType)
        {
            string name;

            if (partType == null)
            {
                return string.Empty;
            }

            name = ConvertToString(GetInstanceMemberValue(partType, "Name"));
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = ConvertToString(GetInstanceMemberValue(partType, "PartTypeId"));
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return partType.GetType().Name;
        }

        private static string GetPartTypeId(object partType)
        {
            string id;

            if (partType == null)
            {
                return string.Empty;
            }

            id = ConvertToString(GetInstanceMemberValue(partType, "PartTypeId"));
            return string.IsNullOrEmpty(id) ? GetPartTypeName(partType) : id;
        }

        private static string GetCompatibilityLabelKey(string partTypeId, object modifiers)
        {
            if (IsLegacyPartTypeId(partTypeId) || HasLegacyModifier(modifiers))
            {
                return "compatibility.legacySp1";
            }

            return "compatibility.sp2";
        }

        private static bool IsLegacyPartTypeId(string partTypeId)
        {
            int prefixIndex;

            if (string.IsNullOrEmpty(partTypeId))
            {
                return false;
            }

            for (prefixIndex = 0; prefixIndex < LegacyPartTypePrefixes.Length; prefixIndex++)
            {
                if (partTypeId.StartsWith(LegacyPartTypePrefixes[prefixIndex], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLegacyModifier(object modifiers)
        {
            IEnumerable enumerable = modifiers as IEnumerable;

            if (enumerable == null)
            {
                return false;
            }

            foreach (object modifier in enumerable)
            {
                string typeName = modifier == null ? string.Empty : modifier.GetType().Name;
                if (IsLegacyModifierTypeName(typeName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLegacyModifierTypeName(string typeName)
        {
            int typeIndex;

            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            for (typeIndex = 0; typeIndex < LegacyModifierTypeNames.Length; typeIndex++)
            {
                if (typeName == LegacyModifierTypeNames[typeIndex])
                {
                    return true;
                }
            }

            return false;
        }

        private static string GenerateXmlText(object partData)
        {
            object xml;
            MethodInfo method = partData.GetType().GetMethod("GenerateXml", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method == null)
            {
                return string.Empty;
            }

            try
            {
                xml = method.Invoke(partData, null);
                CustomXmlAttributeStore.ApplyToXml(partData, xml as System.Xml.Linq.XElement);
                return xml == null ? string.Empty : xml.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private object GetDesignerInstance()
        {
            if (!EnsureDesignerTypeResolved())
            {
                return null;
            }

            if (_designerInstanceProperty == null && _designerInstanceField == null)
            {
                CacheDesignerInstanceMember();
            }

            return GetStaticCachedMemberValue(_designerInstanceProperty, _designerInstanceField);
        }

        private object GetSelectedPart(object designerInstance)
        {
            if (_selectedPartProperty == null && _selectedPartField == null)
            {
                CacheInstanceMember(designerInstance.GetType(), "SelectedPart", out _selectedPartProperty, out _selectedPartField);
            }

            return GetInstanceCachedMemberValue(designerInstance, _selectedPartProperty, _selectedPartField);
        }

        private object GetPartData(object selectedPart)
        {
            if (_partDataProperty == null && _partDataField == null)
            {
                CacheInstanceMember(selectedPart.GetType(), "Part", out _partDataProperty, out _partDataField);
            }

            return GetInstanceCachedMemberValue(selectedPart, _partDataProperty, _partDataField);
        }

        private bool EnsureDesignerTypeResolved()
        {
            if (_designerType != null)
            {
                return true;
            }

            if (Time.unscaledTime < _nextDesignerResolveTime)
            {
                return false;
            }

            _designerType = FindType(DesignerTypeName);
            if (_designerType == null)
            {
                _nextDesignerResolveTime = Time.unscaledTime + DesignerResolveRetryDelaySeconds;
                return false;
            }

            _nextDesignerResolveTime = 0f;
            return true;
        }

        private void CacheDesignerInstanceMember()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _designerInstanceProperty = _designerType.GetProperty("Instance", flags);
            _designerInstanceField = _designerType.GetField("Instance", flags);
        }

        private static void CacheInstanceMember(Type type, string name, out PropertyInfo property, out FieldInfo field)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            property = type.GetProperty(name, flags);
            field = type.GetField(name, flags);
        }

        private void InvalidateDesignerCache()
        {
            _designerType = null;
            _designerInstanceProperty = null;
            _designerInstanceField = null;
            _selectedPartProperty = null;
            _selectedPartField = null;
            _partDataProperty = null;
            _partDataField = null;
            _nextDesignerResolveTime = Time.unscaledTime + DesignerResolveRetryDelaySeconds;
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
            Type type;
            PropertyInfo property;
            FieldInfo field;

            if (target == null)
            {
                return null;
            }

            type = target.GetType();
            try
            {
                property = type.GetProperty(name, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(target, null);
                }

                field = type.GetField(name, flags);
                return field == null ? null : field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static object GetInstanceCachedMemberValue(object target, PropertyInfo property, FieldInfo field)
        {
            if (target == null)
            {
                return null;
            }

            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(target, null);
            }

            return field == null ? null : field.GetValue(target);
        }

        private static int GetObjectId(object target)
        {
            UnityEngine.Object unityObject = target as UnityEngine.Object;
            if (unityObject != null)
            {
                return unityObject.GetInstanceID();
            }

            return RuntimeHelpers.GetHashCode(target);
        }

        private static string ConvertToString(object value)
        {
            return value == null ? string.Empty : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
