using System.Linq;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Effect
{
    public class OrganExtractionEffect : IOperationEffect
    {
        public void Execute(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (surgeon.Mechanism == null ||
                !target.Owner.TryGetComponent(out IBodyPart? part) ||
                part.Mechanisms.FirstOrDefault() is not { } mechanism)
            {
                return;
            }

            part.RemoveMechanism(mechanism);
        }
    }
}
