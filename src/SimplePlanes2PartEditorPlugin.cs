using System;
using System.Collections;
using System.Globalization;
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
        public const string PluginVersion = "0.3.4";

        private string _pluginRootPath;
        private string _settingsPath;
        private PluginSettings _settings;
        private LocalizationProvider _localization;
        private DesignerSelectionService _selectionService;
        private ReflectionMemberScanner _memberScanner;
        private PartRuntimeRefreshService _partRuntimeRefreshService;
        private ModificationRecordService _modificationRecordService;
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
            ProbeSelectionIfDue();
            InputCapture.SetWindowState(_isWindowVisible, _window != null && _window.IsMouseOverWindow());
            InputCapture.SetFloatingButtonState(_isDesignerAvailable && IsPointerOverFloatingButton());

            if (_isDesignerAvailable && Input.GetKeyDown(_toggleWindowKeyCode))
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
            if (_window == null)
            {
                InputCapture.SetWindowState(false, false);
                InputCapture.SetFloatingButtonState(false);
                return;
            }

            if (!_isDesignerAvailable)
            {
                _isWindowVisible = false;
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
            _memberScanner = new ReflectionMemberScanner(_settings.MaxMembersPerGroup, _settings.ShowRuntimeCacheMembers);
            _selectionService = new DesignerSelectionService(_memberScanner);
            _partRuntimeRefreshService = new PartRuntimeRefreshService();
            _modificationRecordService = new ModificationRecordService(Logger, _pluginRootPath);
            _updateCheckService = new UpdateCheckService();
            _window = new ImguiPartEditorWindow(_localization, _settings)
            {
                RefreshRequested = RefreshSelection,
                LanguageToggleRequested = ToggleLanguage,
                CopyXmlRequested = CopyCurrentPartXml,
                ModificationRecordingToggleRequested = ToggleModificationRecording,
                ModificationRecordingStateRequested = IsModificationRecordingEnabled,
                TestScriptRequested = RunPartDataTestScript,
                ModificationRecorded = RecordModification,
                StatusChanged = SetStatusText,
                MemberApplied = RefreshPartRuntimeStateAfterApply,
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

        private bool RefreshPartRuntimeStateAfterApply(InspectableMember member)
        {
            bool refreshed = false;

            if (_partRuntimeRefreshService != null)
            {
                refreshed = _partRuntimeRefreshService.TryRefreshAfterApply(member);
            }

            RefreshSelection();
            return refreshed;
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
            RefreshSelection();
        }

        private void ToggleModificationRecording()
        {
            if (_modificationRecordService == null)
            {
                return;
            }

            _modificationRecordService.Toggle();
            _statusText = _modificationRecordService.IsRecording
                ? _localization.Get("status.recorderStarted") + ": " + _modificationRecordService.RecordFilePath
                : _localization.Get("status.recorderStopped") + ": " + _modificationRecordService.RecordFilePath;
        }

        private bool IsModificationRecordingEnabled()
        {
            return _modificationRecordService != null && _modificationRecordService.IsRecording;
        }

        private void RecordModification(ModificationRecordRequest request)
        {
            if (_modificationRecordService != null)
            {
                if (request != null && string.IsNullOrEmpty(request.RefreshMode))
                {
                    request.RefreshMode = GetRefreshModeName(request);
                }

                _modificationRecordService.Record(request);
            }
        }

        private static string GetRefreshModeName(ModificationRecordRequest request)
        {
            if (request == null || request.Group == null)
            {
                return "Unknown";
            }

            if (request.Snapshot != null && ReferenceEquals(request.Group.TargetObject, request.Snapshot.PartDataObject))
            {
                return "PartDataFull";
            }

            return "ModifierLight";
        }

        private void RunPartDataTestScript(SelectedPartSnapshot snapshot)
        {
            InspectableGroup partDataGroup;
            TestScriptSummary summary = new TestScriptSummary();

            if (snapshot == null)
            {
                _statusText = _localization.Get("status.testScriptNoSelection");
                return;
            }

            partDataGroup = GetPartDataGroup(snapshot);
            if (partDataGroup == null)
            {
                _statusText = _localization.Get("status.testScriptNoPartData");
                return;
            }

            EnsureModificationRecordingStarted();
            RunInspectableGroupTests(snapshot, partDataGroup, summary);
            RunModifierGroupTests(snapshot, summary);

            RefreshSelection();
            _statusText = _localization.Get("status.testScriptCompleted") + ": " +
                          summary.PassedCount.ToString(CultureInfo.InvariantCulture) + "/" +
                          summary.AttemptedCount.ToString(CultureInfo.InvariantCulture) +
                          ", skipped " + summary.SkippedCount.ToString(CultureInfo.InvariantCulture);
        }

        private void EnsureModificationRecordingStarted()
        {
            if (_modificationRecordService != null && !_modificationRecordService.IsRecording)
            {
                _modificationRecordService.Start();
            }
        }

        private void RunModifierGroupTests(SelectedPartSnapshot snapshot, TestScriptSummary summary)
        {
            foreach (InspectableGroup group in snapshot.Groups)
            {
                if (ReferenceEquals(group.TargetObject, snapshot.PartDataObject))
                {
                    continue;
                }

                RunInspectableGroupTests(snapshot, group, summary);
            }
        }

        private void RunInspectableGroupTests(SelectedPartSnapshot snapshot, InspectableGroup group, TestScriptSummary summary)
        {
            foreach (InspectableMember member in group.Members)
            {
                if (!member.CanWrite)
                {
                    continue;
                }

                if (ShouldSkipTestMember(member))
                {
                    summary.SkippedCount++;
                    RecordSkippedTest(snapshot, group, member, "protected identity member");
                    continue;
                }

                summary.AttemptedCount++;
                if (RunInspectableMemberTest(snapshot, group, member))
                {
                    summary.PassedCount++;
                }
            }
        }

        private bool RunInspectableMemberTest(SelectedPartSnapshot snapshot, InspectableGroup group, InspectableMember member)
        {
            string originalValue;
            string testValue;
            bool changed;
            bool restored;

            if (member == null)
            {
                return false;
            }

            originalValue = member.Value ?? string.Empty;
            if (!TryCreateTestValue(member, originalValue, out testValue))
            {
                RecordSkippedTest(snapshot, group, member, "unsupported test value type");
                return false;
            }

            changed = ApplyTestValue(snapshot, group, member, "test." + member.Name, originalValue, testValue);
            restored = ApplyTestValue(snapshot, group, member, "restore." + member.Name, member.Value, originalValue);
            return changed && restored;
        }

        private bool ApplyTestValue(SelectedPartSnapshot snapshot, InspectableGroup group, InspectableMember member, string operationName, string beforeValue, string value)
        {
            bool applySucceeded;
            bool refreshSucceeded = false;
            string error = string.Empty;

            member.EditorValue = value;
            applySucceeded = member.TryApply();
            if (applySucceeded)
            {
                refreshSucceeded = _partRuntimeRefreshService != null && _partRuntimeRefreshService.TryRefreshAfterApply(member);
            }
            else
            {
                error = member.Error;
            }

            RecordModification(new ModificationRecordRequest
            {
                OperationName = operationName,
                Snapshot = snapshot,
                Group = group,
                Member = member,
                BeforeValue = beforeValue,
                RequestedValue = value,
                AfterValue = member.Value,
                ApplySucceeded = applySucceeded,
                RefreshSucceeded = refreshSucceeded,
                Error = error
            });

            return applySucceeded && refreshSucceeded;
        }

        private void RecordSkippedTest(SelectedPartSnapshot snapshot, InspectableGroup group, InspectableMember member, string reason)
        {
            RecordModification(new ModificationRecordRequest
            {
                OperationName = member == null ? "skip" : "skip." + member.Name,
                Snapshot = snapshot,
                Group = group,
                Member = member,
                BeforeValue = member == null ? string.Empty : member.Value,
                RequestedValue = member == null ? string.Empty : member.Value,
                AfterValue = member == null ? string.Empty : member.Value,
                ApplySucceeded = false,
                RefreshSucceeded = false,
                Error = reason
            });
        }

        private static InspectableGroup GetPartDataGroup(SelectedPartSnapshot snapshot)
        {
            foreach (InspectableGroup group in snapshot.Groups)
            {
                if (ReferenceEquals(group.TargetObject, snapshot.PartDataObject))
                {
                    return group;
                }
            }

            return null;
        }

        private static bool ShouldSkipTestMember(InspectableMember member)
        {
            string name;

            if (member == null)
            {
                return true;
            }

            name = member.Name ?? string.Empty;
            return string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "PartType", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "PartTypeId", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryCreateTestValue(InspectableMember member, string originalValue, out string testValue)
        {
            Type valueType = Nullable.GetUnderlyingType(member.ValueType) ?? member.ValueType;

            testValue = originalValue;
            if (string.Equals(member.Name, "PartScale", StringComparison.Ordinal))
            {
                return TryCreatePartScaleTestValue(originalValue, out testValue);
            }

            if (valueType == typeof(string))
            {
                testValue = (originalValue ?? string.Empty) + "__sp2test";
                return true;
            }

            if (valueType == typeof(bool))
            {
                testValue = string.Equals(originalValue, "true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
                return true;
            }

            if (valueType.IsEnum)
            {
                return TryCreateEnumTestValue(valueType, originalValue, out testValue);
            }

            if (IsNumericType(valueType))
            {
                return TryCreateNumericTestValue(valueType, originalValue, out testValue);
            }

            if (valueType == typeof(Vector2))
            {
                return TryCreateVectorTestValue(originalValue, 2, out testValue);
            }

            if (valueType == typeof(Vector3))
            {
                return TryCreateVectorTestValue(originalValue, 3, out testValue);
            }

            if (valueType == typeof(Vector4) || valueType == typeof(Color))
            {
                return TryCreateVectorTestValue(originalValue, 4, out testValue);
            }

            if (valueType.IsArray)
            {
                return TryCreateCollectionTestValue(valueType.GetElementType(), originalValue, out testValue);
            }

            if (typeof(IList).IsAssignableFrom(valueType) && valueType.IsGenericType)
            {
                return TryCreateCollectionTestValue(valueType.GetGenericArguments()[0], originalValue, out testValue);
            }

            return false;
        }

        private static bool TryCreatePartScaleTestValue(string originalValue, out string testValue)
        {
            if (string.IsNullOrWhiteSpace(originalValue))
            {
                testValue = "1.05,1,1";
                return true;
            }

            return TryCreateVectorTestValue(originalValue, 3, out testValue);
        }

        private static bool TryCreateEnumTestValue(Type enumType, string originalValue, out string testValue)
        {
            Array values = Enum.GetValues(enumType);
            testValue = originalValue;

            foreach (object enumValue in values)
            {
                string candidate = enumValue.ToString();
                if (!string.Equals(candidate, originalValue, StringComparison.OrdinalIgnoreCase))
                {
                    testValue = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryCreateNumericTestValue(Type valueType, string originalValue, out string testValue)
        {
            decimal number;

            testValue = originalValue;
            if (!decimal.TryParse(originalValue, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                number = 0m;
            }

            if (valueType == typeof(byte) || valueType == typeof(sbyte) ||
                valueType == typeof(short) || valueType == typeof(ushort) ||
                valueType == typeof(int) || valueType == typeof(uint) ||
                valueType == typeof(long) || valueType == typeof(ulong))
            {
                testValue = (number + 1m).ToString(CultureInfo.InvariantCulture);
                return true;
            }

            testValue = (number + 0.125m).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryCreateVectorTestValue(string originalValue, int componentCount, out string testValue)
        {
            string[] components = SplitCsv(originalValue);
            decimal firstComponent;

            if (components.Length != componentCount)
            {
                components = CreateDefaultVectorComponents(componentCount);
            }

            if (!decimal.TryParse(components[0], NumberStyles.Float, CultureInfo.InvariantCulture, out firstComponent))
            {
                firstComponent = 1m;
            }

            components[0] = (firstComponent + 0.05m).ToString(CultureInfo.InvariantCulture);
            testValue = string.Join(",", components);
            return true;
        }

        private static bool TryCreateCollectionTestValue(Type elementType, string originalValue, out string testValue)
        {
            string[] values = SplitCsv(originalValue);
            string firstValue = values.Length == 0 ? string.Empty : values[0];
            string changedValue;

            if (!TryCreateScalarTestValue(elementType, firstValue, out changedValue))
            {
                testValue = originalValue;
                return false;
            }

            if (values.Length == 0)
            {
                testValue = changedValue;
                return true;
            }

            values[0] = changedValue;
            testValue = string.Join(",", values);
            return true;
        }

        private static bool TryCreateScalarTestValue(Type scalarType, string originalValue, out string testValue)
        {
            Type valueType = Nullable.GetUnderlyingType(scalarType) ?? scalarType;

            testValue = originalValue;
            if (valueType == typeof(string))
            {
                testValue = (originalValue ?? string.Empty) + "__sp2test";
                return true;
            }

            if (valueType == typeof(bool))
            {
                testValue = string.Equals(originalValue, "true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
                return true;
            }

            if (valueType.IsEnum)
            {
                return TryCreateEnumTestValue(valueType, originalValue, out testValue);
            }

            return IsNumericType(valueType) && TryCreateNumericTestValue(valueType, originalValue, out testValue);
        }

        private static string[] SplitCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new string[0];
            }

            string[] values = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = values[index].Trim();
            }

            return values;
        }

        private static string[] CreateDefaultVectorComponents(int componentCount)
        {
            string[] components = new string[componentCount];
            for (int index = 0; index < components.Length; index++)
            {
                components[index] = index == 0 ? "1" : "0";
            }

            return components;
        }

        private static bool IsNumericType(Type valueType)
        {
            return valueType == typeof(byte) || valueType == typeof(sbyte) ||
                   valueType == typeof(short) || valueType == typeof(ushort) ||
                   valueType == typeof(int) || valueType == typeof(uint) ||
                   valueType == typeof(long) || valueType == typeof(ulong) ||
                   valueType == typeof(float) || valueType == typeof(double) ||
                   valueType == typeof(decimal);
        }

        private sealed class TestScriptSummary
        {
            public int PassedCount { get; set; }

            public int AttemptedCount { get; set; }

            public int SkippedCount { get; set; }
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


