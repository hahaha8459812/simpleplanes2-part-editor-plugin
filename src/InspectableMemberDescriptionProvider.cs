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

            // ModifierData properties batch 2 (low-frequency types)
            {"@AdaptiveNoseConeData.mass", "desc.adaptiveNoseCone.mass"},
            {"@AdaptiveNoseConeData.scale", "desc.adaptiveNoseCone.scale"},
            {"@AdjustableJoystickData.colliderPath", "desc.adjustableJoystick.colliderPath"},
            {"@AdjustableJoystickData.cylinderPath", "desc.adjustableJoystick.cylinderPath"},
            {"@AdjustableJoystickData.height", "desc.adjustableJoystick.height"},
            {"@AttitudeBallData.meshPath", "desc.attitudeBall.meshPath"},
            {"@AttitudeBallData.rotationType", "desc.attitudeBall.rotationType"},
            {"@AttitudeBallData.scale", "desc.attitudeBall.scale"},
            {"@CameraVantageData.autoCenterCamera", "desc.cameraVantage.autoCenterCamera"},
            {"@CameraVantageData.autoOrient", "desc.cameraVantage.autoOrient"},
            {"@CameraVantageData.autoZoomOnCockpit", "desc.cameraVantage.autoZoomOnCockpit"},
            {"@CameraVantageData.cockpitAudio", "desc.cameraVantage.cockpitAudio"},
            {"@CameraVantageData.enableGunReticle", "desc.cameraVantage.enableGunReticle"},
            {"@CameraVantageData.enableMissileLocking", "desc.cameraVantage.enableMissileLocking"},
            {"@CameraVantageData.hidePart", "desc.cameraVantage.hidePart"},
            {"@CameraVantageData.name", "desc.cameraVantage.name"},
            {"@CameraVantageData.offset", "desc.cameraVantage.offset"},
            {"@CameraVantageData.viewMode", "desc.cameraVantage.viewMode"},
            {"@CarEngineData.fuelConsumptionRate", "desc.carEngine.fuelConsumptionRate"},
            {"@CarEngineData.power", "desc.carEngine.power"},
            {"@CarEngineData.throttleResponse", "desc.carEngine.throttleResponse"},
            {"@CatapultConnectorData.catapultAcceleration", "desc.catapultConnector.catapultAcceleration"},
            {"@CatapultConnectorData.targetLaunchSpeed", "desc.catapultConnector.targetLaunchSpeed"},
            {"@CockpitButtonData.buttonInteractionType", "desc.cockpitButton.buttonInteractionType"},
            {"@CockpitButtonData.buttonLightTransitionDelay", "desc.cockpitButton.buttonLightTransitionDelay"},
            {"@CockpitButtonData.buttonLightTransitionTime", "desc.cockpitButton.buttonLightTransitionTime"},
            {"@CockpitButtonData.buttonPositionTransitionDelay", "desc.cockpitButton.buttonPositionTransitionDelay"},
            {"@CockpitButtonData.buttonPositionTransitionTime", "desc.cockpitButton.buttonPositionTransitionTime"},
            {"@CockpitButtonData.depthBase", "desc.cockpitButton.depthBase"},
            {"@CockpitButtonData.depthOff", "desc.cockpitButton.depthOff"},
            {"@CockpitButtonData.depthOn", "desc.cockpitButton.depthOn"},
            {"@CockpitButtonData.height", "desc.cockpitButton.height"},
            {"@CockpitButtonData.input", "desc.cockpitButton.input"},
            {"@CockpitButtonData.lightStrength", "desc.cockpitButton.lightStrength"},
            {"@CockpitButtonData.padding", "desc.cockpitButton.padding"},
            {"@CockpitButtonData.tooltip", "desc.cockpitButton.tooltip"},
            {"@CockpitButtonData.width", "desc.cockpitButton.width"},
            {"@CockpitSoundData.activationGroup", "desc.cockpitSound.activationGroup"},
            {"@CockpitSoundData.intensity", "desc.cockpitSound.intensity"},
            {"@CockpitSwitchData.angleOff", "desc.cockpitSwitch.angleOff"},
            {"@CockpitSwitchData.angleOn", "desc.cockpitSwitch.angleOn"},
            {"@CockpitSwitchData.axis", "desc.cockpitSwitch.axis"},
            {"@CockpitSwitchData.input", "desc.cockpitSwitch.input"},
            {"@CockpitSwitchData.scale", "desc.cockpitSwitch.scale"},
            {"@CockpitSwitchData.switchInteractionType", "desc.cockpitSwitch.switchInteractionType"},
            {"@CockpitSwitchData.switchPositionTransitionDelay", "desc.cockpitSwitch.switchPositionTransitionDelay"},
            {"@CockpitSwitchData.switchPositionTransitionTime", "desc.cockpitSwitch.switchPositionTransitionTime"},
            {"@CockpitSwitchData.tooltip", "desc.cockpitSwitch.tooltip"},
            {"@CounterMeasureDispenserData.activationGroup", "desc.counterMeasure.activationGroup"},
            {"@CounterMeasureDispenserData.ammo", "desc.counterMeasure.ammo"},
            {"@CounterMeasureDispenserData.autoDispenseDelay", "desc.counterMeasure.autoDispenseDelay"},
            {"@CounterMeasureDispenserData.breakLockChance", "desc.counterMeasure.breakLockChance"},
            {"@CounterMeasureDispenserData.countermeasureType", "desc.counterMeasure.countermeasureType"},
            {"@CounterMeasureDispenserData.evadeLockChance", "desc.counterMeasure.evadeLockChance"},
            {"@CounterMeasureDispenserData.launchForce", "desc.counterMeasure.launchForce"},
            {"@CowlFlapsData.hideCowl", "desc.cowlFlaps.hideCowl"},
            {"@CowlFlapsData.usedInPropMode", "desc.cowlFlaps.usedInPropMode"},
            {"@EngineNozzleFlapsData.usedInPropMode", "desc.engineNozzleFlaps.usedInPropMode"},
            {"@EngineThrustPortData.exhaustScale", "desc.engineThrustPort.exhaustScale"},
            {"@GunData.activationGroup", "desc.gun.activationGroup"},
            {"@GunData.ammoCount", "desc.gun.ammoCount"},
            {"@GunData.bulletScale", "desc.gun.bulletScale"},
            {"@GunData.burstCount", "desc.gun.burstCount"},
            {"@GunData.damage", "desc.gun.damage"},
            {"@GunData.impactForce", "desc.gun.impactForce"},
            {"@GunData.lifetime", "desc.gun.lifetime"},
            {"@GunData.minTimeBetweenRounds", "desc.gun.minTimeBetweenRounds"},
            {"@GunData.muzzleFlash", "desc.gun.muzzleFlash"},
            {"@GunData.muzzleVelocity", "desc.gun.muzzleVelocity"},
            {"@GunData.roundsPerSecond", "desc.gun.roundsPerSecond"},
            {"@GunData.spread", "desc.gun.spread"},
            {"@GunData.timeBetweenBursts", "desc.gun.timeBetweenBursts"},
            {"@GunData.tracerColor", "desc.gun.tracerColor"},
            {"@GunData.tracerIntensity", "desc.gun.tracerIntensity"},
            {"@JDifferentialData.coastStiffness", "desc.jDifferential.coastStiffness"},
            {"@JDifferentialData.differentialLock", "desc.jDifferential.differentialLock"},
            {"@JDifferentialData.powerStiffness", "desc.jDifferential.powerStiffness"},
            {"@JDifferentialData.size", "desc.jDifferential.size"},
            {"@JDriveHubData.isReversed", "desc.jDriveHub.isReversed"},
            {"@JDriveShaftData.allowTransformation", "desc.jDriveShaft.allowTransformation"},
            {"@JDriveShaftData.bootA", "desc.jDriveShaft.bootA"},
            {"@JDriveShaftData.bootB", "desc.jDriveShaft.bootB"},
            {"@JDriveShaftData.isVisual", "desc.jDriveShaft.isVisual"},
            {"@JDriveShaftData.localAttachEnd", "desc.jDriveShaft.localAttachEnd"},
            {"@JDriveShaftData.localAttachStart", "desc.jDriveShaft.localAttachStart"},
            {"@JDriveShaftData.radius", "desc.jDriveShaft.radius"},
            {"@JFuselageData.autoResizeOnConnected", "desc.jFuselage.autoResizeOnConnected"},
            {"@JFuselageData.buoyancy", "desc.jFuselage.buoyancy"},
            {"@JFuselageData.buoyancyPermitted", "desc.jFuselage.buoyancyPermitted"},
            {"@JFuselageData.coM", "desc.jFuselage.coM"},
            {"@JFuselageData.colliderCornerSamples", "desc.jFuselage.colliderCornerSamples"},
            {"@JFuselageData.fuelCapacity", "desc.jFuselage.fuelCapacity"},
            {"@JFuselageData.fuelProportion", "desc.jFuselage.fuelProportion"},
            {"@JFuselageData.isCone", "desc.jFuselage.isCone"},
            {"@JFuselageData.isHollow", "desc.jFuselage.isHollow"},
            {"@JFuselageData.isTransparent", "desc.jFuselage.isTransparent"},
            {"@JGearboxData.gearRatio", "desc.jGearbox.gearRatio"},
            {"@JGearboxData.isReversed", "desc.jGearbox.isReversed"},
            {"@JGearboxData.size", "desc.jGearbox.size"},
            {"@JGearboxData.sizePercentage", "desc.jGearbox.sizePercentage"},
            {"@JTransmissionData.finalGearRatio", "desc.jTransmission.finalGearRatio"},
            {"@JTransmissionData.gearProfileType", "desc.jTransmission.gearProfileType"},
            {"@JTransmissionData.numGears", "desc.jTransmission.numGears"},
            {"@JTransmissionData.postShiftBan", "desc.jTransmission.postShiftBan"},
            {"@JTransmissionData.shiftDownRpmPercent", "desc.jTransmission.shiftDownRpmPercent"},
            {"@JTransmissionData.shiftDuration", "desc.jTransmission.shiftDuration"},
            {"@JTransmissionData.shiftGuardSpeedThreshold", "desc.jTransmission.shiftGuardSpeedThreshold"},
            {"@JTransmissionData.shiftUpRpmPercent", "desc.jTransmission.shiftUpRpmPercent"},
            {"@JTransmissionData.size", "desc.jTransmission.size"},
            {"@JTransmissionData.sizePercentage", "desc.jTransmission.sizePercentage"},
            {"@JTransmissionData.transmissionType", "desc.jTransmission.transmissionType"},
            {"@JTransmissionData.variableShift", "desc.jTransmission.variableShift"},
            {"@JWingData.coM", "desc.jWing.coM"},
            {"@JWingData.disableWingtipVortices", "desc.jWing.disableWingtipVortices"},
            {"@JWingData.fuelFraction", "desc.jWing.fuelFraction"},
            {"@JWingData.liftScale", "desc.jWing.liftScale"},
            {"@JWingData.mass", "desc.jWing.mass"},
            {"@JWingData.totalFuelVolume", "desc.jWing.totalFuelVolume"},
            {"@JWingData.viscousDragScale", "desc.jWing.viscousDragScale"},
            {"@JWingData.wingArea", "desc.jWing.wingArea"},
            {"@JWingData.wingSpan", "desc.jWing.wingSpan"},
            {"@JWingData.zeroLiftDragScale", "desc.jWing.zeroLiftDragScale"},
            {"@LabelData.curvatureAngle", "desc.label.curvatureAngle"},
            {"@LabelData.curvatureDirection", "desc.label.curvatureDirection"},
            {"@LabelData.fontName", "desc.label.fontName"},
            {"@LabelData.gradient", "desc.label.gradient"},
            {"@LabelData.horizontalAlignment", "desc.label.horizontalAlignment"},
            {"@LabelData.outlineWidth", "desc.label.outlineWidth"},
            {"@LabelData.paintIndexShift", "desc.label.paintIndexShift"},
            {"@LabelData.performanceCost", "desc.label.performanceCost"},
            {"@LabelData.renderQueueOffset", "desc.label.renderQueueOffset"},
            {"@LabelData.verticalAlignment", "desc.label.verticalAlignment"},
            {"@MfdData.name", "desc.mfd.name"},
            {"@PistonData.input", "desc.piston.input"},
            {"@ProceduralMissileData.defaultFiringDelay", "desc.proceduralMissile.defaultFiringDelay"},
            {"@PropellerAssemblyData.bladeCount", "desc.propeller.bladeCount"},
            {"@PropellerAssemblyData.bladeStyle", "desc.propeller.bladeStyle"},
            {"@PropellerAssemblyData.chordScale", "desc.propeller.chordScale"},
            {"@PropellerAssemblyData.diameter", "desc.propeller.diameter"},
            {"@PropellerAssemblyData.hubMass", "desc.propeller.hubMass"},
            {"@PropellerAssemblyData.hubScale", "desc.propeller.hubScale"},
            {"@PropellerAssemblyData.isManual", "desc.propeller.isManual"},
            {"@PropellerAssemblyData.isPushProp", "desc.propeller.isPushProp"},
            {"@PropellerAssemblyData.mass", "desc.propeller.mass"},
            {"@PropellerAssemblyData.maxPitch", "desc.propeller.maxPitch"},
            {"@PropellerAssemblyData.performanceCost", "desc.propeller.performanceCost"},
            {"@PropellerAssemblyData.reverseRotation", "desc.propeller.reverseRotation"},
            {"@PropellerAssemblyData.thrustScalar", "desc.propeller.thrustScalar"},
            {"@ResizableFuelTankData.size", "desc.resizableFuelTank.size"},
            {"@ResizableShapeData.bounciness", "desc.resizableShape.bounciness"},
            {"@ResizableShapeData.friction", "desc.resizableShape.friction"},
            {"@ResizableShapeData.mass", "desc.resizableShape.mass"},
            {"@ResizableShapeData.size", "desc.resizableShape.size"},
            {"@ResizableWheelData.brakeTorque", "desc.resizableWheel.brakeTorque"},
            {"@ResizableWheelData.damper", "desc.resizableWheel.damper"},
            {"@ResizableWheelData.enableSuspension", "desc.resizableWheel.enableSuspension"},
            {"@ResizableWheelData.frictionScale", "desc.resizableWheel.frictionScale"},
            {"@ResizableWheelData.hideRims", "desc.resizableWheel.hideRims"},
            {"@ResizableWheelData.mass", "desc.resizableWheel.mass"},
            {"@ResizableWheelData.maxAngularVelocity", "desc.resizableWheel.maxAngularVelocity"},
            {"@ResizableWheelData.radius", "desc.resizableWheel.radius"},
            {"@ResizableWheelData.slipForwardAsymptote", "desc.resizableWheel.slipForwardAsymptote"},
            {"@ResizableWheelData.slipForwardExtremum", "desc.resizableWheel.slipForwardExtremum"},
            {"@ResizableWheelData.slipSidewaysAsymptote", "desc.resizableWheel.slipSidewaysAsymptote"},
            {"@ResizableWheelData.slipSidewaysExtremum", "desc.resizableWheel.slipSidewaysExtremum"},
            {"@ResizableWheelData.spring", "desc.resizableWheel.spring"},
            {"@ResizableWheelData.suspensionDistance", "desc.resizableWheel.suspensionDistance"},
            {"@ResizableWheelData.suspensionStiffness", "desc.resizableWheel.suspensionStiffness"},
            {"@ResizableWheelData.thicknessScale", "desc.resizableWheel.thicknessScale"},
            {"@ResizableWheelData.tractionForward", "desc.resizableWheel.tractionForward"},
            {"@ResizableWheelData.tractionSideways", "desc.resizableWheel.tractionSideways"},
            {"@ResizableWheelData.turningAngle", "desc.resizableWheel.turningAngle"},
            {"@ResizableWheelData.turningRate", "desc.resizableWheel.turningRate"},
            {"@RetractableLandingGearData.activationGroup", "desc.retractableGear.activationGroup"},
            {"@SuspensionData.attachPointIndex", "desc.suspension.attachPointIndex"},
            {"@SuspensionData.damper", "desc.suspension.damper"},
            {"@SuspensionData.radius", "desc.suspension.radius"},
            {"@SuspensionData.spring", "desc.suspension.spring"},
            {"@TargetingPodData.activationGroup", "desc.targetingPod.activationGroup"},
            {"@TargetingPodData.cameraOffset", "desc.targetingPod.cameraOffset"},
            {"@TargetingPodData.maxDistance", "desc.targetingPod.maxDistance"},
            {"@TransparencyData.hideBack", "desc.transparency.hideBack"},
            {"@TransparencyData.hideFront", "desc.transparency.hideFront"},
            {"@TransparencyData.hideInside", "desc.transparency.hideInside"},
            {"@TransparencyData.isTransparent", "desc.transparency.isTransparent"},
            {"@TransparencyData.opacity", "desc.transparency.opacity"},
            {"@WinchData.attachPointIndex", "desc.winch.attachPointIndex"},
            {"@WinchData.breakScale", "desc.winch.breakScale"},
            {"@WinchData.minRange", "desc.winch.minRange"},
            {"@WinchData.range", "desc.winch.range"},
            {"@WinchData.speed", "desc.winch.speed"},
            {"@WinchData.startRange", "desc.winch.startRange"},
            {"@WinchData.volume", "desc.winch.volume"},
            {"@WingData.airfoil", "desc.wing.airfoil"},
            {"@WingData.allowControlSurfaces", "desc.wing.allowControlSurfaces"},
            {"@WingData.angleOfAttack", "desc.wing.angleOfAttack"},
            {"@WingData.baseChord", "desc.wing.baseChord"},
            {"@WingData.baseThickness", "desc.wing.baseThickness"},
            {"@WingData.density", "desc.wing.density"},
            {"@WingData.fuelPercentage", "desc.wing.fuelPercentage"},
            {"@WingData.inverted", "desc.wing.inverted"},
            {"@WingData.liftScale", "desc.wing.liftScale"},
            {"@WingData.mass", "desc.wing.mass"},
            {"@WingData.minSectionLength", "desc.wing.minSectionLength"},
            {"@WingData.rootLeadingOffset", "desc.wing.rootLeadingOffset"},
            {"@WingData.rootTrailingOffset", "desc.wing.rootTrailingOffset"},
            {"@WingData.tipChord", "desc.wing.tipChord"},
            {"@WingData.tipLeadingOffset", "desc.wing.tipLeadingOffset"},
            {"@WingData.tipPosition", "desc.wing.tipPosition"},
            {"@WingData.tipThickness", "desc.wing.tipThickness"},
            {"@WingData.tipTrailingOffset", "desc.wing.tipTrailingOffset"},
            {"@WingData.wingArea", "desc.wing.wingArea"},
            {"@WingData.wingSpan", "desc.wing.wingSpan"},
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
