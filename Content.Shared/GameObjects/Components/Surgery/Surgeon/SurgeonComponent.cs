using System.Threading;
using Content.Shared.GameObjects.Components.Body.Slot;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon
{
    [RegisterComponent]
    public class SurgeonComponent : Component
    {
        public override string Name => "Surgeon";
        public override uint? NetID => ContentNetIDs.SURGEON;

        private SurgeryTargetComponent? _target;
        private BodyPartSlot? _slot;

        public SurgeryTargetComponent? Target
        {
            get => _target;
            set
            {
                if (_target == value)
                {
                    return;
                }

                _target = value;
                Dirty();
            }
        }

        public BodyPartSlot? Slot
        {
            get => _slot;
            set
            {
                if (_slot == value)
                {
                    return;
                }

                _slot = value;
                Dirty();
            }
        }

        public CancellationTokenSource? SurgeryCancellation { get; set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SurgeonComponentState state)
            {
                return;
            }

            _target = state.Target == null
                ? null
                : Owner.EntityManager.GetEntity(state.Target.Value).EnsureComponent<SurgeryTargetComponent>();
        }
    }
}
