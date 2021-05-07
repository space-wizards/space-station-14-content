using System.Threading;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon
{
    [RegisterComponent]
    public class SurgeonComponent : Component
    {
        public override string Name => "Surgeon";
        public override uint? NetID => ContentNetIDs.SURGEON;

        private SurgeryTargetComponent? _target;
        private IMechanism? _mechanism;

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

        public IMechanism? Mechanism
        {
            get => _mechanism;
            set
            {
                if (_mechanism == value)
                {
                    return;
                }

                _mechanism = value;
                Dirty();
            }
        }

        public CancellationTokenSource? SurgeryCancellation { get; set; }

        public IBody? TargetedBody => _target == null
            ? null
            : _target.Owner.GetComponentOrNull<IBody>() ??
              _target.Owner.GetComponentOrNull<IBodyPart>()?.Body;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SurgeonComponentState(_target?.Owner.Uid, _mechanism?.Owner.Uid);
        }

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
