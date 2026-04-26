using System;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;

namespace SimplePlanes2PartEditor
{
    internal static class HarmonyInputPatch
    {
        public static void Install(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            PatchMethod(harmony, logger, "Assets.Scripts.Design.Designer", "HandleInput", "Designer.HandleInput");
            PatchMethod(harmony, logger, "Assets.Scripts.Design.DesignerScript", "HandleInput", "DesignerScript.HandleInput");
            PatchMethod(harmony, logger, "Assets.Scripts.Design.DesignerScript", "HandleScroll", "DesignerScript.HandleScroll");
            PatchMethod(harmony, logger, "Assets.Scripts.Design.DesignerScript", "HandleCameraMovementInputs", "DesignerScript.HandleCameraMovementInputs");
            PatchMethod(harmony, logger, "Assets.Scripts.Design.Tools.DesignerTool", "HandleInput", "DesignerTool.HandleInput");
            PatchGenerateXml(harmony, logger);
            PatchGenerateModifierStateXml(harmony, logger);
        }

        private static void PatchMethod(Harmony harmony, BepInEx.Logging.ManualLogSource logger, string typeName, string methodName, string label)
        {
            Type targetType = FindType(typeName);
            MethodInfo targetMethod;
            MethodInfo prefixMethod;

            if (targetType == null)
            {
                logger.LogWarning(label + " patch target type was not found.");
                return;
            }

            targetMethod = AccessTools.Method(targetType, methodName);
            prefixMethod = AccessTools.Method(typeof(HarmonyInputPatch), "BlockWhenPointerOverWindowPrefix");
            if (targetMethod == null || prefixMethod == null)
            {
                logger.LogWarning(label + " patch target method was not found.");
                return;
            }

            harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
            logger.LogInfo("Patched " + label + " for editor window input capture.");
        }

        private static void PatchGenerateXml(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            Type partDataType = FindType("Assets.Scripts.Craft.Parts.PartData");
            MethodInfo targetMethod;
            MethodInfo postfixMethod;

            if (partDataType == null)
            {
                logger.LogWarning("PartData.GenerateXml patch target type was not found.");
                return;
            }

            targetMethod = AccessTools.Method(partDataType, "GenerateXml");
            postfixMethod = AccessTools.Method(typeof(HarmonyInputPatch), "PartDataGenerateXmlPostfix");
            if (targetMethod == null || postfixMethod == null)
            {
                logger.LogWarning("PartData.GenerateXml patch target method was not found.");
                return;
            }

            harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
            logger.LogInfo("Patched PartData.GenerateXml for custom XML attributes.");
        }

        private static void PatchGenerateModifierStateXml(Harmony harmony, BepInEx.Logging.ManualLogSource logger)
        {
            Type modifierDataType = FindType("Assets.Scripts.Craft.Parts.Modifiers.PartModifierData");
            MethodInfo targetMethod;
            MethodInfo postfixMethod;

            if (modifierDataType == null)
            {
                logger.LogWarning("PartModifierData.GenerateStateXml patch target type was not found.");
                return;
            }

            targetMethod = AccessTools.Method(modifierDataType, "GenerateStateXml");
            postfixMethod = AccessTools.Method(typeof(HarmonyInputPatch), "PartModifierDataGenerateStateXmlPostfix");
            if (targetMethod == null || postfixMethod == null)
            {
                logger.LogWarning("PartModifierData.GenerateStateXml patch target method was not found.");
                return;
            }

            harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
            logger.LogInfo("Patched PartModifierData.GenerateStateXml for custom XML attributes.");
        }

        private static bool BlockWhenPointerOverWindowPrefix()
        {
            return !InputCapture.ShouldBlockGameInput;
        }

        private static void PartDataGenerateXmlPostfix(object __instance, ref XElement __result)
        {
            CustomXmlAttributeStore.ApplyToXml(__instance, __result);
        }

        private static void PartModifierDataGenerateStateXmlPostfix(object __instance, ref XElement __result)
        {
            CustomXmlAttributeStore.ApplyToXml(__instance, __result);
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
