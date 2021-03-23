using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon.ComponentMessages
{
    public class SurgeonStoppedOperationComponentMessage : ComponentMessage
    {
        public SurgeonStoppedOperationComponentMessage(SurgeonComponent surgeon, SurgeryTargetComponent oldTarget,
            SurgeryOperationPrototype oldOperation)
        {
            Surgeon = surgeon;
            OldTarget = oldTarget;
            OldOperation = oldOperation;
        }

        public SurgeonComponent Surgeon { get; }

        public SurgeryTargetComponent OldTarget { get; }

        public SurgeryOperationPrototype OldOperation { get; }
    }
}
