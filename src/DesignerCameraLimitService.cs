using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class DesignerCameraLimitService
    {
        private const string DesignerTypeName = "Assets.Scripts.Design.Designer";
        private const string CameraControllerPropertyName = "CameraController";
        private const string CamerasFieldName = "_cameras";
        private const string DefaultFarPlaneFieldName = "_defaultFarPlane";
        private const string MaxDistanceFieldName = "_maxDistance";
        private const float FarClipPlaneMultiplier = 4f;
        private const float MinimumFarClipPlane = 2000f;

        private Type _designerType;
        private PropertyInfo _designerInstanceProperty;
        private FieldInfo _designerInstanceField;
        private PropertyInfo _cameraControllerProperty;
        private FieldInfo _camerasField;
        private FieldInfo _defaultFarPlaneField;
        private FieldInfo _maxDistanceField;
        private Type _cameraControllerType;
        private object _lastCameraController;

        public void ApplyMaxDistance(float maxDistance)
        {
            object designerInstance;
            object cameraController;

            if (maxDistance <= 0f || !EnsureDesignerTypeResolved())
            {
                return;
            }

            designerInstance = GetDesignerInstance();
            if (designerInstance == null)
            {
                _lastCameraController = null;
                return;
            }

            cameraController = GetCameraController(designerInstance);
            if (cameraController == null)
            {
                _lastCameraController = null;
                return;
            }

            if (!EnsureCameraControllerFieldsResolved(cameraController.GetType()))
            {
                return;
            }

            _maxDistanceField.SetValue(cameraController, maxDistance);
            ApplyFarClipPlane(cameraController, maxDistance);
            _lastCameraController = cameraController;
        }

        private bool EnsureDesignerTypeResolved()
        {
            if (_designerType != null)
            {
                return true;
            }

            _designerType = FindType(DesignerTypeName);
            if (_designerType == null)
            {
                return false;
            }

            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _designerInstanceProperty = _designerType.GetProperty("Instance", flags);
            _designerInstanceField = _designerType.GetField("Instance", flags);
            _cameraControllerProperty = _designerType.GetProperty(CameraControllerPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return true;
        }

        private object GetDesignerInstance()
        {
            if (_designerInstanceProperty != null)
            {
                return _designerInstanceProperty.GetValue(null, null);
            }

            return _designerInstanceField == null ? null : _designerInstanceField.GetValue(null);
        }

        private object GetCameraController(object designerInstance)
        {
            if (_cameraControllerProperty == null)
            {
                return null;
            }

            try
            {
                return _cameraControllerProperty.GetValue(designerInstance, null);
            }
            catch
            {
                return null;
            }
        }

        private bool EnsureCameraControllerFieldsResolved(Type cameraControllerType)
        {
            if (_maxDistanceField != null && _cameraControllerType == cameraControllerType)
            {
                return true;
            }

            _maxDistanceField = cameraControllerType.GetField(MaxDistanceFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _camerasField = cameraControllerType.GetField(CamerasFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _defaultFarPlaneField = cameraControllerType.GetField(DefaultFarPlaneFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _cameraControllerType = cameraControllerType;
            return _maxDistanceField != null;
        }

        private void ApplyFarClipPlane(object cameraController, float maxDistance)
        {
            float farClipPlane = Math.Max(MinimumFarClipPlane, maxDistance * FarClipPlaneMultiplier);

            if (_defaultFarPlaneField != null)
            {
                _defaultFarPlaneField.SetValue(cameraController, farClipPlane);
            }

            foreach (Camera camera in GetCameras(cameraController))
            {
                if (camera != null && camera.farClipPlane < farClipPlane)
                {
                    camera.farClipPlane = farClipPlane;
                }
            }
        }

        private IEnumerable GetCameras(object cameraController)
        {
            IEnumerable cameras = null;
            if (_camerasField != null)
            {
                cameras = _camerasField.GetValue(cameraController) as IEnumerable;
            }

            return cameras ?? new Camera[0];
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
