using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SimplePlanes2PartEditorPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.codex.simpleplanes2.parteditor";
        public const string PluginName = "SimplePlanes 2 Part Editor";
        public const string PluginVersion = "0.4.1";

        private string _pluginRootPath;
        private string _settingsPath;
        private PluginSettings _settings;
        private LocalizationProvider _localization;
        private DesignerSelectionService _selectionService;
        private ReflectionMemberScanner _memberScanner;
        private InspectableMemberDescriptionProvider _descriptionProvider;
        private PartRuntimeRefreshService _partRuntimeRefreshService;
        private DesignerCameraLimitService _designerCameraLimitService;
        private DesignerSceneDetector _designerSceneDetector;
        private UpdateCheckService _updateCheckService;
        private ImguiPartEditorWindow _window;
        private SelectionReadResult _currentSelection;
        private readonly object _updateNoticeLock = new object();
        private UpdateCheckResult _availableUpdate;
        private bool _isWindowVisible;
        private bool _isDesignerAvailable;
        private bool _updateCheckStarted;
        private bool _isFloatingButtonDragging;
        private bool _floatingButtonMoved;
        private Vector2 _floatingButtonDragOffset;
        private float _nextSelectionRefreshTime;
        private string _statusText;
        private KeyCode _toggleWindowKeyCode;
        private Harmony _harmony;
        private Texture2D _floatingButtonTexture;
        private Texture2D _floatingButtonActiveTexture;
        private GUIStyle _floatingButtonTopStyle;
        private GUIStyle _floatingButtonBottomStyle;

        private void Awake()
        {
            _pluginRootPath = Path.GetDirectoryName(Info.Location);
            _settingsPath = Path.Combine(_pluginRootPath, "settings.json");
            EnsureRuntimeFiles();
            LoadSettingsAndServices();
            _harmony = new Harmony(PluginGuid);
            HarmonyInputPatch.Install(_harmony, Logger);
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            bool designerUiActive;

            ProbeSelectionIfDue();
            ApplyDesignerCameraLimit();
            if (_designerSceneDetector != null)
            {
                _designerSceneDetector.Update();
            }

            designerUiActive = IsDesignerUiActive();
            if (!designerUiActive)
            {
                _isWindowVisible = false;
                _isFloatingButtonDragging = false;
            }

            InputCapture.SetWindowState(_isWindowVisible, designerUiActive && _window != null && _window.IsMouseOverWindow());
            InputCapture.SetFloatingButtonState(designerUiActive && IsPointerOverFloatingButton());

            if (_settings.ToggleWindowHotkeyEnabled && designerUiActive && Input.GetKeyDown(_toggleWindowKeyCode))
            {
                _isWindowVisible = !_isWindowVisible;
                if (_isWindowVisible)
                {
                    RefreshSelection();
                }

                InputCapture.SetWindowState(_isWindowVisible, _window != null && _window.IsMouseOverWindow());
            }
        }

        private void OnGUI()
        {
            bool designerUiActive;

            if (_window == null)
            {
                InputCapture.SetWindowState(false, false);
                InputCapture.SetFloatingButtonState(false);
                return;
            }

            designerUiActive = IsDesignerUiActive();
            if (!designerUiActive)
            {
                _isWindowVisible = false;
                _isFloatingButtonDragging = false;
                InputCapture.SetWindowState(false, false);
                InputCapture.SetFloatingButtonState(false);
                return;
            }

            if (_isWindowVisible)
            {
                StartUpdateCheckOnFirstPanelOpen();
                _window.Draw(_currentSelection, _statusText, GetUpdateNoticeText());
                InputCapture.SetWindowState(true, _window.IsMouseOverWindow());
            }
            else
            {
                InputCapture.SetWindowState(false, false);
            }

            DrawFloatingButton();
            InputCapture.SetFloatingButtonState(IsPointerOverFloatingButton() || _isFloatingButtonDragging);
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }
        }

        private void LoadSettingsAndServices()
        {
            _settings = File.Exists(_settingsPath)
                ? PluginSettings.FromJson(File.ReadAllText(_settingsPath, System.Text.Encoding.UTF8))
                : PluginSettings.CreateDefault();

            _toggleWindowKeyCode = ParseKeyCode(_settings.ToggleWindowHotkey, KeyCode.F8);
            _localization = new LocalizationProvider(Path.Combine(_pluginRootPath, "localization"));
            _localization.Load(_settings.Language);
            _descriptionProvider = new InspectableMemberDescriptionProvider(_localization);
            _memberScanner = new ReflectionMemberScanner(_settings.MaxMembersPerGroup, _settings.ShowRuntimeCacheMembers, _descriptionProvider);
            _selectionService = new DesignerSelectionService(_memberScanner);
            _partRuntimeRefreshService = new PartRuntimeRefreshService();
            _designerCameraLimitService = new DesignerCameraLimitService();
            _designerSceneDetector = new DesignerSceneDetector();
            _updateCheckService = new UpdateCheckService();
            _window = new ImguiPartEditorWindow(_localization, _settings)
            {
                RefreshRequested = RefreshSelection,
                LanguageToggleRequested = ToggleLanguage,
                CopyXmlRequested = CopyCurrentPartXml,
                StatusChanged = SetStatusText,
                MemberApplied = RefreshPartRuntimeStateAfterApply,
                DirectObjectApplied = RefreshRuntimeStateAfterDirectApply,
                SettingsSaveRequested = SaveSettings
            };
            _statusText = _localization.Get("status.ready");
        }

        private void StartUpdateCheckOnFirstPanelOpen()
        {
            if (_updateCheckStarted || _updateCheckService == null || _settings == null ||
                !_settings.UpdateCheckEnabled || string.IsNullOrEmpty(_settings.UpdateIndexUrl))
            {
                return;
            }

            _updateCheckStarted = true;
            _updateCheckService.CheckForUpdates(_settings.UpdateIndexUrl, PluginVersion, OnUpdateCheckCompleted);
        }

        private void OnUpdateCheckCompleted(UpdateCheckResult result)
        {
            if (result == null || !result.HasUpdate)
            {
                return;
            }

            lock (_updateNoticeLock)
            {
                _availableUpdate = result;
            }
        }

        private string GetUpdateNoticeText()
        {
            lock (_updateNoticeLock)
            {
                return _availableUpdate == null ? string.Empty : FormatUpdateNotice(_availableUpdate);
            }
        }

        private string FormatUpdateNotice(UpdateCheckResult result)
        {
            string notice = _localization.Get("update.available") + " " +
                            _localization.Get("update.currentVersion") + " " + PluginVersion + " / " +
                            _localization.Get("update.latestVersion") + " " + result.LatestVersion;

            if (!string.IsNullOrEmpty(result.ReleaseNotes))
            {
                notice += "\n" + _localization.Get("update.releaseNotes") + "\n" + result.ReleaseNotes;
            }

            return notice;
        }

        private void RefreshSelection()
        {
            _currentSelection = _selectionService.ReadSelection();
            _isDesignerAvailable = _currentSelection == null || !IsNoDesignerStatus(_currentSelection.StatusKey);
            _nextSelectionRefreshTime = Time.unscaledTime + _settings.SelectionRefreshIntervalSeconds;
            _statusText = _localization.Get("status.refreshed");
        }

        private void ProbeSelectionIfDue()
        {
            SelectionProbeResult probeResult;

            if (_selectionService == null || _settings == null || Time.unscaledTime < _nextSelectionRefreshTime)
            {
                return;
            }

            _nextSelectionRefreshTime = Time.unscaledTime + _settings.SelectionRefreshIntervalSeconds;
            probeResult = _selectionService.ProbeSelection();
            _isDesignerAvailable = probeResult.IsDesignerAvailable;

            if (!_isDesignerAvailable)
            {
                _isWindowVisible = false;
                _currentSelection = SelectionReadResult.FromStatus(probeResult.StatusKey);
                return;
            }

            if (!_isWindowVisible || InputCapture.TextInputFocused || InputCapture.PointerOverWindow)
            {
                return;
            }

            RefreshSelectionWhenProbeChanged(probeResult);
        }

        private void RefreshSelectionWhenProbeChanged(SelectionProbeResult probeResult)
        {
            if (probeResult.HasSelectedPart)
            {
                if (_currentSelection == null || !probeResult.IsSameSelection(_currentSelection.Snapshot))
                {
                    RefreshSelection();
                }

                return;
            }

            if (_currentSelection == null || !probeResult.IsSameStatus(_currentSelection))
            {
                _currentSelection = SelectionReadResult.FromStatus(probeResult.StatusKey);
            }
        }

        private void ToggleLanguage()
        {
            string nextLanguage = string.Equals(_settings.Language, "zh-CN", StringComparison.OrdinalIgnoreCase) ? "en-US" : "zh-CN";
            _settings.SetLanguage(nextLanguage);
            File.WriteAllText(_settingsPath, _settings.ToJson(), System.Text.Encoding.UTF8);
            _localization.Load(_settings.Language);
            _statusText = _localization.Get("status.ready");
        }

        private void CopyCurrentPartXml()
        {
            if (_currentSelection == null || _currentSelection.Snapshot == null || string.IsNullOrEmpty(_currentSelection.Snapshot.XmlText))
            {
                _statusText = _localization.Get("status.copyXmlUnavailable");
                return;
            }

            GUIUtility.systemCopyBuffer = _currentSelection.Snapshot.XmlText;
            _statusText = _localization.Get("status.copiedXml");
        }

        private void SetStatusText(string statusText)
        {
            _statusText = statusText;
        }

        private void RefreshPartRuntimeStateAfterApply(InspectableMember member)
        {
            if (_partRuntimeRefreshService != null)
            {
                _partRuntimeRefreshService.TryRefreshAfterApply(member);
            }

            RefreshSelection();
        }

        private void RefreshRuntimeStateAfterDirectApply(object target, System.Collections.Generic.IEnumerable<string> propertyNames, string value)
        {
            if (_partRuntimeRefreshService != null)
            {
                _partRuntimeRefreshService.TryRefreshTargetAfterApply(target, propertyNames, value);
            }

            RefreshSelection();
        }

        private void SaveSettings()
        {
            File.WriteAllText(_settingsPath, _settings.ToJson(), System.Text.Encoding.UTF8);
            _toggleWindowKeyCode = ParseKeyCode(_settings.ToggleWindowHotkey, KeyCode.F8);
            if (_memberScanner != null)
            {
                _memberScanner.ShowRuntimeCacheMembers = _settings.ShowRuntimeCacheMembers;
            }

            _nextSelectionRefreshTime = 0f;
            ResetUpdateNotice();
            ApplyDesignerCameraLimit();
            RefreshSelection();
        }

        private void ApplyDesignerCameraLimit()
        {
            if (_designerCameraLimitService != null && _settings != null)
            {
                _designerCameraLimitService.ApplyMaxDistance(_settings.DesignerCameraMaxDistance);
            }
        }

        private void ResetUpdateNotice()
        {
            _updateCheckStarted = false;
            lock (_updateNoticeLock)
            {
                _availableUpdate = null;
            }
        }

        private void DrawFloatingButton()
        {
            float size = _settings.FloatingButtonSize;
            Rect buttonRect = new Rect(_settings.FloatingButtonX, _settings.FloatingButtonY, size, size);
            Event currentEvent = Event.current;

            if (currentEvent != null)
            {
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && buttonRect.Contains(currentEvent.mousePosition))
                {
                    _isFloatingButtonDragging = true;
                    _floatingButtonMoved = false;
                    _floatingButtonDragOffset = currentEvent.mousePosition - new Vector2(buttonRect.x, buttonRect.y);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && _isFloatingButtonDragging)
                {
                    if (!_settings.LockFloatingButtonPosition)
                    {
                        Vector2 position = currentEvent.mousePosition - _floatingButtonDragOffset;
                        position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, Screen.width - size));
                        position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, Screen.height - size));
                        _settings.SetFloatingButtonPosition(position.x, position.y);
                        _floatingButtonMoved = true;
                    }

                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp && _isFloatingButtonDragging)
                {
                    _isFloatingButtonDragging = false;
                    if (_floatingButtonMoved)
                    {
                        SaveSettings();
                    }
                    else
                    {
                        _isWindowVisible = !_isWindowVisible;
                        if (_isWindowVisible)
                        {
                            RefreshSelection();
                        }
                    }

                    currentEvent.Use();
                }
            }

            buttonRect = new Rect(_settings.FloatingButtonX, _settings.FloatingButtonY, size, size);
            GUI.DrawTexture(buttonRect, _isWindowVisible ? GetFloatingButtonActiveTexture() : GetFloatingButtonTexture());
            GUI.Label(new Rect(buttonRect.x, buttonRect.y + size * 0.16f, buttonRect.width, size * 0.34f), "SP2", GetFloatingButtonTopStyle());
            GUI.Label(new Rect(buttonRect.x, buttonRect.y + size * 0.52f, buttonRect.width, size * 0.28f), "Editor", GetFloatingButtonBottomStyle());
        }

        private bool IsPointerOverFloatingButton()
        {
            float size = _settings == null ? 52f : _settings.FloatingButtonSize;
            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            Rect buttonRect = new Rect(_settings.FloatingButtonX, _settings.FloatingButtonY, size, size);
            return buttonRect.Contains(mousePosition);
        }

        private Texture2D GetFloatingButtonTexture()
        {
            if (_floatingButtonTexture == null)
            {
                _floatingButtonTexture = CreateRoundedTexture(
                    96,
                    12,
                    ColorFromHex(0x28, 0x59, 0x9e, 0.98f),
                    ColorFromHex(0x20, 0x49, 0x83, 1f),
                    ColorFromHex(0x28, 0x59, 0x9e, 0.98f));
            }

            return _floatingButtonTexture;
        }

        private Texture2D GetFloatingButtonActiveTexture()
        {
            if (_floatingButtonActiveTexture == null)
            {
                _floatingButtonActiveTexture = CreateRoundedTexture(
                    96,
                    12,
                    ColorFromHex(0x32, 0x6f, 0xc6, 0.98f),
                    ColorFromHex(0x28, 0x5c, 0xa5, 1f),
                    ColorFromHex(0x32, 0x6f, 0xc6, 0.98f));
            }

            return _floatingButtonActiveTexture;
        }

        private GUIStyle GetFloatingButtonTopStyle()
        {
            if (_floatingButtonTopStyle == null)
            {
                _floatingButtonTopStyle = new GUIStyle(GUI.skin.label);
                _floatingButtonTopStyle.alignment = TextAnchor.MiddleCenter;
                _floatingButtonTopStyle.normal.textColor = Color.white;
                _floatingButtonTopStyle.fontStyle = FontStyle.Bold;
            }

            _floatingButtonTopStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(_settings.FloatingButtonSize * 0.28f), 10, 26);
            return _floatingButtonTopStyle;
        }

        private GUIStyle GetFloatingButtonBottomStyle()
        {
            if (_floatingButtonBottomStyle == null)
            {
                _floatingButtonBottomStyle = new GUIStyle(GUI.skin.label);
                _floatingButtonBottomStyle.alignment = TextAnchor.MiddleCenter;
                _floatingButtonBottomStyle.normal.textColor = new Color(0.86f, 0.94f, 1f, 1f);
                _floatingButtonBottomStyle.fontStyle = FontStyle.Bold;
            }

            _floatingButtonBottomStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(_settings.FloatingButtonSize * 0.18f), 8, 18);
            return _floatingButtonBottomStyle;
        }

        private static Texture2D CreateRoundedTexture(int size, int radius, Color fill, Color border, Color highlight)
        {
            Texture2D texture = new Texture2D(size, size);
            float centerMax = size - radius - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, radius, centerMax);
                    float cy = Mathf.Clamp(y, radius, centerMax);
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else if (x < 4 || y < 4 || x >= size - 4 || y >= size - 4 || distance > radius - 4)
                    {
                        texture.SetPixel(x, y, border);
                    }
                    else
                    {
                        texture.SetPixel(x, y, fill);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private static Color ColorFromHex(byte red, byte green, byte blue, float alpha)
        {
            return new Color(red / 255f, green / 255f, blue / 255f, alpha);
        }

        private static bool IsNoDesignerStatus(string statusKey)
        {
            return string.Equals(statusKey, "label.noDesigner", StringComparison.Ordinal);
        }

        private bool IsDesignerUiActive()
        {
            return _designerSceneDetector != null && _designerSceneDetector.IsInDesignerScene;
        }

        private void EnsureRuntimeFiles()
        {
            string localizationDirectory = Path.Combine(_pluginRootPath, "localization");
            Directory.CreateDirectory(localizationDirectory);

            if (!File.Exists(_settingsPath))
            {
                File.WriteAllText(_settingsPath, PluginSettings.CreateDefault().ToJson(), System.Text.Encoding.UTF8);
            }
        }

        private static KeyCode ParseKeyCode(string keyName, KeyCode fallback)
        {
            try
            {
                return (KeyCode)Enum.Parse(typeof(KeyCode), keyName, true);
            }
            catch
            {
                return fallback;
            }
        }
    }
}




