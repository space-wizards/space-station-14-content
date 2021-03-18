#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Surgery
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSurgeryToolComponent))]
    public class SurgeryToolComponent : SharedSurgeryToolComponent, IAfterInteract
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        protected override void Startup()
        {
            base.Startup();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }
        }

        private async void OnUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case PartSelectedUIMessage msg:
                    if (Behavior == null)
                    {
                        return;
                    }

                    if (!Owner.EntityManager.TryGetEntity(msg.Id, out var partEntity))
                    {
                        return;
                    }

                    if (!partEntity.TryGetComponent(out IBodyPart? part))
                    {
                        return;
                    }

                    var performer = message.Session.AttachedEntity;
                    if (performer == null)
                    {
                        return;
                    }

                    var surgerySystem = EntitySystem.Get<SurgerySystem>();
                    if (!surgerySystem.TrySetPerformer(performer, part))
                    {
                        return;
                    }

                    var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                    if (Delay > 0)
                    {
                        var result = await doAfterSystem.DoAfter(new DoAfterEventArgs(performer, Delay, target: partEntity)
                        {
                            BreakOnDamage = true,
                            BreakOnStun = true,
                            BreakOnTargetMove = true,
                            BreakOnUserMove = true,
                            NeedHand = true
                        });


                        if (result == DoAfterStatus.Finished)
                        {
                            Behavior.Perform(performer, part);
                        }
                    }
                    else
                    {
                        Behavior.Perform(performer, part);
                    }

                    break;
            }
        }

        private void OpenUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void UpdateUI(IBody body)
        {
            if (UserInterface == null)
            {
                return;
            }

            var parts = new EntityUid[body.Parts.Count];
            var i = 0;

            foreach (var part in body.Parts.Values)
            {
                parts[i] = part.Owner.Uid;
                i++;
            }

            var state = new SurgeryUIState(parts);
            UserInterface.SetState(state);
        }

        // TODO BODY: Add surgery for dismembered limbs
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (Behavior == null)
            {
                return false;
            }

            var target = eventArgs.Target;
            if (target == null)
            {
                return false;
            }

            var user = eventArgs.User;
            if (!user.TryGetComponent(out IActorComponent? actor) ||
                !target.TryGetComponent(out IBody? body))
            {
                return false;
            }

            var surgerySystem = EntitySystem.Get<SurgerySystem>();

            if (!surgerySystem.TryGetPerformerPart(eventArgs.User, out var part))
            {
                OpenUI(actor.playerSession);
                UpdateUI(body);
                return true;
            }

            Behavior.Perform(user, part);
            return true;
        }
    }
}
