using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Effect
{
    public class LimbReplacementOperation : IOperationEffect
    {
        public void Execute(SurgeryTargetComponent target)
        {
            // target.Owner.GetComponentOrNull<IBody>()?.
        }
    }
}
