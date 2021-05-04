using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Effect
{
    public class AmputationEffect : IOperationEffect
    {
        public void Execute(SurgeryTargetComponent target)
        {
            if (target.Owner.TryGetComponent(out IBodyPart? part))
            {
                part.Body?.RemovePart(part);
            }
        }
    }
}
