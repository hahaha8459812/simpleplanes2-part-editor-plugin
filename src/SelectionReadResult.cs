namespace SimplePlanes2PartEditor
{
    internal sealed class SelectionReadResult
    {
        private SelectionReadResult(string statusKey, SelectedPartSnapshot snapshot)
        {
            StatusKey = statusKey;
            Snapshot = snapshot;
        }

        public string StatusKey { get; private set; }

        public SelectedPartSnapshot Snapshot { get; private set; }

        public static SelectionReadResult FromStatus(string statusKey)
        {
            return new SelectionReadResult(statusKey, null);
        }

        public static SelectionReadResult FromSnapshot(SelectedPartSnapshot snapshot)
        {
            return new SelectionReadResult("status.ready", snapshot);
        }
    }
}
