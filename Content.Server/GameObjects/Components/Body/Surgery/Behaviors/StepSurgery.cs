using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery.Step;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Body.Surgery.Behaviors
{
    public class StepSurgery : SurgeryBehavior
    {
        [field: DataField("step")] private string? StepId { get; }

        public SurgeryStepPrototype? Step => StepId == null
            ? null
            : IoCManager.Resolve<IPrototypeManager>().Index<SurgeryStepPrototype>(StepId);

        public override bool Perform(IEntity surgeon, IBodyPart part)
        {
            var step = Step;

            if (step == null)
            {
                return false;
            }

            var target = part.Body?.Owner;

            surgeon.PopupMessage(step.SurgeonBeginPopup(surgeon, target, part.Owner));

            if (target != null && target != surgeon)
            {
                target.PopupMessage(step.TargetBeginPopup(surgeon, part.Owner));
            }

            surgeon.PopupMessageOtherClients(step.OutsiderBeginPopup(surgeon, target, part.Owner));

            return part.AddSurgeryTag(step.ID);
        }
    }
}
