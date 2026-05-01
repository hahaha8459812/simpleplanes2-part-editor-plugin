using System;
using System.Reflection;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class DesignerSceneDetector
    {
        private const string GameStateTypeName = "Assets.Scripts.GameState";
        private const string SceneManagerTypeName = "Assets.Scripts.Scenes.SceneManager";
        private const string DesignerUiScriptTypeName = "Assets.Scripts.Design.UI.DesignerUIScript";
        private const string DesignerScreenInputScriptTypeName = "Assets.Scripts.Design.UI.Input.DesignerScreenInputScript";
        private static Type _gameStateType;
        private static PropertyInfo _isInDesignerProperty;
        private static PropertyInfo _gameStateInstanceProperty;
        private static Type _sceneManagerType;
        private static PropertyInfo _sceneManagerInstanceProperty;
        private static PropertyInfo _sceneManagerInDesignerProperty;
        private static PropertyInfo _sceneManagerInDesignerSceneProperty;
        private static Type _designerUiScriptType;
        private static Type _designerScreenInputScriptType;
        private bool _isInDesigner = true;
        private bool _hasActiveDesignerUiObject;
        private bool _isResolved;
        private float _nextResolveTime;
        private float _nextDesignerUiObjectCheckTime;

        public bool IsInDesignerScene { get { return _isInDesigner; } }

        public event Action DesignerEntered;
        public event Action DesignerExited;

        public void Update()
        {
            if (!_isResolved)
            {
                if (Time.unscaledTime < _nextResolveTime)
                {
                    return;
                }
                _nextResolveTime = Time.unscaledTime + 2f;
                ResolveTypes();
                if (_isResolved)
                {
                    _isInDesigner = CheckIsInDesigner();
                }
                return;
            }

            bool currently = CheckIsInDesigner();
            if (currently != _isInDesigner)
            {
                _isInDesigner = currently;
                if (currently)
                {
                    if (DesignerEntered != null)
                    {
                        DesignerEntered();
                    }
                }
                else
                {
                    if (DesignerExited != null)
                    {
                        DesignerExited();
                    }
                }
            }
        }

        private bool CheckIsInDesigner()
        {
            bool isInDesigner;

            if (!_isResolved)
            {
                return true;
            }

            if (HasActiveDesignerUiObject())
            {
                return true;
            }

            if (TryReadGameStateDesignerFlag(out isInDesigner))
            {
                return isInDesigner;
            }

            if (TryReadSceneManagerDesignerFlag(out isInDesigner))
            {
                return isInDesigner;
            }

            return true;
        }

        private bool TryReadGameStateDesignerFlag(out bool isInDesigner)
        {
            isInDesigner = true;
            if (_gameStateType == null || _gameStateInstanceProperty == null || _isInDesignerProperty == null)
            {
                return false;
            }

            try
            {
                object instance = _gameStateInstanceProperty.GetValue(null, null);
                if (instance == null)
                {
                    isInDesigner = false;
                    return true;
                }

                isInDesigner = (bool)_isInDesignerProperty.GetValue(instance, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryReadSceneManagerDesignerFlag(out bool isInDesigner)
        {
            isInDesigner = true;
            if (_sceneManagerType == null || _sceneManagerInstanceProperty == null)
            {
                return false;
            }

            try
            {
                object instance = _sceneManagerInstanceProperty.GetValue(null, null);
                if (instance == null)
                {
                    isInDesigner = false;
                    return true;
                }

                if (_sceneManagerInDesignerProperty != null)
                {
                    isInDesigner = (bool)_sceneManagerInDesignerProperty.GetValue(instance, null);
                    return true;
                }

                if (_sceneManagerInDesignerSceneProperty != null)
                {
                    isInDesigner = (bool)_sceneManagerInDesignerSceneProperty.GetValue(instance, null);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private void ResolveTypes()
        {
            if (_isResolved)
            {
                return;
            }

            try
            {
                _gameStateType = FindType(GameStateTypeName);
                if (_gameStateType == null)
                {
                    return;
                }
                _gameStateInstanceProperty = _gameStateType.GetProperty("Instance", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (_gameStateInstanceProperty == null)
                {
                    return;
                }
                _isInDesignerProperty = _gameStateType.GetProperty("IsInDesigner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (_isInDesignerProperty == null)
                {
                    return;
                }

                ResolveSceneManagerType();
                ResolveDesignerUiTypes();
                _isResolved = true;
            }
            catch
            {
            }
        }

        private void ResolveSceneManagerType()
        {
            _sceneManagerType = FindType(SceneManagerTypeName);
            if (_sceneManagerType == null)
            {
                return;
            }

            _sceneManagerInstanceProperty = _sceneManagerType.GetProperty("Instance", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _sceneManagerInDesignerProperty = _sceneManagerType.GetProperty("InDesigner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _sceneManagerInDesignerSceneProperty = _sceneManagerType.GetProperty("InDesignerScene", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private void ResolveDesignerUiTypes()
        {
            _designerUiScriptType = FindType(DesignerUiScriptTypeName);
            _designerScreenInputScriptType = FindType(DesignerScreenInputScriptTypeName);
        }

        private bool HasActiveDesignerUiObject()
        {
            if (Time.unscaledTime < _nextDesignerUiObjectCheckTime)
            {
                return _hasActiveDesignerUiObject;
            }

            _nextDesignerUiObjectCheckTime = Time.unscaledTime + 0.5f;
            _hasActiveDesignerUiObject = HasActiveObjectOfType(_designerUiScriptType) ||
                                         HasActiveObjectOfType(_designerScreenInputScriptType);
            return _hasActiveDesignerUiObject;
        }

        private static bool HasActiveObjectOfType(Type type)
        {
            UnityEngine.Object[] objects;

            if (type == null)
            {
                return false;
            }

            try
            {
                objects = UnityEngine.Object.FindObjectsOfType(type);
            }
            catch
            {
                return false;
            }

            for (int index = 0; index < objects.Length; index++)
            {
                if (IsUnityObjectActive(objects[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnityObjectActive(UnityEngine.Object unityObject)
        {
            Behaviour behaviour = unityObject as Behaviour;
            if (behaviour != null)
            {
                return behaviour.isActiveAndEnabled &&
                       behaviour.gameObject != null &&
                       behaviour.gameObject.activeInHierarchy;
            }

            GameObject gameObject = unityObject as GameObject;
            if (gameObject != null)
            {
                return gameObject.activeInHierarchy;
            }

            Component component = unityObject as Component;
            return component != null &&
                   component.gameObject != null &&
                   component.gameObject.activeInHierarchy;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
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
