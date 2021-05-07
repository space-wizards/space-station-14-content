using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon.Messages
{
    public class SurgeonStoppedOperation : EntityEventArgs
    {
        public SurgeonStoppedOperation(SurgeryTargetComponent oldTarget)
        {
            OldTarget = oldTarget;
        }

        public SurgeryTargetComponent OldTarget { get; }
    }
}
