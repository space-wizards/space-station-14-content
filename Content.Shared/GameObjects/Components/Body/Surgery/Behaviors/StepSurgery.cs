#nullable enable
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery.Step;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Behaviors
{
    public class StepSurgery : SurgeryBehavior
    {
        public SurgeryStepPrototype? Step { get; [UsedImplicitly] private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Step, "step", null);
        }

        public override bool Perform(IEntity performer, IBodyPart part)
        {
            if (Step == null)
            {
                return false;
            }

            return part.AddSurgeryTag(Step.ID);
        }
    }
}
