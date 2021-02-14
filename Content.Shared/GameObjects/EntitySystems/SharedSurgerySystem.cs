#nullable enable
using Content.Shared.GameObjects.Components.Body.Surgery.Operations;
using Content.Shared.GameObjects.Components.Body.Surgery.Step;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class SharedSurgerySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            ValidateSurgeryOperationSteps();
        }

        private void ValidateSurgeryOperationSteps()
        {
            foreach (var operation in _prototypeManager.EnumeratePrototypes<SurgeryOperationPrototype>())
            {
                foreach (var step in operation.Steps)
                {
                    if (!_prototypeManager.HasIndex<SurgeryStepPrototype>(step))
                    {
                        throw new PrototypeLoadException($"Invalid {nameof(SurgeryStepPrototype)} found in surgery operation with id {operation.ID}: No step found with id {step}");
                    }
                }
            }
        }
    }
}
