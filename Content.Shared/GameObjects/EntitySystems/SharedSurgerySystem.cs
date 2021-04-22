using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Operation.Step;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class SharedSurgerySystem : EntitySystem
    {
        public const string SurgeryLogId = "surgery";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            ValidateOperations();

            SubscribeLocalEvent<SurgeryTargetComponent, SurgeryTargetComponentRemovedMessage>(HandleSurgeryTargetComponentRemovedMessage);
        }

        private void HandleSurgeryTargetComponentRemovedMessage(EntityUid uid, SurgeryTargetComponent component, SurgeryTargetComponentRemovedMessage args)
        {

        }

        private void ValidateOperations()
        {
            foreach (var operation in _prototypeManager.EnumeratePrototypes<SurgeryOperationPrototype>())
            {
                foreach (var step in operation.Steps)
                {
                    if (!_prototypeManager.HasIndex<SurgeryStepPrototype>(step.Id))
                    {
                        throw new PrototypeLoadException(
                            $"Invalid {nameof(SurgeryStepPrototype)} found in surgery operation with id {operation.ID}: No step found with id {step}");
                    }
                }
            }
        }
    }
}
