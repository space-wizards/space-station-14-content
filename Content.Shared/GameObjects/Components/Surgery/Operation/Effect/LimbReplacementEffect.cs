using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Effect
{
    public class LimbReplacementEffect : IOperationEffect
    {
        public void Execute(SurgeryTargetComponent target)
        {
            // target.Owner.GetComponentOrNull<IBody>()?.
        }
    }
}
