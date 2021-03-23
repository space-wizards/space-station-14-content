using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Operation.Effect;
using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Server.GameObjects.Components.Surgery.Operation.Effect
{
    public class AmputationOperation : IOperationEffect
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
