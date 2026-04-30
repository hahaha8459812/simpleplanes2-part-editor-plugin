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
            // ModifierData properties (type-specific)
            {"@AirBrakeData.drag", "desc.airBrake.drag"},
            {"@AirBrakeData.speed", "desc.airBrake.speed"},
            {"@BeaconLightData.activationGroup", "desc.beaconLight.activationGroup"},
            {"@BeaconLightData.input", "desc.beaconLight.input"},
            {"@BeaconLightData.intensity", "desc.beaconLight.intensity"},
            {"@BeaconLightData.showHalo", "desc.beaconLight.showHalo"},
            {"@BombData.defaultFiringDelay", "desc.bomb.defaultFiringDelay"},
            {"@CannonData.activationGroup", "desc.cannon.activationGroup"},
            {"@CannonData.ammoCount", "desc.cannon.ammoCount"},
            {"@CannonData.ammoStyle", "desc.cannon.ammoStyle"},
            {"@CannonData.ammoType", "desc.cannon.ammoType"},
            {"@CannonData.barrelLength", "desc.cannon.barrelLength"},
            {"@CannonData.caliberInMilimeters", "desc.cannon.caliberInMilimeters"},
            {"@CannonData.diameter", "desc.cannon.diameter"},
            {"@CannonData.explosionScalar", "desc.cannon.explosionScalar"},
            {"@CannonData.firingDelay", "desc.cannon.firingDelay"},
            {"@CannonData.function", "desc.cannon.function"},
            {"@CannonData.mass", "desc.cannon.mass"},
            {"@CannonData.muzzleBrake", "desc.cannon.muzzleBrake"},
            {"@CannonData.projectileLifetime", "desc.cannon.projectileLifetime"},
            {"@CannonData.projectileVelocity", "desc.cannon.projectileVelocity"},
            {"@CannonData.recoilForce", "desc.cannon.recoilForce"},
            {"@CanopyData.animationSpeed", "desc.canopy.animationSpeed"},
            {"@CanopyData.dragWhenOpen", "desc.canopy.dragWhenOpen"},
            {"@CanopyData.hasAnimation", "desc.canopy.hasAnimation"},
            {"@CanopyData.opacity", "desc.canopy.opacity"},
            {"@CockpitData.hasCamera", "desc.cockpit.hasCamera"},
            {"@CockpitData.primaryCockpit", "desc.cockpit.primaryCockpit"},
            {"@ControlSurfaceData.activationGroup", "desc.controlSurface.activationGroup"},
            {"@ControlSurfaceData.activationGroupLocksInput", "desc.controlSurface.activationGroupLocksInput"},
            {"@ControlSurfaceData.activationString", "desc.controlSurface.activationString"},
            {"@ControlSurfaceData.autoInvert", "desc.controlSurface.autoInvert"},
            {"@ControlSurfaceData.end", "desc.controlSurface.end"},
            {"@ControlSurfaceData.inputId", "desc.controlSurface.inputId"},
            {"@ControlSurfaceData.invert", "desc.controlSurface.invert"},
            {"@ControlSurfaceData.length", "desc.controlSurface.length"},
            {"@ControlSurfaceData.maxDeflectionDegree", "desc.controlSurface.maxDeflectionDegree"},
            {"@ControlSurfaceData.start", "desc.controlSurface.start"},
            {"@ControlSurfaceData.trim", "desc.controlSurface.trim"},
            {"@DetacherData.attachPointsToDetach", "desc.detacher.attachPointsToDetach"},
            {"@DetacherData.delay", "desc.detacher.delay"},
            {"@DetacherData.designerForce", "desc.detacher.designerForce"},
            {"@DetacherData.detacherForce", "desc.detacher.detacherForce"},
            {"@DetacherData.direction", "desc.detacher.direction"},
            {"@DetacherData.enabled", "desc.detacher.enabled"},
            {"@DetacherData.group", "desc.detacher.group"},
            {"@EngineData.alphaTiedToThrottle", "desc.engine.alphaTiedToThrottle"},
            {"@EngineData.ductedThrust", "desc.engine.ductedThrust"},
            {"@EngineData.engineType", "desc.engine.engineType"},
            {"@EngineData.exhaustScale", "desc.engine.exhaustScale"},
            {"@EngineData.fuelConsumptionRate", "desc.engine.fuelConsumptionRate"},
            {"@EngineData.power", "desc.engine.power"},
            {"@EngineData.powerMultiplier", "desc.engine.powerMultiplier"},
            {"@EngineData.requiredAirIntake", "desc.engine.requiredAirIntake"},
            {"@EngineData.soundOverride", "desc.engine.soundOverride"},
            {"@EngineData.throttleResponse", "desc.engine.throttleResponse"},
            {"@FloatingPartData.enabled", "desc.floatingPart.enabled"},
            {"@FloatingPartData.reduceBuoyancyIfBySelf", "desc.floatingPart.reduceBuoyancyIfBySelf"},
            {"@FloatingPartData.weightFactor", "desc.floatingPart.weightFactor"},
            {"@FuelTankData.capacity", "desc.fuelTank.capacity"},
            {"@FuelTankData.fuel", "desc.fuelTank.fuel"},
            {"@FuelTankData.mass", "desc.fuelTank.mass"},
            {"@GyroscopeData.activationGroup", "desc.gyroscope.activationGroup"},
            {"@GyroscopeData.autoOrient", "desc.gyroscope.autoOrient"},
            {"@GyroscopeData.pitchEnabled", "desc.gyroscope.pitchEnabled"},
            {"@GyroscopeData.pitchRange", "desc.gyroscope.pitchRange"},
            {"@GyroscopeData.rollEnabled", "desc.gyroscope.rollEnabled"},
            {"@GyroscopeData.rollRange", "desc.gyroscope.rollRange"},
            {"@GyroscopeData.speed", "desc.gyroscope.speed"},
            {"@GyroscopeData.stability", "desc.gyroscope.stability"},
            {"@GyroscopeData.yawPower", "desc.gyroscope.yawPower"},
            {"@InletData.airIntakeMultiplier", "desc.inlet.airIntakeMultiplier"},
            {"@InputControllerData.activationGroup", "desc.inputController.activationGroup"},
            {"@InputControllerData.input", "desc.inputController.input"},
            {"@InputControllerData.invert", "desc.inputController.invert"},
            {"@InputControllerData.invertOnMirror", "desc.inputController.invertOnMirror"},
            {"@InputControllerData.invertType", "desc.inputController.invertType"},
            {"@InputControllerData.maxValue", "desc.inputController.maxValue"},
            {"@InputControllerData.minValue", "desc.inputController.minValue"},
            {"@InputControllerData.name", "desc.inputController.name"},
            {"@InputControllerData.zeroOnDeactivate", "desc.inputController.zeroOnDeactivate"},
            {"@JetEngineData.bypassRatio", "desc.jetEngine.bypassRatio"},
            {"@JetEngineData.compressionRatio", "desc.jetEngine.compressionRatio"},
            {"@JetEngineData.hasAfterburner", "desc.jetEngine.hasAfterburner"},
            {"@JetEngineData.hasReverseThrust", "desc.jetEngine.hasReverseThrust"},
            {"@JetEngineData.mass", "desc.jetEngine.mass"},
            {"@JetEngineData.maxGimbalAngle", "desc.jetEngine.maxGimbalAngle"},
            {"@JetEngineData.scale", "desc.jetEngine.scale"},
            {"@JetEngineData.throttleResponse", "desc.jetEngine.throttleResponse"},
            {"@LabelData.designText", "desc.label.designText"},
            {"@LabelData.emissionDay", "desc.label.emissionDay"},
            {"@LabelData.emissionNight", "desc.label.emissionNight"},
            {"@LabelData.fontSize", "desc.label.fontSize"},
            {"@LabelData.height", "desc.label.height"},
            {"@LabelData.width", "desc.label.width"},
            {"@MagnetData.activationGroup", "desc.magnet.activationGroup"},
            {"@MagnetData.power", "desc.magnet.power"},
            {"@MissileData.guidanceActivationDelay", "desc.missile.guidanceActivationDelay"},
            {"@MissileData.ignitionDelay", "desc.missile.ignitionDelay"},
            {"@MissileData.maxForwardThrustForce", "desc.missile.maxForwardThrustForce"},
            {"@MissileData.maxFuelTime", "desc.missile.maxFuelTime"},
            {"@MissileData.maxRange", "desc.missile.maxRange"},
            {"@MissileData.maxSpeed", "desc.missile.maxSpeed"},
            {"@MissileData.maxTargetingAngle", "desc.missile.maxTargetingAngle"},
            {"@MissileData.proximityDetonationRangeMax", "desc.missile.proximityDetonationRangeMax"},
            {"@MissileData.proximityDetonationRangeMin", "desc.missile.proximityDetonationRangeMin"},
            {"@MissileData.seeker", "desc.missile.seeker"},
            {"@MissileData.waterproof", "desc.missile.waterproof"},
            {"@ParachuteData.activationGroup", "desc.parachute.activationGroup"},
            {"@ParachuteData.drag", "desc.parachute.drag"},
            {"@ParachuteData.scale", "desc.parachute.scale"},
            {"@RotatorData.enabled", "desc.rotator.enabled"},
            {"@RotatorData.inputX", "desc.rotator.inputX"},
            {"@RotatorData.inputY", "desc.rotator.inputY"},
            {"@RotatorData.inputZ", "desc.rotator.inputZ"},
            {"@RotatorData.target", "desc.rotator.target"},
            {"@SeatData.primarySeat", "desc.seat.primarySeat"},
            {"@SeatData.reclination", "desc.seat.reclination"},
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
