using System;
using System.Collections.Generic;
using System.Globalization;

namespace SimplePlanes2PartEditor
{
    internal sealed class PluginSettings
    {
        public string Language { get; private set; }
        public string ToggleWindowHotkey { get; private set; }
        public bool UpdateCheckEnabled { get; private set; }
        public string UpdateIndexUrl { get; private set; }
        public float SelectionRefreshIntervalSeconds { get; private set; }
        public int MaxMembersPerGroup { get; private set; }
        public int FontSize { get; private set; }
        public float WindowWidth { get; private set; }
        public float WindowHeight { get; private set; }
        public float BackgroundOpacity { get; private set; }
        public float FloatingButtonX { get; private set; }
        public float FloatingButtonY { get; private set; }
        public float FloatingButtonSize { get; private set; }
        public float ExpandedEditorX { get; private set; }
        public float ExpandedEditorY { get; private set; }
        public float ExpandedEditorWidth { get; private set; }
        public float ExpandedEditorHeight { get; private set; }
        public bool LockFloatingButtonPosition { get; private set; }
        public bool ShowTypeColumn { get; private set; }
        public bool ShowAccessColumn { get; private set; }
        public bool ShowFullTypeName { get; private set; }
        public bool ShowRuntimeCacheMembers { get; private set; }

        private PluginSettings()
        {
            Language = "zh-CN";
            ToggleWindowHotkey = "F8";
            UpdateCheckEnabled = true;
            UpdateIndexUrl = string.Empty;
            SelectionRefreshIntervalSeconds = 0.25f;
            MaxMembersPerGroup = 120;
            FontSize = 18;
            WindowWidth = 1320f;
            WindowHeight = 860f;
            BackgroundOpacity = 0.96f;
            FloatingButtonX = 28f;
            FloatingButtonY = 140f;
            FloatingButtonSize = 52f;
            ExpandedEditorX = 120f;
            ExpandedEditorY = 120f;
            ExpandedEditorWidth = 820f;
            ExpandedEditorHeight = 420f;
            LockFloatingButtonPosition = false;
            ShowTypeColumn = true;
            ShowAccessColumn = true;
            ShowFullTypeName = true;
            ShowRuntimeCacheMembers = false;
        }

        public static PluginSettings CreateDefault()
        {
            return new PluginSettings();
        }

        public static PluginSettings FromJson(string json)
        {
            PluginSettings settings = CreateDefault();
            Dictionary<string, string> values = SimpleJson.ReadFlatObject(json);

            settings.Language = GetString(values, "language", settings.Language);
            settings.ToggleWindowHotkey = GetString(values, "toggleWindowHotkey", settings.ToggleWindowHotkey);
            settings.UpdateCheckEnabled = GetBool(values, "updateCheckEnabled", settings.UpdateCheckEnabled);
            settings.UpdateIndexUrl = GetString(values, "updateIndexUrl", settings.UpdateIndexUrl);
            settings.SelectionRefreshIntervalSeconds = GetFloat(values, "selectionRefreshIntervalSeconds", settings.SelectionRefreshIntervalSeconds);
            settings.MaxMembersPerGroup = GetInt(values, "maxMembersPerGroup", settings.MaxMembersPerGroup);
            settings.FontSize = GetInt(values, "fontSize", settings.FontSize);
            settings.WindowWidth = GetFloat(values, "windowWidth", settings.WindowWidth);
            settings.WindowHeight = GetFloat(values, "windowHeight", settings.WindowHeight);
            settings.BackgroundOpacity = GetFloat(values, "backgroundOpacity", settings.BackgroundOpacity);
            settings.FloatingButtonX = GetFloat(values, "floatingButtonX", settings.FloatingButtonX);
            settings.FloatingButtonY = GetFloat(values, "floatingButtonY", settings.FloatingButtonY);
            settings.FloatingButtonSize = GetFloat(values, "floatingButtonSize", settings.FloatingButtonSize);
            settings.ExpandedEditorX = GetFloat(values, "expandedEditorX", settings.ExpandedEditorX);
            settings.ExpandedEditorY = GetFloat(values, "expandedEditorY", settings.ExpandedEditorY);
            settings.ExpandedEditorWidth = GetFloat(values, "expandedEditorWidth", settings.ExpandedEditorWidth);
            settings.ExpandedEditorHeight = GetFloat(values, "expandedEditorHeight", settings.ExpandedEditorHeight);
            settings.LockFloatingButtonPosition = GetBool(values, "lockFloatingButtonPosition", settings.LockFloatingButtonPosition);
            settings.ShowTypeColumn = GetBool(values, "showTypeColumn", settings.ShowTypeColumn);
            settings.ShowAccessColumn = GetBool(values, "showAccessColumn", settings.ShowAccessColumn);
            settings.ShowFullTypeName = GetBool(values, "showFullTypeName", settings.ShowFullTypeName);
            settings.ShowRuntimeCacheMembers = GetBool(values, "showRuntimeCacheMembers", settings.ShowRuntimeCacheMembers);
            settings.ClampValues();
            return settings;
        }

        public string ToJson()
        {
            return "{\n" +
                   "  \"language\": \"" + SimpleJson.Escape(Language) + "\",\n" +
                   "  \"toggleWindowHotkey\": \"" + SimpleJson.Escape(ToggleWindowHotkey) + "\",\n" +
                   "  \"updateCheckEnabled\": " + UpdateCheckEnabled.ToString().ToLowerInvariant() + ",\n" +
                   "  \"updateIndexUrl\": \"" + SimpleJson.Escape(UpdateIndexUrl) + "\",\n" +
                   "  \"selectionRefreshIntervalSeconds\": " + SelectionRefreshIntervalSeconds.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"maxMembersPerGroup\": " + MaxMembersPerGroup.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"fontSize\": " + FontSize.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"windowWidth\": " + WindowWidth.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"windowHeight\": " + WindowHeight.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"backgroundOpacity\": " + BackgroundOpacity.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"floatingButtonX\": " + FloatingButtonX.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"floatingButtonY\": " + FloatingButtonY.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"floatingButtonSize\": " + FloatingButtonSize.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"expandedEditorX\": " + ExpandedEditorX.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"expandedEditorY\": " + ExpandedEditorY.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"expandedEditorWidth\": " + ExpandedEditorWidth.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"expandedEditorHeight\": " + ExpandedEditorHeight.ToString(CultureInfo.InvariantCulture) + ",\n" +
                   "  \"lockFloatingButtonPosition\": " + LockFloatingButtonPosition.ToString().ToLowerInvariant() + ",\n" +
                   "  \"showTypeColumn\": " + ShowTypeColumn.ToString().ToLowerInvariant() + ",\n" +
                   "  \"showAccessColumn\": " + ShowAccessColumn.ToString().ToLowerInvariant() + ",\n" +
                   "  \"showFullTypeName\": " + ShowFullTypeName.ToString().ToLowerInvariant() + ",\n" +
                   "  \"showRuntimeCacheMembers\": " + ShowRuntimeCacheMembers.ToString().ToLowerInvariant() + "\n" +
                   "}\n";
        }

        public void SetLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                Language = language;
            }
        }

        public void SetToggleWindowHotkey(string hotkey)
        {
            if (!string.IsNullOrEmpty(hotkey))
            {
                ToggleWindowHotkey = hotkey.Trim();
            }
        }

        public void SetUpdateCheckOptions(bool enabled, string indexUrl)
        {
            UpdateCheckEnabled = enabled;
            UpdateIndexUrl = indexUrl == null ? string.Empty : indexUrl.Trim();
        }

        public void SetUiLayout(int fontSize, float windowWidth, float windowHeight, float backgroundOpacity)
        {
            FontSize = fontSize;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
            BackgroundOpacity = backgroundOpacity;
            ClampValues();
        }

        public void SetSelectionRefreshInterval(float seconds)
        {
            SelectionRefreshIntervalSeconds = seconds;
            ClampValues();
        }

        public void SetFloatingButtonPosition(float x, float y)
        {
            FloatingButtonX = x;
            FloatingButtonY = y;
            ClampValues();
        }

        public void SetFloatingButtonSize(float size)
        {
            FloatingButtonSize = size;
            ClampValues();
        }

        public void SetExpandedEditorLayout(float x, float y, float width, float height)
        {
            ExpandedEditorX = x;
            ExpandedEditorY = y;
            ExpandedEditorWidth = width;
            ExpandedEditorHeight = height;
            ClampValues();
        }

        public void SetLockFloatingButtonPosition(bool locked)
        {
            LockFloatingButtonPosition = locked;
        }

        public void SetDisplayOptions(bool showTypeColumn, bool showAccessColumn, bool showFullTypeName)
        {
            ShowTypeColumn = showTypeColumn;
            ShowAccessColumn = showAccessColumn;
            ShowFullTypeName = showFullTypeName;
        }

        public void SetRuntimeCacheMembersVisible(bool visible)
        {
            ShowRuntimeCacheMembers = visible;
        }

        private void ClampValues()
        {
            if (SelectionRefreshIntervalSeconds < 0.1f)
            {
                SelectionRefreshIntervalSeconds = 0.1f;
            }
            else if (SelectionRefreshIntervalSeconds > 5f)
            {
                SelectionRefreshIntervalSeconds = 5f;
            }

            if (MaxMembersPerGroup < 20)
            {
                MaxMembersPerGroup = 20;
            }

            if (FontSize < 12)
            {
                FontSize = 12;
            }
            else if (FontSize > 32)
            {
                FontSize = 32;
            }

            if (WindowWidth < 720f)
            {
                WindowWidth = 720f;
            }
            else if (WindowWidth > 2400f)
            {
                WindowWidth = 2400f;
            }

            if (WindowHeight < 480f)
            {
                WindowHeight = 480f;
            }
            else if (WindowHeight > 1600f)
            {
                WindowHeight = 1600f;
            }

            if (BackgroundOpacity < 0.65f)
            {
                BackgroundOpacity = 0.65f;
            }
            else if (BackgroundOpacity > 1f)
            {
                BackgroundOpacity = 1f;
            }

            if (FloatingButtonX < 0f)
            {
                FloatingButtonX = 0f;
            }

            if (FloatingButtonY < 0f)
            {
                FloatingButtonY = 0f;
            }

            if (FloatingButtonX > 4000f)
            {
                FloatingButtonX = 4000f;
            }

            if (FloatingButtonY > 4000f)
            {
                FloatingButtonY = 4000f;
            }

            if (FloatingButtonSize < 32f)
            {
                FloatingButtonSize = 32f;
            }
            else if (FloatingButtonSize > 120f)
            {
                FloatingButtonSize = 120f;
            }

            if (ExpandedEditorX < 0f)
            {
                ExpandedEditorX = 0f;
            }

            if (ExpandedEditorY < 0f)
            {
                ExpandedEditorY = 0f;
            }

            if (ExpandedEditorX > 4000f)
            {
                ExpandedEditorX = 4000f;
            }

            if (ExpandedEditorY > 4000f)
            {
                ExpandedEditorY = 4000f;
            }

            if (ExpandedEditorWidth < 360f)
            {
                ExpandedEditorWidth = 360f;
            }
            else if (ExpandedEditorWidth > 2400f)
            {
                ExpandedEditorWidth = 2400f;
            }

            if (ExpandedEditorHeight < 220f)
            {
                ExpandedEditorHeight = 220f;
            }
            else if (ExpandedEditorHeight > 1600f)
            {
                ExpandedEditorHeight = 1600f;
            }
        }

        private static string GetString(Dictionary<string, string> values, string key, string fallback)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            return value;
        }

        private static float GetFloat(Dictionary<string, string> values, string key, float fallback)
        {
            string value;
            float parsedValue;
            if (!values.TryGetValue(key, out value) || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                return fallback;
            }

            return parsedValue;
        }

        private static int GetInt(Dictionary<string, string> values, string key, int fallback)
        {
            string value;
            int parsedValue;
            if (!values.TryGetValue(key, out value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                return fallback;
            }

            return parsedValue;
        }

        private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
        {
            string value;
            bool parsedValue;
            if (!values.TryGetValue(key, out value) || !bool.TryParse(value, out parsedValue))
            {
                return fallback;
            }

            return parsedValue;
        }
    }
}
