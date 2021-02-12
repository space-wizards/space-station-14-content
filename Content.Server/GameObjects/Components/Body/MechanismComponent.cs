#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    [ComponentReference(typeof(IMechanism))]
    public class MechanismComponent : SharedMechanismComponent, IAfterInteract
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            CloseAllSurgeryUIs();
            Performer = null;
            SelectedBody = null;

            if (eventArgs.Target.TryGetComponent(out IBody? body))
            {
                SendParts(eventArgs, body);
            }
            else if (eventArgs.Target.TryGetComponent<IBodyPart>(out var part) &&
                     !part.TryAddMechanism(this))
            {
                eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("You can't fit it in!"));
            }

            return true;
        }

        private void SendParts(AfterInteractEventArgs eventArgs, IBody body)
        {
            var toSend = new Dictionary<string, int>();

            foreach (var (key, value) in body.Parts)
            {
                if (value.CanAddMechanism(this))
                {
                    toSend.Add(key + ": " + value.Name, IdHash++);
                }
            }

            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                OpenSurgeryUI(actor.playerSession);
                UpdateSurgeryUI(actor.playerSession, toSend);
                Performer = eventArgs.User;
            }
            else
            {
                eventArgs.Target.PopupMessage(eventArgs.User,
                    Loc.GetString("You see no way to install the {0}.", Owner.Name));
            }
        }

        private void NotUsefulPopup(IEntity entity)
        {
            entity.PopupMessage(entity,
                Loc.GetString("You see no useful way to use the {0} anymore.", Owner.Name));
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyParts that can be operated on.
        /// </summary>
        private void HandlePart(int index)
        {
            if (Performer == null ||
                !Performer.TryGetComponent(out IActorComponent? actor) ||
                actor.playerSession.AttachedEntity == null)
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (SelectedBody == null)
            {
                NotUsefulPopup(Performer);
                return;
            }

            if (SelectedBody.Parts.Count <= index)
            {
                NotUsefulPopup(Performer);
                return;
            }

            var selectedPart = SelectedBody.PartAt(index).Value;
            var message = selectedPart.TryAddMechanism(this)
                ? Loc.GetString("You jam {0:theName} inside {1:them}.", Owner, Performer)
                : Loc.GetString("You can't fit it in!");
            var popupEntity = selectedPart.Body?.Owner ?? Performer;

            popupEntity.PopupMessage(Performer, message);
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void UpdateSurgeryUI(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
        }

        private void CloseSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Close(session);
        }

        private void CloseAllSurgeryUIs()
        {
            UserInterface?.CloseAll();
        }

        private void OnUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ReceivePartMessage msg:
                    HandlePart(msg.SelectedOptionId);
                    break;
            }
        }
    }
}
