using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon.ComponentMessages
{
    public class SurgeonStartedOperationComponentMessage : ComponentMessage
    {
        public SurgeonStartedOperationComponentMessage(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation)
        {
            Directed = false;

            Surgeon = surgeon;
            Target = target;
            Operation = operation;
        }

        public SurgeonComponent Surgeon { get; }

        public SurgeryTargetComponent Target { get; }

        public SurgeryOperationPrototype Operation { get; }
    }
}
