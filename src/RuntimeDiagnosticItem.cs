namespace SimplePlanes2PartEditor
{
    internal sealed class RuntimeDiagnosticItem
    {
        public RuntimeDiagnosticItem(string labelKey, string value, bool isHealthy)
        {
            LabelKey = labelKey;
            Value = value;
            IsHealthy = isHealthy;
        }

        public string LabelKey { get; private set; }

        public string Value { get; private set; }

        public bool IsHealthy { get; private set; }
    }
}
