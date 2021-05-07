using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Step.Conditional
{
    public class StumpConditional : IOperationStepConditional
    {
        public bool Necessary(SurgeryTargetComponent target)
        {
            return target.Owner.TryGetComponent(out IBodyPart? part) &&
                   part.Body != null &&
                   part.Body.TryGetSlot(part, out var slot) &&
                   slot.HasStump;
        }
    }
}
