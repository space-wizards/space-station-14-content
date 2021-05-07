#nullable enable
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Utility;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Body.Part
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    [ComponentReference(typeof(IBodyPart))]
    public class BodyPartComponent : SharedBodyPartComponent
    {
        private Container _mechanismContainer = default!;

        public override bool CanAddMechanism(IMechanism mechanism)
        {
            return base.CanAddMechanism(mechanism) &&
                   _mechanismContainer.CanInsert(mechanism.Owner);
        }

        protected override void OnAddMechanism(IMechanism mechanism)
        {
            base.OnAddMechanism(mechanism);

            _mechanismContainer.Insert(mechanism.Owner);
        }

        protected override void OnRemoveMechanism(IMechanism mechanism)
        {
            base.OnRemoveMechanism(mechanism);

            _mechanismContainer.Remove(mechanism.Owner);
            mechanism.Owner.RandomOffset(0.25f);
        }

        public override void Initialize()
        {
            base.Initialize();

            _mechanismContainer = Owner.EnsureContainer<Container>($"{Name}-{nameof(BodyPartComponent)}");

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var mechanismId in MechanismIds)
            {
                var entity = Owner.EntityManager.SpawnEntity(mechanismId, Owner.Transform.MapPosition);

                if (!entity.TryGetComponent(out IMechanism? mechanism))
                {
                    Logger.Error($"Entity {mechanismId} does not have a {nameof(IMechanism)} component.");
                    continue;
                }

                TryAddMechanism(mechanism, true);
            }
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var mechanism in Mechanisms)
            {
                mechanism.Dirty();
            }
        }

        [Verb]
        public class AttachBodyPartVerb : Verb<BodyPartComponent>
        {
            protected override void GetData(IEntity user, BodyPartComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (user == component.Owner)
                {
                    return;
                }

                if (!user.TryGetComponent(out IActorComponent? actor))
                {
                    return;
                }

                var groupController = IoCManager.Resolve<IConGroupController>();

                if (!groupController.CanCommand(actor.playerSession, "attachbodypart"))
                {
                    return;
                }

                if (!user.TryGetComponent(out IBody? body))
                {
                    return;
                }

                if (body.HasPart(component))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Attach Body Part");
            }

            protected override void Activate(IEntity user, BodyPartComponent component)
            {
                if (!user.TryGetComponent(out IBody? body))
                {
                    return;
                }

                body.SetPart($"{nameof(AttachBodyPartVerb)}-{component.Owner.Uid}", component);
            }
        }
    }
}
