using System;

namespace SimplePlanes2PartEditor
{
    internal sealed class SelectionProbeResult
    {
        private SelectionProbeResult(string statusKey, bool isDesignerAvailable, bool hasSelectedPart, int selectedObjectId)
        {
            StatusKey = statusKey;
            IsDesignerAvailable = isDesignerAvailable;
            HasSelectedPart = hasSelectedPart;
            SelectedObjectId = selectedObjectId;
        }

        public string StatusKey { get; private set; }

        public bool IsDesignerAvailable { get; private set; }

        public bool HasSelectedPart { get; private set; }

        public int SelectedObjectId { get; private set; }

        public bool IsSameSelection(SelectedPartSnapshot snapshot)
        {
            return HasSelectedPart && snapshot != null && snapshot.SelectedObjectId == SelectedObjectId;
        }

        public bool IsSameStatus(SelectionReadResult selectionReadResult)
        {
            return selectionReadResult != null &&
                   selectionReadResult.Snapshot == null &&
                   string.Equals(selectionReadResult.StatusKey, StatusKey, StringComparison.Ordinal);
        }

        public static SelectionProbeResult FromNoDesigner()
        {
            return new SelectionProbeResult("label.noDesigner", false, false, 0);
        }

        public static SelectionProbeResult FromStatus(string statusKey)
        {
            return new SelectionProbeResult(statusKey, true, false, 0);
        }

        public static SelectionProbeResult FromSelectedPart(int selectedObjectId)
        {
            return new SelectionProbeResult("status.ready", true, true, selectedObjectId);
        }
    }
}
