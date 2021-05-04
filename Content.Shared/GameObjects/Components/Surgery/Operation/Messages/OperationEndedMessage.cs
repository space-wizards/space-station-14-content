using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Messages
{
    public class OperationEndedMessage : EntityEventArgs
    {
        public OperationEndedMessage(SurgeonComponent oldSurgeon, SurgeryOperationPrototype oldOperation)
        {
            OldSurgeon = oldSurgeon;
            OldOperation = oldOperation;
        }

        public SurgeonComponent OldSurgeon { get; }

        public SurgeryOperationPrototype OldOperation { get; }
    }
}
