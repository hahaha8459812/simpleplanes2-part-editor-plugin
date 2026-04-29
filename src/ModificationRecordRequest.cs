namespace SimplePlanes2PartEditor
{
    internal sealed class ModificationRecordRequest
    {
        public string OperationName { get; set; }

        public string RefreshMode { get; set; }

        public SelectedPartSnapshot Snapshot { get; set; }

        public InspectableGroup Group { get; set; }

        public InspectableMember Member { get; set; }

        public string BeforeValue { get; set; }

        public string RequestedValue { get; set; }

        public string AfterValue { get; set; }

        public bool ApplySucceeded { get; set; }

        public bool RefreshSucceeded { get; set; }

        public string Error { get; set; }
    }
}
