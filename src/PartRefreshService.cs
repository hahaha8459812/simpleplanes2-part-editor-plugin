using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimplePlanes2PartEditor
{
    internal sealed class PartRefreshResult
    {
        public PartRefreshResult(int invokedMethodCount, string error)
        {
            InvokedMethodCount = invokedMethodCount;
            Error = error ?? string.Empty;
        }

        public int InvokedMethodCount { get; private set; }

        public string Error { get; private set; }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(Error); }
        }
    }

    internal static class PartRefreshService
    {
        private static readonly string[] RefreshMethodNames =
        {
            "OnValidate",
            "Validate",
            "Refresh",
            "RefreshPart",
            "RefreshData",
            "Rebuild",
            "RebuildPart",
            "UpdatePart",
            "UpdatePartData",
            "UpdateModifiers",
            "UpdateVisuals",
            "UpdateMesh",
            "Apply",
            "ApplyState",
            "Recalculate",
            "RecalculateMass",
            "RecalculateDrag",
            "CalculateMass",
            "CalculateDrag"
        };

        public static PartRefreshResult TryRefresh(SelectedPartSnapshot snapshot, InspectableGroup group)
        {
            List<object> targets = new List<object>();
            string error;
            int invokedMethodCount = 0;

            AddUniqueTarget(targets, group == null ? null : group.TargetObject);
            AddUniqueTarget(targets, snapshot == null ? null : snapshot.PartDataObject);
            AddUniqueTarget(targets, snapshot == null ? null : snapshot.SelectedPartObject);

            foreach (object target in targets)
            {
                invokedMethodCount += InvokeRefreshMethods(target, out error);
                if (!string.IsNullOrEmpty(error))
                {
                    return new PartRefreshResult(invokedMethodCount, error);
                }
            }

            return new PartRefreshResult(invokedMethodCount, string.Empty);
        }

        private static void AddUniqueTarget(List<object> targets, object target)
        {
            if (target == null || targets.Contains(target))
            {
                return;
            }

            targets.Add(target);
        }

        private static int InvokeRefreshMethods(object target, out string error)
        {
            Type type = target.GetType();
            int invokedMethodCount = 0;

            error = string.Empty;
            foreach (string methodName in RefreshMethodNames)
            {
                MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method == null || method.ContainsGenericParameters)
                {
                    continue;
                }

                try
                {
                    method.Invoke(target, null);
                    invokedMethodCount++;
                }
                catch (Exception exception)
                {
                    error = methodName + ": " + exception.GetType().Name;
                    return invokedMethodCount;
                }
            }

            return invokedMethodCount;
        }
    }
}
