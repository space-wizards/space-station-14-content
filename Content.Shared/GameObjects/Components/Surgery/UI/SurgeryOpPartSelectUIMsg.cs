using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Surgery.UI
{
    [Serializable, NetSerializable]
    public class SurgeryOpPartSelectUIMsg : BoundUserInterfaceMessage
    {
        public SurgeryOpPartSelectUIMsg(EntityUid part, string operationId)
        {
            Part = part;
            OperationId = operationId;
        }

        public EntityUid Part { get; }

        public string OperationId { get; }
    }
}
