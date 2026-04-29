using System.Collections.Generic;

namespace SimplePlanes2PartEditor
{
    internal sealed class SelectedPartSnapshot
    {
        public SelectedPartSnapshot(
            int selectedObjectId,
            string partName,
            string partId,
            string partTypeName,
            string partTypeId,
            string partDataTypeName,
            object partDataObject,
            RuntimeDiagnosticSnapshot runtimeDiagnostics,
            string xmlText,
            List<InspectableGroup> groups)
        {
            SelectedObjectId = selectedObjectId;
            PartName = partName;
            PartId = partId;
            PartTypeName = partTypeName;
            PartTypeId = partTypeId;
            PartDataTypeName = partDataTypeName;
            PartDataObject = partDataObject;
            RuntimeDiagnostics = runtimeDiagnostics;
            XmlText = xmlText;
            Groups = groups;
        }

        public int SelectedObjectId { get; private set; }

        public string PartName { get; private set; }

        public string PartId { get; private set; }

        public string PartTypeName { get; private set; }

        public string PartTypeId { get; private set; }

        public string PartDataTypeName { get; private set; }

        public object PartDataObject { get; private set; }

        public RuntimeDiagnosticSnapshot RuntimeDiagnostics { get; private set; }

        public string XmlText { get; private set; }

        public List<InspectableGroup> Groups { get; private set; }
    }
}
