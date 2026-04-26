using System.Collections.Generic;

namespace SimplePlanes2PartEditor
{
    internal sealed class InspectableGroup
    {
        public InspectableGroup(string title, string targetTypeName, object targetObject, List<InspectableMember> members)
        {
            Title = title;
            TargetTypeName = targetTypeName;
            TargetObject = targetObject;
            Members = members;
        }

        public string Title { get; private set; }

        public string TargetTypeName { get; private set; }

        public object TargetObject { get; private set; }

        public List<InspectableMember> Members { get; private set; }
    }
}
