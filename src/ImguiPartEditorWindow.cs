using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SimplePlanes2PartEditor
{
    internal sealed class ImguiPartEditorWindow
    {
        private readonly LocalizationProvider _localization;
        private readonly PluginSettings _settings;
        private Rect _windowRect;
        private Rect _expandedEditorRect;
        private Vector2 _scrollPosition;
        private Vector2 _expandedEditorScrollPosition;
        private string _searchTerm = string.Empty;
        private bool _layoutInitialized;
        private bool _expandedEditorLayoutInitialized;
        private bool _showSettings;
        private bool _partInfoExpanded = true;
        private int _selectedGroupIndex;
        private InspectableMember _expandedEditorMember;
        private InspectableGroup _expandedEditorGroup;
        private SelectedPartSnapshot _expandedEditorSnapshot;
        private bool _draggingWindow;
        private bool _draggingExpandedEditor;
        private bool _resizingExpandedEditor;
        private bool _expandedEditorLayoutChanged;
        private Vector2 _dragOffset;
        private Vector2 _expandedEditorDragOffset;
        private string _fontSizeText;
        private string _windowWidthText;
        private string _windowHeightText;
        private string _backgroundOpacityText;
        private string _hotkeyText;
        private bool _updateCheckEnabled;
        private string _updateIndexUrlText;
        private string _selectionRefreshIntervalText;
        private string _floatingButtonSizeText;
        private bool _lockFloatingButtonPosition;
        private bool _showTypeColumn;
        private bool _showAccessColumn;
        private bool _showFullTypeName;
        private bool _showRuntimeCacheMembers;
        private string _customXmlAttributeName = string.Empty;
        private string _customXmlAttributeValue = string.Empty;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _mutedLabelStyle;
        private GUIStyle _headerLabelStyle;
        private GUIStyle _headerLabelRightStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _textAreaStyle;
        private GUIStyle _readOnlyValueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _columnHeaderStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _rowAltStyle;

        private bool _alternateRow;
        private Texture2D _sectionBackgroundTexture;
        private Texture2D _textFieldBackgroundTexture;
        private Texture2D _buttonBackgroundTexture;
        private Texture2D _buttonHoverTexture;
        private Texture2D _buttonActiveTexture;
        private Texture2D _rowBackgroundTexture;
        private Texture2D _rowAltBackgroundTexture;

        public ImguiPartEditorWindow(LocalizationProvider localization, PluginSettings settings)
        {
            _localization = localization;
            _settings = settings;
            LoadSettingsText();
            _windowRect = new Rect(80f, 60f, _settings.WindowWidth, _settings.WindowHeight);
            _expandedEditorRect = new Rect(_settings.ExpandedEditorX, _settings.ExpandedEditorY, _settings.ExpandedEditorWidth, _settings.ExpandedEditorHeight);
        }

        public Action RefreshRequested { get; set; }

        public Action LanguageToggleRequested { get; set; }

        public Action CopyXmlRequested { get; set; }

        public Action<string> StatusChanged { get; set; }

        public Action SettingsSaveRequested { get; set; }

        public Action<InspectableMember> MemberApplied { get; set; }

        public bool IsMouseOverWindow()
        {
            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return _windowRect.Contains(mousePosition) || IsMouseOverExpandedEditor(mousePosition);
        }

        public void Draw(SelectionReadResult selectionReadResult, string statusText, string updateNoticeText)
        {
            InitializeLayoutIfNeeded();
            ApplySkin();
            ConsumeScrollWheelBeforePanel();
            InitializeExpandedEditorLayoutIfNeeded();
            HandleExpandedEditorLayout();
            HandleWindowDrag();
            HandleBlankAreaDragScroll();
            DrawPanelChrome(updateNoticeText);

            GUILayout.BeginArea(GetContentRect());
            DrawWindow(selectionReadResult, statusText, updateNoticeText);
            GUILayout.EndArea();

            DrawExpandedEditor();
            InputCapture.SetTextInputFocused(GUIUtility.keyboardControl != 0);
        }

        private void InitializeLayoutIfNeeded()
        {
            float width;
            float height;

            if (_layoutInitialized || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            width = Mathf.Clamp(_settings.WindowWidth, 720f, Mathf.Max(720f, Screen.width - 40f));
            height = Mathf.Clamp(_settings.WindowHeight, 480f, Mathf.Max(480f, Screen.height - 40f));
            _windowRect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            _layoutInitialized = true;
        }

        private void InitializeExpandedEditorLayoutIfNeeded()
        {
            if (_expandedEditorLayoutInitialized || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            _expandedEditorRect = new Rect(
                Mathf.Clamp(_settings.ExpandedEditorX, 0f, Mathf.Max(0f, Screen.width - 360f)),
                Mathf.Clamp(_settings.ExpandedEditorY, 0f, Mathf.Max(0f, Screen.height - 220f)),
                Mathf.Clamp(_settings.ExpandedEditorWidth, 360f, Mathf.Max(360f, Screen.width - 40f)),
                Mathf.Clamp(_settings.ExpandedEditorHeight, 220f, Mathf.Max(220f, Screen.height - 40f)));
            ClampExpandedEditorToScreen();
            _expandedEditorLayoutInitialized = true;
        }

        private bool IsMouseOverExpandedEditor(Vector2 mousePosition)
        {
            return _expandedEditorMember != null && _expandedEditorRect.Contains(mousePosition);
        }

        private Rect GetContentRect()
        {
            return new Rect(_windowRect.x + 18f, _windowRect.y + 52f, _windowRect.width - 36f, _windowRect.height - 70f);
        }

        private void DrawWindow(SelectionReadResult selectionReadResult, string statusText, string updateNoticeText)
        {
            GUILayout.BeginVertical();
            DrawToolbar(selectionReadResult);
            GUILayout.Space(10f);

            if (_showSettings)
            {
                DrawSettingsPage();
            }
            else
            {
                DrawStatus(selectionReadResult, statusText, updateNoticeText);
                GUILayout.Space(8f);
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                DrawSnapshot(selectionReadResult == null ? null : selectionReadResult.Snapshot);
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private void DrawToolbar(SelectionReadResult selectionReadResult)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_localization.Get("button.refresh"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(34f)) && RefreshRequested != null)
            {
                RefreshRequested();
            }

            if (GUILayout.Button(_localization.Get("button.settings"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(34f)))
            {
                _showSettings = !_showSettings;
                LoadSettingsText();
            }

            if (GUILayout.Button(_localization.Get("button.language") + ": " + _localization.Language, GetButtonStyle(), GUILayout.Width(190f), GUILayout.Height(34f)) && LanguageToggleRequested != null)
            {
                LanguageToggleRequested();
            }

            GUI.enabled = selectionReadResult != null && selectionReadResult.Snapshot != null && !string.IsNullOrEmpty(selectionReadResult.Snapshot.XmlText);
            if (GUILayout.Button(_localization.Get("button.copyXml"), GetButtonStyle(), GUILayout.Width(125f), GUILayout.Height(34f)) && CopyXmlRequested != null)
            {
                CopyXmlRequested();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawSettingsPage()
        {
            GUILayout.BeginVertical(GetSectionStyle());
            GUILayout.Label(_localization.Get("settings.title"), GetTitleStyle());
            GUILayout.Space(8f);
            DrawSettingsRow("settings.fontSize", ref _fontSizeText);
            DrawSettingsRow("settings.windowWidth", ref _windowWidthText);
            DrawSettingsRow("settings.windowHeight", ref _windowHeightText);
            DrawSettingsRow("settings.backgroundOpacity", ref _backgroundOpacityText);
            DrawSettingsRow("settings.hotkey", ref _hotkeyText);
            _updateCheckEnabled = GUILayout.Toggle(_updateCheckEnabled, _localization.Get("settings.updateCheckEnabled"));
            DrawSettingsRow("settings.updateIndexUrl", ref _updateIndexUrlText, -1f);
            DrawSettingsRow("settings.selectionRefreshIntervalSeconds", ref _selectionRefreshIntervalText);
            DrawSettingsRow("settings.floatingButtonSize", ref _floatingButtonSizeText);
            _lockFloatingButtonPosition = GUILayout.Toggle(_lockFloatingButtonPosition, _localization.Get("settings.lockFloatingButtonPosition"));
            _showTypeColumn = GUILayout.Toggle(_showTypeColumn, _localization.Get("settings.showTypeColumn"));
            _showAccessColumn = GUILayout.Toggle(_showAccessColumn, _localization.Get("settings.showAccessColumn"));
            _showFullTypeName = GUILayout.Toggle(_showFullTypeName, _localization.Get("settings.showFullTypeName"));
            _showRuntimeCacheMembers = GUILayout.Toggle(_showRuntimeCacheMembers, _localization.Get("settings.showRuntimeCacheMembers"));
            GUILayout.Space(8f);
            GUILayout.Label(_localization.Get("settings.hint"), GetMutedLabelStyle());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_localization.Get("button.saveSettings"), GetButtonStyle(), GUILayout.Width(150f), GUILayout.Height(34f)))
            {
                ApplySettingsPage();
            }

            if (GUILayout.Button(_localization.Get("button.resetSettings"), GetButtonStyle(), GUILayout.Width(150f), GUILayout.Height(34f)))
            {
                _settings.SetUiLayout(18, 1320f, 860f, 0.96f);
                _settings.SetToggleWindowHotkey("F8");
                _settings.SetUpdateCheckOptions(true, string.Empty);
                _settings.SetSelectionRefreshInterval(0.25f);
                _settings.SetFloatingButtonSize(52f);
                _settings.SetExpandedEditorLayout(120f, 120f, 820f, 420f);
                _settings.SetLockFloatingButtonPosition(false);
                _settings.SetDisplayOptions(true, true, true);
                _settings.SetRuntimeCacheMembersVisible(false);
                LoadSettingsText();
                ApplyWindowSizeFromSettings();
                SaveSettings();
                RaiseStatus(_localization.Get("status.settingsSaved"));
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawSettingsRow(string labelKey, ref string value)
        {
            DrawSettingsRow(labelKey, ref value, 180f);
        }

        private void DrawSettingsRow(string labelKey, ref string value, float valueWidth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_localization.Get(labelKey), GetLabelStyle(), GUILayout.Width(330f));
            value = DrawTextField("settings-" + labelKey, value, valueWidth);
            GUILayout.EndHorizontal();
        }

        private void ApplySettingsPage()
        {
            int fontSize;
            float windowWidth;
            float windowHeight;
            float backgroundOpacity;
            float selectionRefreshIntervalSeconds;
            float floatingButtonSize;

            if (!int.TryParse(_fontSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out fontSize) ||
                !float.TryParse(_windowWidthText, NumberStyles.Float, CultureInfo.InvariantCulture, out windowWidth) ||
                !float.TryParse(_windowHeightText, NumberStyles.Float, CultureInfo.InvariantCulture, out windowHeight) ||
                !float.TryParse(_backgroundOpacityText, NumberStyles.Float, CultureInfo.InvariantCulture, out backgroundOpacity) ||
                !float.TryParse(_selectionRefreshIntervalText, NumberStyles.Float, CultureInfo.InvariantCulture, out selectionRefreshIntervalSeconds) ||
                !float.TryParse(_floatingButtonSizeText, NumberStyles.Float, CultureInfo.InvariantCulture, out floatingButtonSize))
            {
                RaiseStatus(_localization.Get("status.settingsInvalid"));
                return;
            }

            _settings.SetUiLayout(fontSize, windowWidth, windowHeight, backgroundOpacity);
            _settings.SetToggleWindowHotkey(_hotkeyText);
            _settings.SetUpdateCheckOptions(_updateCheckEnabled, _updateIndexUrlText);
            _settings.SetSelectionRefreshInterval(selectionRefreshIntervalSeconds);
            _settings.SetFloatingButtonSize(floatingButtonSize);
            _settings.SetLockFloatingButtonPosition(_lockFloatingButtonPosition);
            _settings.SetDisplayOptions(_showTypeColumn, _showAccessColumn, _showFullTypeName);
            _settings.SetRuntimeCacheMembersVisible(_showRuntimeCacheMembers);
            LoadSettingsText();
            ApplyWindowSizeFromSettings();
            SaveSettings();
            RaiseStatus(_localization.Get("status.settingsSaved"));
        }

        private void LoadSettingsText()
        {
            _fontSizeText = _settings.FontSize.ToString(CultureInfo.InvariantCulture);
            _windowWidthText = _settings.WindowWidth.ToString("0", CultureInfo.InvariantCulture);
            _windowHeightText = _settings.WindowHeight.ToString("0", CultureInfo.InvariantCulture);
            _backgroundOpacityText = _settings.BackgroundOpacity.ToString("0.##", CultureInfo.InvariantCulture);
            _hotkeyText = _settings.ToggleWindowHotkey;
            _updateCheckEnabled = _settings.UpdateCheckEnabled;
            _updateIndexUrlText = _settings.UpdateIndexUrl;
            _selectionRefreshIntervalText = _settings.SelectionRefreshIntervalSeconds.ToString("0.##", CultureInfo.InvariantCulture);
            _floatingButtonSizeText = _settings.FloatingButtonSize.ToString("0", CultureInfo.InvariantCulture);
            _lockFloatingButtonPosition = _settings.LockFloatingButtonPosition;
            _showTypeColumn = _settings.ShowTypeColumn;
            _showAccessColumn = _settings.ShowAccessColumn;
            _showFullTypeName = _settings.ShowFullTypeName;
            _showRuntimeCacheMembers = _settings.ShowRuntimeCacheMembers;
        }

        private void ApplyWindowSizeFromSettings()
        {
            _windowRect.width = Mathf.Clamp(_settings.WindowWidth, 720f, Mathf.Max(720f, Screen.width - 40f));
            _windowRect.height = Mathf.Clamp(_settings.WindowHeight, 480f, Mathf.Max(480f, Screen.height - 40f));
            ClampWindowToScreen();
        }

        private void SaveSettings()
        {
            if (SettingsSaveRequested != null)
            {
                SettingsSaveRequested();
            }
        }

        private void DrawStatus(SelectionReadResult selectionReadResult, string statusText, string updateNoticeText)
        {
            string message = statusText;
            if (selectionReadResult != null && selectionReadResult.Snapshot == null)
            {
                message = _localization.Get(selectionReadResult.StatusKey);
            }

            GUILayout.Label(_localization.Get("label.status") + ": " + message, GetMutedLabelStyle());
            if (!string.IsNullOrEmpty(updateNoticeText))
            {
                GUILayout.Space(4f);
                GUILayout.BeginVertical(GetSectionStyle());
                GUILayout.Label(updateNoticeText, GetLabelStyle());
                GUILayout.EndVertical();
            }
        }

        private void DrawSnapshot(SelectedPartSnapshot snapshot)
        {
            if (snapshot == null)
            {
                CloseExpandedEditor();
                return;
            }

            CloseExpandedEditorIfStale(snapshot);
            DrawPartInfo(snapshot);
            GUILayout.Space(10f);

            if (snapshot.Groups.Count == 0)
            {
                return;
            }

            if (_selectedGroupIndex < 0 || _selectedGroupIndex >= snapshot.Groups.Count)
            {
                _selectedGroupIndex = 0;
            }

            DrawGroupChooserAndCustomXml(snapshot);
            DrawGroup(snapshot, snapshot.Groups[_selectedGroupIndex]);
        }

        private void CloseExpandedEditorIfStale(SelectedPartSnapshot snapshot)
        {
            if (_expandedEditorMember == null || snapshot == null)
            {
                return;
            }

            foreach (InspectableGroup group in snapshot.Groups)
            {
                if (group.Members.Contains(_expandedEditorMember))
                {
                    _expandedEditorGroup = group;
                    _expandedEditorSnapshot = snapshot;
                    return;
                }
            }

            CloseExpandedEditor();
        }

        private void DrawPartInfo(SelectedPartSnapshot snapshot)
        {
            GUILayout.BeginVertical(GetSectionStyle());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button((_partInfoExpanded ? "▼ " : "▶ ") + _localization.Get("section.partInfo"), GetButtonStyle(), GUILayout.Width(190f), GUILayout.Height(30f)))
            {
                _partInfoExpanded = !_partInfoExpanded;
            }

            GUILayout.Label(snapshot.PartName + "  #" + snapshot.PartId, GetMutedLabelStyle());
            GUILayout.EndHorizontal();

            if (_partInfoExpanded)
            {
                GUILayout.Space(6f);
                GUILayout.Label(_localization.Get("label.part") + ": " + snapshot.PartName, GetLabelStyle());
                GUILayout.Label(_localization.Get("label.partId") + ": " + snapshot.PartId, GetLabelStyle());
                GUILayout.Label(_localization.Get("label.partType") + ": " + snapshot.PartTypeName, GetLabelStyle());
                GUILayout.Label(_localization.Get("label.partDataType") + ": " + snapshot.PartDataTypeName, GetMutedLabelStyle());
                GUILayout.Space(8f);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(_localization.Get("label.search"), GetLabelStyle(), GUILayout.Width(80f));
            _searchTerm = DrawTextField("search", _searchTerm ?? string.Empty, -1f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawGroupChooserAndCustomXml(SelectedPartSnapshot snapshot)
        {
            GUILayout.BeginHorizontal();
            DrawGroupChooser(snapshot);
            DrawCustomXmlAttributes(snapshot.Groups[_selectedGroupIndex]);
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
        }

        private void DrawGroupChooser(SelectedPartSnapshot snapshot)
        {
            GUILayout.BeginVertical(GetSectionStyle(), GUILayout.Width(330f));
            GUILayout.Label(_localization.Get("groupChooser.title"), GetTitleStyle());

            for (int index = 0; index < snapshot.Groups.Count; index++)
            {
                InspectableGroup group = snapshot.Groups[index];
                bool selected = index == _selectedGroupIndex;
                string label = selected ? "● " + group.Title : "○ " + group.Title;
                if (GUILayout.Button(label, GetButtonStyle(), GUILayout.Height(30f)))
                {
                    _selectedGroupIndex = index;
                    _scrollPosition.y = 0f;
                    _customXmlAttributeName = string.Empty;
                    _customXmlAttributeValue = string.Empty;
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawCustomXmlAttributes(InspectableGroup group)
        {
            List<CustomXmlAttribute> attributes = CustomXmlAttributeStore.GetAttributes(group.TargetObject);

            GUILayout.BeginVertical(GetSectionStyle(), GUILayout.ExpandWidth(true));
            GUILayout.Label(_localization.Get("customXml.title") + ": " + group.Title, GetTitleStyle());

            GUILayout.BeginHorizontal();
            GUILayout.Label(_localization.Get("customXml.name"), GetLabelStyle(), GUILayout.Width(120f));
            _customXmlAttributeName = DrawTextField("custom-xml-name", _customXmlAttributeName, 220f);
            GUILayout.Label(_localization.Get("customXml.value"), GetLabelStyle(), GUILayout.Width(80f));
            _customXmlAttributeValue = DrawTextField("custom-xml-value", _customXmlAttributeValue, 320f);
            if (GUILayout.Button(_localization.Get("customXml.add"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(30f)))
            {
                string error;
                if (CustomXmlAttributeStore.SetAttribute(group.TargetObject, _customXmlAttributeName, _customXmlAttributeValue, out error))
                {
                    _customXmlAttributeName = string.Empty;
                    _customXmlAttributeValue = string.Empty;
                    RaiseStatus(_localization.Get("customXml.added"));
                    if (RefreshRequested != null)
                    {
                        RefreshRequested();
                    }
                }
                else
                {
                    RaiseStatus(_localization.Get("customXml.failed") + ": " + error);
                }
            }
            GUILayout.EndHorizontal();

            if (attributes.Count == 0)
            {
                GUILayout.Label(_localization.Get("customXml.empty"), GetMutedLabelStyle());
            }
            else
            {
                foreach (CustomXmlAttribute attribute in attributes)
                {
                    GUILayout.BeginHorizontal(_alternateRow ? GetRowAltStyle() : GetRowStyle(), GUILayout.MinHeight(30f));
                    GUILayout.Label(attribute.Name, GetLabelStyle(), GUILayout.Width(220f));
                    GUILayout.Label(attribute.Value, GetReadOnlyValueStyle(), GUILayout.Width(520f));
                    if (GUILayout.Button(_localization.Get("customXml.remove"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(28f)))
                    {
                        CustomXmlAttributeStore.RemoveAttribute(group.TargetObject, attribute.Name);
                        RaiseStatus(_localization.Get("customXml.removed"));
                        if (RefreshRequested != null)
                        {
                            RefreshRequested();
                        }
                    }
                    GUILayout.EndHorizontal();
                    _alternateRow = !_alternateRow;
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawGroup(SelectedPartSnapshot snapshot, InspectableGroup group)
        {
            int visibleCount = 0;

            foreach (InspectableMember member in group.Members)
            {
                if (member.Matches(_searchTerm))
                {
                    visibleCount++;
                }
            }

            if (visibleCount == 0)
            {
                return;
            }

            GUILayout.BeginVertical(GetSectionStyle());
            GUILayout.Label(group.Title + "  " + _localization.Get("label.memberCount") + ": " + visibleCount, GetTitleStyle());
            if (_settings.ShowFullTypeName)
            {
                GUILayout.Label(group.TargetTypeName, GetMutedLabelStyle());
            }
            DrawHeaderRow();

            _alternateRow = false;
            foreach (InspectableMember member in group.Members)
            {
                if (!member.Matches(_searchTerm))
                {
                    continue;
                }

                DrawMemberRow(snapshot, group, member);
            }

            GUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        private void DrawHeaderRow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_localization.Get("column.name"), GetColumnHeaderStyle(), GUILayout.Width(GetNameColumnWidth()));
            if (_settings.ShowTypeColumn)
            {
                GUILayout.Label(_localization.Get("column.type"), GetColumnHeaderStyle(), GUILayout.Width(GetTypeColumnWidth()));
            }
            GUILayout.Label(_localization.Get("column.value"), GetColumnHeaderStyle(), GUILayout.Width(GetValueColumnWidth()));
            if (_settings.ShowAccessColumn)
            {
                GUILayout.Label(_localization.Get("column.access"), GetColumnHeaderStyle(), GUILayout.Width(GetAccessColumnWidth()));
            }
            GUILayout.Label(_localization.Get("column.actions"), GetColumnHeaderStyle(), GUILayout.Width(GetActionsColumnWidth()));
            GUILayout.EndHorizontal();
        }

        private void DrawMemberRow(SelectedPartSnapshot snapshot, InspectableGroup group, InspectableMember member)
        {
            GUILayout.BeginHorizontal(_alternateRow ? GetRowAltStyle() : GetRowStyle(), GUILayout.MinHeight(34f));
            GUILayout.Label(member.Name, GetLabelStyle(), GUILayout.Width(GetNameColumnWidth()));
            if (_settings.ShowTypeColumn)
            {
                GUILayout.Label(member.TypeName, GetMutedLabelStyle(), GUILayout.Width(GetTypeColumnWidth()));
            }
            if (member.CanWrite)
            {
                member.EditorValue = DrawTextField("member-" + member.GetHashCode().ToString(CultureInfo.InvariantCulture), member.EditorValue ?? string.Empty, GetValueColumnWidth());
            }
            else
            {
                GUILayout.Label(member.EditorValue ?? string.Empty, GetReadOnlyValueStyle(), GUILayout.Width(GetValueColumnWidth()));
            }
            if (_settings.ShowAccessColumn)
            {
                GUILayout.Label(member.Access, GetMutedLabelStyle(), GUILayout.Width(GetAccessColumnWidth()));
            }

            GUI.enabled = member.CanWrite;
            if (GUILayout.Button(_localization.Get("button.expandEditor"), GetButtonStyle(), GUILayout.Width(70f), GUILayout.Height(30f)))
            {
                OpenExpandedEditor(snapshot, group, member);
            }

            GUI.enabled = member.CanWrite && member.IsDirty;
            if (GUILayout.Button(_localization.Get("button.apply"), GetButtonStyle(), GUILayout.Width(70f), GUILayout.Height(30f)))
            {
                ApplyMemberValue(member);
            }

            GUI.enabled = member.CanWrite && member.IsDirty;
            if (GUILayout.Button(_localization.Get("button.reset"), GetButtonStyle(), GUILayout.Width(70f), GUILayout.Height(30f)))
            {
                member.ResetEditorValue();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            _alternateRow = !_alternateRow;

            if (!string.IsNullOrEmpty(member.Attributes))
            {
                GUILayout.Label("  [" + member.Attributes + "]", GetMutedLabelStyle());
            }

            if (!string.IsNullOrEmpty(member.Error))
            {
                GUILayout.Label("  " + _localization.Get("label.error") + ": " + member.Error, GetMutedLabelStyle());
            }
        }

        private float GetNameColumnWidth()
        {
            return 250f;
        }

        private float GetTypeColumnWidth()
        {
            return 130f;
        }

        private float GetAccessColumnWidth()
        {
            return 170f;
        }

        private float GetActionsColumnWidth()
        {
            return 220f;
        }

        private float GetValueColumnWidth()
        {
            float width = 470f;
            if (!_settings.ShowTypeColumn)
            {
                width += GetTypeColumnWidth();
            }

            if (!_settings.ShowAccessColumn)
            {
                width += GetAccessColumnWidth();
            }

            return width;
        }

        private void OpenExpandedEditor(SelectedPartSnapshot snapshot, InspectableGroup group, InspectableMember member)
        {
            if (member == null || !member.CanWrite)
            {
                return;
            }

            _expandedEditorSnapshot = snapshot;
            _expandedEditorGroup = group;
            _expandedEditorMember = member;
            _expandedEditorScrollPosition = Vector2.zero;
            InitializeExpandedEditorLayoutIfNeeded();
        }

        private void CloseExpandedEditor()
        {
            _expandedEditorMember = null;
            _expandedEditorGroup = null;
            _expandedEditorSnapshot = null;
        }

        private void DrawExpandedEditor()
        {
            Rect contentRect;

            if (_expandedEditorMember == null)
            {
                return;
            }

            DrawExpandedEditorChrome();
            contentRect = new Rect(_expandedEditorRect.x + 14f, _expandedEditorRect.y + 42f, _expandedEditorRect.width - 28f, _expandedEditorRect.height - 58f);

            GUILayout.BeginArea(contentRect);
            GUILayout.BeginVertical();
            GUILayout.Label(_expandedEditorMember.Name, GetTitleStyle());
            GUILayout.Space(6f);
            _expandedEditorScrollPosition = GUILayout.BeginScrollView(_expandedEditorScrollPosition, GUILayout.ExpandHeight(true));
            _expandedEditorMember.EditorValue = DrawTextArea("expanded-editor-" + _expandedEditorMember.GetHashCode().ToString(CultureInfo.InvariantCulture), _expandedEditorMember.EditorValue ?? string.Empty);
            GUILayout.EndScrollView();
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            GUI.enabled = _expandedEditorMember.CanWrite && _expandedEditorMember.IsDirty;
            if (GUILayout.Button(_localization.Get("button.apply"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(32f)))
            {
                ApplyMemberValue(_expandedEditorMember);
            }

            if (GUILayout.Button(_localization.Get("button.reset"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(32f)))
            {
                _expandedEditorMember.ResetEditorValue();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_localization.Get("button.close"), GetButtonStyle(), GUILayout.Width(110f), GUILayout.Height(32f)))
            {
                CloseExpandedEditor();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void ApplyMemberValue(InspectableMember member)
        {
            if (member.TryApply())
            {
                if (MemberApplied != null)
                {
                    MemberApplied(member);
                }

                RaiseStatus(_localization.Get("status.applied") + ": " + member.Name);
            }
            else
            {
                RaiseStatus(_localization.Get("status.applyFailed") + ": " + member.Name);
            }
        }

        private void DrawExpandedEditorChrome()
        {
            Rect shadowRect = new Rect(_expandedEditorRect.x + 8f, _expandedEditorRect.y + 8f, _expandedEditorRect.width, _expandedEditorRect.height);
            Rect resizeHandleRect = GetExpandedEditorResizeHandleRect();

            DrawRect(shadowRect, new Color(0f, 0f, 0f, 0.32f));
            DrawRect(_expandedEditorRect, new Color(0.035f, 0.045f, 0.06f, 0.98f));
            DrawRect(new Rect(_expandedEditorRect.x, _expandedEditorRect.y, _expandedEditorRect.width, 34f), new Color(0.02f, 0.08f, 0.18f, 1f));
            DrawBorder(_expandedEditorRect, new Color(0.2f, 0.48f, 0.86f, 1f));
            DrawBorder(new Rect(_expandedEditorRect.x + 1f, _expandedEditorRect.y + 1f, _expandedEditorRect.width - 2f, _expandedEditorRect.height - 2f), new Color(0.08f, 0.2f, 0.38f, 1f));
            DrawRect(resizeHandleRect, new Color(0.1f, 0.36f, 0.8f, 0.85f));

            GUI.Label(new Rect(_expandedEditorRect.x + 16f, _expandedEditorRect.y + 5f, _expandedEditorRect.width - 32f, 24f), _localization.Get("expandedEditor.title"), GetHeaderLabelStyle());
        }

        private void HandleExpandedEditorLayout()
        {
            Event currentEvent = Event.current;
            Rect resizeHandleRect;

            if (currentEvent == null || _expandedEditorMember == null)
            {
                return;
            }

            resizeHandleRect = GetExpandedEditorResizeHandleRect();
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                if (resizeHandleRect.Contains(currentEvent.mousePosition))
                {
                    _resizingExpandedEditor = true;
                    _expandedEditorLayoutChanged = false;
                    currentEvent.Use();
                    return;
                }

                if (IsExpandedEditorBorder(currentEvent.mousePosition))
                {
                    _draggingExpandedEditor = true;
                    _expandedEditorLayoutChanged = false;
                    _expandedEditorDragOffset = currentEvent.mousePosition - new Vector2(_expandedEditorRect.x, _expandedEditorRect.y);
                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseDrag && _draggingExpandedEditor)
            {
                _expandedEditorRect.x = currentEvent.mousePosition.x - _expandedEditorDragOffset.x;
                _expandedEditorRect.y = currentEvent.mousePosition.y - _expandedEditorDragOffset.y;
                ClampExpandedEditorToScreen();
                _expandedEditorLayoutChanged = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && _resizingExpandedEditor)
            {
                _expandedEditorRect.width = Mathf.Max(360f, currentEvent.mousePosition.x - _expandedEditorRect.x);
                _expandedEditorRect.height = Mathf.Max(220f, currentEvent.mousePosition.y - _expandedEditorRect.y);
                ClampExpandedEditorToScreen();
                _expandedEditorLayoutChanged = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                if ((_draggingExpandedEditor || _resizingExpandedEditor) && _expandedEditorLayoutChanged)
                {
                    SaveExpandedEditorLayout();
                }

                _draggingExpandedEditor = false;
                _resizingExpandedEditor = false;
                _expandedEditorLayoutChanged = false;
            }
        }

        private bool IsExpandedEditorBorder(Vector2 mousePosition)
        {
            const float borderWidth = 12f;
            Rect innerRect;

            if (!_expandedEditorRect.Contains(mousePosition))
            {
                return false;
            }

            innerRect = new Rect(
                _expandedEditorRect.x + borderWidth,
                _expandedEditorRect.y + borderWidth,
                _expandedEditorRect.width - borderWidth * 2f,
                _expandedEditorRect.height - borderWidth * 2f);
            return !innerRect.Contains(mousePosition);
        }

        private Rect GetExpandedEditorResizeHandleRect()
        {
            const float handleSize = 22f;
            return new Rect(_expandedEditorRect.xMax - handleSize - 5f, _expandedEditorRect.yMax - handleSize - 5f, handleSize, handleSize);
        }

        private void ClampExpandedEditorToScreen()
        {
            _expandedEditorRect.width = Mathf.Clamp(_expandedEditorRect.width, 360f, Mathf.Max(360f, Screen.width - 20f));
            _expandedEditorRect.height = Mathf.Clamp(_expandedEditorRect.height, 220f, Mathf.Max(220f, Screen.height - 20f));
            _expandedEditorRect.x = Mathf.Clamp(_expandedEditorRect.x, 0f, Mathf.Max(0f, Screen.width - _expandedEditorRect.width));
            _expandedEditorRect.y = Mathf.Clamp(_expandedEditorRect.y, 0f, Mathf.Max(0f, Screen.height - _expandedEditorRect.height));
        }

        private void SaveExpandedEditorLayout()
        {
            _settings.SetExpandedEditorLayout(_expandedEditorRect.x, _expandedEditorRect.y, _expandedEditorRect.width, _expandedEditorRect.height);
            SaveSettings();
        }

        private void ApplySkin()
        {
            int fontSize = _settings.FontSize;
            GUI.skin.settings.cursorColor = new Color(0.1f, 0.36f, 0.95f, 1f);
            GUI.skin.settings.selectionColor = new Color(0.1f, 0.36f, 0.95f, 0.35f);
            GUI.skin.label.fontSize = fontSize;
            GUI.skin.button.fontSize = fontSize;
            GUI.skin.textField.fontSize = fontSize;
            GUI.skin.textArea.fontSize = fontSize;
            GUI.skin.box.fontSize = fontSize;
            GUI.skin.window.fontSize = fontSize + 1;
            GUI.skin.toggle.fontSize = fontSize;
        }

        private string DrawTextField(string controlName, string value, float width)
        {
            string before = value ?? string.Empty;
            string after;

            GUI.SetNextControlName(controlName);
            if (width > 0f)
            {
                after = GUILayout.TextField(before, GetTextFieldStyle(), GUILayout.Width(width), GUILayout.Height(30f));
            }
            else
            {
                after = GUILayout.TextField(before, GetTextFieldStyle(), GUILayout.Height(30f));
            }

            DrawTextFieldBorder(GUILayoutUtility.GetLastRect(), GUI.GetNameOfFocusedControl() == controlName);

            if (GUI.GetNameOfFocusedControl() == controlName && Event.current != null && Event.current.type == EventType.KeyDown && after == before)
            {
                string injected = TryInjectKey(before, Event.current);
                if (!string.Equals(injected, before, StringComparison.Ordinal))
                {
                    after = injected;
                    Event.current.Use();
                }
            }

            return after;
        }

        private string DrawTextArea(string controlName, string value)
        {
            string before = value ?? string.Empty;
            string after;

            GUI.SetNextControlName(controlName);
            after = GUILayout.TextArea(before, GetTextAreaStyle(), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinHeight(180f));
            DrawTextFieldBorder(GUILayoutUtility.GetLastRect(), GUI.GetNameOfFocusedControl() == controlName);
            return after;
        }

        private static string TryInjectKey(string value, Event keyEvent)
        {
            char character = keyEvent.character;
            string text = value ?? string.Empty;

            if ((keyEvent.control || keyEvent.command) && keyEvent.keyCode == KeyCode.V)
            {
                return text + (GUIUtility.systemCopyBuffer ?? string.Empty);
            }

            if (keyEvent.keyCode == KeyCode.Backspace && text.Length > 0)
            {
                return text.Substring(0, text.Length - 1);
            }

            if (keyEvent.keyCode == KeyCode.Delete && text.Length > 0)
            {
                return string.Empty;
            }

            if (character >= 32 && character != 127)
            {
                return text + character;
            }

            character = KeyCodeToCharacter(keyEvent.keyCode, keyEvent.shift);
            if (character >= 32 && character != 127)
            {
                return text + character;
            }

            return text;
        }

        private static char KeyCodeToCharacter(KeyCode keyCode, bool shift)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                char value = (char)('a' + ((int)keyCode - (int)KeyCode.A));
                return shift ? char.ToUpperInvariant(value) : value;
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                string normal = "0123456789";
                string shifted = ")!@#$%^&*(";
                int index = (int)keyCode - (int)KeyCode.Alpha0;
                return shift ? shifted[index] : normal[index];
            }

            if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
            {
                return (char)('0' + ((int)keyCode - (int)KeyCode.Keypad0));
            }

            switch (keyCode)
            {
                case KeyCode.Space:
                    return ' ';
                case KeyCode.Minus:
                    return shift ? '_' : '-';
                case KeyCode.Equals:
                    return shift ? '+' : '=';
                case KeyCode.Period:
                    return shift ? '>' : '.';
                case KeyCode.Comma:
                    return shift ? '<' : ',';
                case KeyCode.Slash:
                    return shift ? '?' : '/';
                case KeyCode.Backslash:
                    return shift ? '|' : '\\';
                case KeyCode.Semicolon:
                    return shift ? ':' : ';';
                case KeyCode.Quote:
                    return shift ? '"' : '\'';
                case KeyCode.LeftBracket:
                    return shift ? '{' : '[';
                case KeyCode.RightBracket:
                    return shift ? '}' : ']';
                case KeyCode.BackQuote:
                    return shift ? '~' : '`';
                case KeyCode.KeypadPeriod:
                    return '.';
                case KeyCode.KeypadDivide:
                    return '/';
                case KeyCode.KeypadMultiply:
                    return '*';
                case KeyCode.KeypadMinus:
                    return '-';
                case KeyCode.KeypadPlus:
                    return '+';
                default:
                    return '\0';
            }
        }

        private void HandleWindowDrag()
        {
            Event currentEvent = Event.current;
            Rect headerRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, 38f);

            if (currentEvent == null)
            {
                return;
            }

            if (!_draggingWindow && IsMouseOverExpandedEditor(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && headerRect.Contains(currentEvent.mousePosition))
            {
                _draggingWindow = true;
                _dragOffset = currentEvent.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && _draggingWindow)
            {
                _windowRect.x = currentEvent.mousePosition.x - _dragOffset.x;
                _windowRect.y = currentEvent.mousePosition.y - _dragOffset.y;
                ClampWindowToScreen();
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                _draggingWindow = false;
            }
        }

        private void HandleBlankAreaDragScroll()
        {
            Event currentEvent = Event.current;
            Rect contentRect = GetContentRect();

            if (currentEvent == null || _showSettings || GUIUtility.keyboardControl != 0 || GUIUtility.hotControl != 0 ||
                IsMouseOverExpandedEditor(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && contentRect.Contains(currentEvent.mousePosition))
            {
                _scrollPosition.y = Mathf.Max(0f, _scrollPosition.y - currentEvent.delta.y);
                currentEvent.Use();
            }
        }

        private void ClampWindowToScreen()
        {
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
        }

        private void DrawPanelChrome(string updateNoticeText)
        {
            Rect shadowRect = new Rect(_windowRect.x + 8f, _windowRect.y + 8f, _windowRect.width, _windowRect.height);
            string title = _localization.Get("window.title");

            if (!string.IsNullOrEmpty(updateNoticeText))
            {
                title += "  [" + _localization.Get("update.titleBadge") + "]";
            }

            DrawRect(shadowRect, new Color(0f, 0f, 0f, 0.28f));
            DrawRect(_windowRect, new Color(0.035f, 0.045f, 0.06f, _settings.BackgroundOpacity));
            DrawRect(new Rect(_windowRect.x, _windowRect.y, _windowRect.width, 42f), new Color(0.02f, 0.08f, 0.18f, 1f));
            DrawRect(new Rect(_windowRect.x, _windowRect.y + 42f, _windowRect.width, 4f), new Color(0.08f, 0.42f, 1f, 1f));
            DrawBorder(_windowRect, new Color(0.2f, 0.38f, 0.7f, 1f));

            GUI.Label(new Rect(_windowRect.x + 20f, _windowRect.y + 7f, _windowRect.width - 300f, 28f), title, GetHeaderLabelStyle());
            GUI.Label(new Rect(_windowRect.x + _windowRect.width - 260f, _windowRect.y + 8f, 240f, 24f), _localization.Get("label.hotkey"), GetHeaderLabelRightStyle());
        }

        private static void DrawBorder(Rect rect, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private GUIStyle GetTitleStyle()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label);
                _titleStyle.normal.textColor = Color.white;
                _titleStyle.fontStyle = FontStyle.Bold;
            }

            _titleStyle.fontSize = _settings.FontSize + 1;
            return _titleStyle;
        }

        private GUIStyle GetLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.normal.textColor = new Color(0.94f, 0.97f, 1f, 1f);
            }

            _labelStyle.fontSize = _settings.FontSize;
            return _labelStyle;
        }

        private GUIStyle GetMutedLabelStyle()
        {
            if (_mutedLabelStyle == null)
            {
                _mutedLabelStyle = new GUIStyle(GUI.skin.label);
                _mutedLabelStyle.normal.textColor = new Color(0.66f, 0.76f, 0.9f, 1f);
            }

            _mutedLabelStyle.fontSize = _settings.FontSize;
            return _mutedLabelStyle;
        }

        private GUIStyle GetHeaderLabelStyle()
        {
            if (_headerLabelStyle == null)
            {
                _headerLabelStyle = new GUIStyle(GUI.skin.label);
                _headerLabelStyle.alignment = TextAnchor.MiddleLeft;
                _headerLabelStyle.normal.textColor = Color.white;
                _headerLabelStyle.fontStyle = FontStyle.Bold;
            }

            _headerLabelStyle.fontSize = _settings.FontSize + 1;
            return _headerLabelStyle;
        }

        private GUIStyle GetHeaderLabelRightStyle()
        {
            if (_headerLabelRightStyle == null)
            {
                _headerLabelRightStyle = new GUIStyle(GUI.skin.label);
                _headerLabelRightStyle.alignment = TextAnchor.MiddleRight;
                _headerLabelRightStyle.normal.textColor = Color.white;
                _headerLabelRightStyle.fontStyle = FontStyle.Bold;
                _headerLabelRightStyle.wordWrap = false;
                _headerLabelRightStyle.clipping = TextClipping.Clip;
            }

            _headerLabelRightStyle.fontSize = Mathf.Clamp(_settings.FontSize, 12, 18);
            return _headerLabelRightStyle;
        }

        private GUIStyle GetColumnHeaderStyle()
        {
            if (_columnHeaderStyle == null)
            {
                _columnHeaderStyle = new GUIStyle(GUI.skin.label);
                _columnHeaderStyle.normal.textColor = new Color(0.8f, 0.9f, 1f, 1f);
                _columnHeaderStyle.fontStyle = FontStyle.Bold;
            }

            _columnHeaderStyle.fontSize = _settings.FontSize;
            return _columnHeaderStyle;
        }

        private GUIStyle GetTextFieldStyle()
        {
            if (_textFieldStyle == null)
            {
                _textFieldStyle = new GUIStyle(GUI.skin.textField);
                _textFieldStyle.normal.background = GetTextFieldBackgroundTexture();
                _textFieldStyle.hover.background = GetTextFieldBackgroundTexture();
                _textFieldStyle.active.background = GetTextFieldBackgroundTexture();
                _textFieldStyle.focused.background = GetTextFieldBackgroundTexture();
                _textFieldStyle.normal.textColor = Color.white;
                _textFieldStyle.hover.textColor = Color.white;
                _textFieldStyle.active.textColor = Color.white;
                _textFieldStyle.focused.textColor = Color.white;
                _textFieldStyle.padding = new RectOffset(9, 9, 5, 5);
            }

            _textFieldStyle.fontSize = _settings.FontSize;
            return _textFieldStyle;
        }

        private GUIStyle GetTextAreaStyle()
        {
            if (_textAreaStyle == null)
            {
                _textAreaStyle = new GUIStyle(GUI.skin.textArea);
                _textAreaStyle.normal.background = GetTextFieldBackgroundTexture();
                _textAreaStyle.hover.background = GetTextFieldBackgroundTexture();
                _textAreaStyle.active.background = GetTextFieldBackgroundTexture();
                _textAreaStyle.focused.background = GetTextFieldBackgroundTexture();
                _textAreaStyle.normal.textColor = Color.white;
                _textAreaStyle.hover.textColor = Color.white;
                _textAreaStyle.active.textColor = Color.white;
                _textAreaStyle.focused.textColor = Color.white;
                _textAreaStyle.padding = new RectOffset(10, 10, 8, 8);
                _textAreaStyle.wordWrap = true;
            }

            _textAreaStyle.fontSize = _settings.FontSize;
            return _textAreaStyle;
        }

        private static void DrawTextFieldBorder(Rect rect, bool focused)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color border = focused ? new Color(0.15f, 0.55f, 1f, 1f) : new Color(0.28f, 0.42f, 0.62f, 1f);
            Color inner = focused ? new Color(0.35f, 0.7f, 1f, 0.75f) : new Color(0.18f, 0.28f, 0.44f, 1f);
            DrawBorder(rect, border);
            DrawBorder(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), inner);
        }

        private GUIStyle GetReadOnlyValueStyle()
        {
            if (_readOnlyValueStyle == null)
            {
                _readOnlyValueStyle = new GUIStyle(GUI.skin.label);
                _readOnlyValueStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 1f);
                _readOnlyValueStyle.padding = new RectOffset(8, 8, 5, 5);
            }

            _readOnlyValueStyle.fontSize = _settings.FontSize;
            return _readOnlyValueStyle;
        }

        private GUIStyle GetButtonStyle()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.normal.background = GetButtonBackgroundTexture();
                _buttonStyle.hover.background = GetButtonHoverTexture();
                _buttonStyle.active.background = GetButtonActiveTexture();
                _buttonStyle.focused.background = GetButtonBackgroundTexture();
                _buttonStyle.normal.textColor = Color.white;
                _buttonStyle.hover.textColor = Color.white;
                _buttonStyle.active.textColor = Color.white;
                _buttonStyle.focused.textColor = Color.white;
                _buttonStyle.fontStyle = FontStyle.Bold;
            }

            _buttonStyle.fontSize = _settings.FontSize;
            return _buttonStyle;
        }

        private GUIStyle GetRowStyle()
        {
            if (_rowStyle == null)
            {
                _rowStyle = new GUIStyle();
                _rowStyle.normal.background = GetRowBackgroundTexture();
                _rowStyle.padding = new RectOffset(8, 8, 3, 3);
            }

            return _rowStyle;
        }

        private GUIStyle GetRowAltStyle()
        {
            if (_rowAltStyle == null)
            {
                _rowAltStyle = new GUIStyle();
                _rowAltStyle.normal.background = GetRowAltBackgroundTexture();
                _rowAltStyle.padding = new RectOffset(8, 8, 3, 3);
            }

            return _rowAltStyle;
        }

        private GUIStyle GetSectionStyle()
        {
            if (_sectionStyle == null)
            {
                _sectionStyle = new GUIStyle(GUI.skin.box);
                _sectionStyle.normal.background = GetSectionBackgroundTexture();
                _sectionStyle.hover.background = GetSectionBackgroundTexture();
                _sectionStyle.active.background = GetSectionBackgroundTexture();
                _sectionStyle.focused.background = GetSectionBackgroundTexture();
                _sectionStyle.padding = new RectOffset(14, 14, 12, 12);
            }

            _sectionStyle.fontSize = _settings.FontSize;
            return _sectionStyle;
        }

        private Texture2D GetSectionBackgroundTexture()
        {
            if (_sectionBackgroundTexture == null)
            {
                _sectionBackgroundTexture = CreateSolidTexture(new Color(0.07f, 0.09f, 0.125f, 1f));
            }

            return _sectionBackgroundTexture;
        }

        private Texture2D GetTextFieldBackgroundTexture()
        {
            if (_textFieldBackgroundTexture == null)
            {
                _textFieldBackgroundTexture = CreateSolidTexture(new Color(0.015f, 0.025f, 0.04f, 1f));
            }

            return _textFieldBackgroundTexture;
        }

        private Texture2D GetButtonBackgroundTexture()
        {
            if (_buttonBackgroundTexture == null)
            {
                _buttonBackgroundTexture = CreateSolidTexture(new Color(0.1f, 0.36f, 0.8f, 1f));
            }

            return _buttonBackgroundTexture;
        }

        private Texture2D GetButtonHoverTexture()
        {
            if (_buttonHoverTexture == null)
            {
                _buttonHoverTexture = CreateSolidTexture(new Color(0.08f, 0.3f, 0.7f, 1f));
            }

            return _buttonHoverTexture;
        }

        private Texture2D GetButtonActiveTexture()
        {
            if (_buttonActiveTexture == null)
            {
                _buttonActiveTexture = CreateSolidTexture(new Color(0.06f, 0.22f, 0.56f, 1f));
            }

            return _buttonActiveTexture;
        }

        private Texture2D GetRowBackgroundTexture()
        {
            if (_rowBackgroundTexture == null)
            {
                _rowBackgroundTexture = CreateSolidTexture(new Color(0.085f, 0.105f, 0.145f, 1f));
            }

            return _rowBackgroundTexture;
        }

        private Texture2D GetRowAltBackgroundTexture()
        {
            if (_rowAltBackgroundTexture == null)
            {
                _rowAltBackgroundTexture = CreateSolidTexture(new Color(0.055f, 0.073f, 0.105f, 1f));
            }

            return _rowAltBackgroundTexture;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void RaiseStatus(string message)
        {
            if (StatusChanged != null)
            {
                StatusChanged(message);
            }
        }

        private void ConsumeScrollWheelBeforePanel()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || !_windowRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.ScrollWheel)
            {
                currentEvent.Use();
            }
        }
    }
}

