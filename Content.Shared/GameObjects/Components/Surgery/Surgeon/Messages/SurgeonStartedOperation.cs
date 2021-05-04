using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon.Messages
{
    public class SurgeonStartedOperation : EntityEventArgs
    {
        public SurgeonStartedOperation(SurgeryTargetComponent target, SurgeryOperationPrototype operation)
        {
            Target = target;
            Operation = operation;
        }

        public SurgeryTargetComponent Target { get; }

        public SurgeryOperationPrototype Operation { get; }
    }
}
