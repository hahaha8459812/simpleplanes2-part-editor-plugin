using System;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx.Logging;

namespace SimplePlanes2PartEditor
{
    internal sealed class ModificationRecordService
    {
        private readonly ManualLogSource _logger;
        private readonly string _recordDirectoryPath;
        private string _recordFilePath;

        public ModificationRecordService(ManualLogSource logger, string pluginRootPath)
        {
            _logger = logger;
            _recordDirectoryPath = Path.Combine(pluginRootPath, "modification-records");
        }

        public bool IsRecording { get; private set; }

        public string RecordFilePath
        {
            get { return _recordFilePath ?? string.Empty; }
        }

        public void Start()
        {
            Directory.CreateDirectory(_recordDirectoryPath);
            _recordFilePath = Path.Combine(_recordDirectoryPath, "record-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".jsonl");
            File.AppendAllText(_recordFilePath, BuildSessionLine("start") + Environment.NewLine, Encoding.UTF8);
            IsRecording = true;

            if (_logger != null)
            {
                _logger.LogInfo("Modification recorder started: " + _recordFilePath);
            }
        }

        public void Stop()
        {
            if (!IsRecording)
            {
                return;
            }

            File.AppendAllText(_recordFilePath, BuildSessionLine("stop") + Environment.NewLine, Encoding.UTF8);
            IsRecording = false;

            if (_logger != null)
            {
                _logger.LogInfo("Modification recorder stopped: " + _recordFilePath);
            }
        }

        public void Toggle()
        {
            if (IsRecording)
            {
                Stop();
                return;
            }

            Start();
        }

        public void Record(ModificationRecordRequest request)
        {
            string line;

            if (!IsRecording || request == null || string.IsNullOrEmpty(_recordFilePath))
            {
                return;
            }

            line = BuildModificationLine(request);
            File.AppendAllText(_recordFilePath, line + Environment.NewLine, Encoding.UTF8);

            if (_logger != null)
            {
                _logger.LogInfo("Modification record: " + line);
            }
        }

        private static string BuildSessionLine(string state)
        {
            StringBuilder builder = new StringBuilder();
            AppendJsonStart(builder);
            AppendJsonProperty(builder, "event", "recorder." + state, false);
            AppendJsonProperty(builder, "time", DateTime.Now.ToString("O", CultureInfo.InvariantCulture), true);
            builder.Append("}");
            return builder.ToString();
        }

        private static string BuildModificationLine(ModificationRecordRequest request)
        {
            SelectedPartSnapshot snapshot = request.Snapshot;
            InspectableGroup group = request.Group;
            InspectableMember member = request.Member;
            StringBuilder builder = new StringBuilder();

            AppendJsonStart(builder);
            AppendJsonProperty(builder, "event", "member.apply", false);
            AppendJsonProperty(builder, "time", DateTime.Now.ToString("O", CultureInfo.InvariantCulture), true);
            AppendJsonProperty(builder, "operation", request.OperationName, true);
            AppendJsonProperty(builder, "refreshMode", request.RefreshMode, true);
            AppendJsonProperty(builder, "partName", snapshot == null ? string.Empty : snapshot.PartName, true);
            AppendJsonProperty(builder, "partId", snapshot == null ? string.Empty : snapshot.PartId, true);
            AppendJsonProperty(builder, "partType", snapshot == null ? string.Empty : snapshot.PartTypeName, true);
            AppendJsonProperty(builder, "partTypeId", snapshot == null ? string.Empty : snapshot.PartTypeId, true);
            AppendJsonProperty(builder, "partDataType", snapshot == null ? string.Empty : snapshot.PartDataTypeName, true);
            AppendJsonProperty(builder, "groupTitle", group == null ? string.Empty : group.Title, true);
            AppendJsonProperty(builder, "groupType", group == null ? string.Empty : group.TargetTypeName, true);
            AppendJsonProperty(builder, "memberName", member == null ? string.Empty : member.Name, true);
            AppendJsonProperty(builder, "memberType", member == null ? string.Empty : member.TypeName, true);
            AppendJsonProperty(builder, "memberAccess", member == null ? string.Empty : member.Access, true);
            AppendJsonProperty(builder, "memberAttributes", member == null ? string.Empty : member.Attributes, true);
            AppendJsonProperty(builder, "beforeValue", request.BeforeValue, true);
            AppendJsonProperty(builder, "requestedValue", request.RequestedValue, true);
            AppendJsonProperty(builder, "afterValue", request.AfterValue, true);
            AppendJsonProperty(builder, "applySucceeded", request.ApplySucceeded, true);
            AppendJsonProperty(builder, "refreshSucceeded", request.RefreshSucceeded, true);
            AppendJsonProperty(builder, "error", request.Error, true);
            builder.Append("}");
            return builder.ToString();
        }

        private static void AppendJsonStart(StringBuilder builder)
        {
            builder.Append("{");
        }

        private static void AppendJsonProperty(StringBuilder builder, string name, string value, bool appendComma)
        {
            if (appendComma)
            {
                builder.Append(",");
            }

            builder.Append("\"");
            builder.Append(EscapeJson(name));
            builder.Append("\":\"");
            builder.Append(EscapeJson(value ?? string.Empty));
            builder.Append("\"");
        }

        private static void AppendJsonProperty(StringBuilder builder, string name, bool value, bool appendComma)
        {
            if (appendComma)
            {
                builder.Append(",");
            }

            builder.Append("\"");
            builder.Append(EscapeJson(name));
            builder.Append("\":");
            builder.Append(value ? "true" : "false");
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
