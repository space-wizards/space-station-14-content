using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Surgery.UI
{
    [Serializable, NetSerializable]
    public class SurgeryOperationSelectedUIMessage : BoundUserInterfaceMessage
    {
        public SurgeryOperationSelectedUIMessage(EntityUid target, string operationId)
        {
            Target = target;
            OperationId = operationId;
        }

        public EntityUid Target { get; }

        public string OperationId { get; }
    }
}
