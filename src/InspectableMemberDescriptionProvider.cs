using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimplePlanes2PartEditor
{
    internal sealed class InspectableMemberDescriptionProvider
    {
        private readonly LocalizationProvider _localization;

        // Keys starting with '@' are localization keys resolved at lookup time.
        // Other entries are literal strings used as-is.
        private static readonly Dictionary<string, string> CustomDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // PartData properties (shared across all parts via *.<memberName>)
            {"@*.AllowTransformation", "desc.partData.allowTransformation"},
            {"@*.CenterOfMass", "desc.partData.centerOfMass"},
            {"@*.DisableAircraftCollisions", "desc.partData.disableAircraftCollisions"},
            {"@*.DragScale", "desc.partData.dragScale"},
            {"@*.DragType", "desc.partData.dragType"},
            {"@*.DragTypeAsConfigured", "desc.partData.dragTypeAsConfigured"},
            {"@*.DragTypeDefault", "desc.partData.dragTypeDefault"},
            {"@*.EmptyMass", "desc.partData.emptyMass"},
            {"@*.Enabled", "desc.partData.enabled"},
            {"@*.GroupId", "desc.partData.groupId"},
            {"@*.Health", "desc.partData.health"},
            {"@*.Id", "desc.partData.id"},
            {"@*.InitiallyConnectedToMainCockpit", "desc.partData.initiallyConnectedToMainCockpit"},
            {"@*.InitialRotationMirrored", "desc.partData.initialRotationMirrored"},
            {"@*.IsCockpit", "desc.partData.isCockpit"},
            {"@*.IsPowertrainPart", "desc.partData.isPowertrainPart"},
            {"@*.IsUsingConstantDrag", "desc.partData.isUsingConstantDrag"},
            {"@*.LoadContext", "desc.partData.loadContext"},
            {"@*.LoadedMass", "desc.partData.loadedMass"},
            {"@*.MassScale", "desc.partData.massScale"},
            {"@*.ModifierMass", "desc.partData.modifierMass"},
            {"@*.Name", "desc.partData.name"},
            {"@*.PartCollisionResponse", "desc.partData.partCollisionResponse"},
            {"@*.PartDrag", "desc.partData.partDrag"},
            {"@*.PartScale", "desc.partData.partScale"},
            {"@*.Position", "desc.partData.position"},
            {"@*.RenderQueue", "desc.partData.renderQueue"},
            {"@*.Rotation", "desc.partData.rotation"},
            {"@*.SharesRigidBody", "desc.partData.sharesRigidBody"},
            {"@*.SymmetryDisabled", "desc.partData.symmetryDisabled"},
            {"@*.SymmetryId", "desc.partData.symmetryId"},
            {"@*.UnderwaterDragScalar", "desc.partData.underwaterDragScalar"},
            {"@*.VisibleInDesigner", "desc.partData.visibleInDesigner"},
            {"@*.MaterialIds", "desc.partData.materialIds"},
            {"@*.AttachPoints", "desc.partData.attachPoints"},
            {"@*.Decals", "desc.partData.decals"},
            {"@*.Modifiers", "desc.partData.modifiers"},
            {"@*.PartConnections", "desc.partData.partConnections"},
            {"@*.PartCreationInfoUsedForInitialization", "desc.partData.partCreationInfoUsedForInitialization"},
            {"@*.PartScript", "desc.partData.partScript"},
            {"@*.PartType", "desc.partData.partType"},
        };

        private static readonly Dictionary<string, string> LiteralDescriptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
        };

        public InspectableMemberDescriptionProvider(LocalizationProvider localization)
        {
            _localization = localization;
        }

        public string GetDescription(Type targetType, MemberInfo member)
        {
            string description;

            if (member == null)
            {
                return string.Empty;
            }

            if (TryGetAttributeDescription(member, out description))
            {
                return description;
            }

            if (TryGetCustomDescription(targetType, member.Name, out description))
            {
                return description;
            }

            return string.Empty;
        }

        private bool TryGetCustomDescription(Type targetType, string memberName, out string description)
        {
            description = string.Empty;
            if (targetType == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            string exactKey = targetType.Name + "." + memberName;
            string sharedKey = "*." + memberName;

            if (TryResolveDescription(exactKey, out description))
            {
                return true;
            }

            return TryResolveDescription(sharedKey, out description);
        }

        private bool TryResolveDescription(string key, out string description)
        {
            description = string.Empty;

            string localizedKey;
            if (CustomDescriptions.TryGetValue("@" + key, out localizedKey))
            {
                description = _localization.Get(localizedKey);
                return !string.IsNullOrEmpty(description) && !string.Equals(description, localizedKey, StringComparison.Ordinal);
            }

            if (LiteralDescriptions.TryGetValue(key, out description))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetAttributeDescription(MemberInfo member, out string description)
        {
            object[] attributes = member.GetCustomAttributes(false);
            description = string.Empty;

            foreach (object attribute in attributes)
            {
                if (TryReadStringMember(attribute, "Description", out description) ||
                    TryReadStringMember(attribute, "Tooltip", out description) ||
                    TryReadStringMember(attribute, "tooltip", out description))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadStringMember(object source, string memberName, out string value)
        {
            Type sourceType;
            PropertyInfo property;
            FieldInfo field;
            object rawValue;

            value = string.Empty;
            if (source == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            sourceType = source.GetType();
            property = sourceType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(string))
            {
                rawValue = property.GetValue(source, null);
                value = rawValue as string;
                return !string.IsNullOrEmpty(value);
            }

            field = sourceType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(string))
            {
                rawValue = field.GetValue(source);
                value = rawValue as string;
                return !string.IsNullOrEmpty(value);
            }

            return false;
        }
    }
}
