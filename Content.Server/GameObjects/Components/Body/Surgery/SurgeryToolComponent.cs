#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body.Surgery.Messages;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Surgery
{
    /// <summary>
    ///     Server-side component representing a generic tool capable of performing surgery.
    ///     For instance, the scalpel.
    /// </summary>
    [RegisterComponent]
    public class SurgeryToolComponent : Component, ISurgeon, IAfterInteract
    {
        public override string Name => "SurgeryTool";
        public override uint? NetID => ContentNetIDs.SURGERY;

        private float _baseOperateTime;

        private ISurgeon.MechanismRequestCallback? _callbackCache;

        private int _idHash;

        private SurgeryType _surgeryType;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public IBody? BodyCache { get; private set; }

        public IEntity? PerformerCache { get; private set; }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }

            CloseAllSurgeryUIs();

            if (eventArgs.Target.TryGetComponent(out IBody? body))
            {
                var toSend = new Dictionary<string, int>();

                foreach (var (key, value) in body.Parts)
                {
                    if (value.SurgeryCheck(_surgeryType))
                    {
                        toSend.Add(key + ": " + value.Name, _idHash++);
                    }
                }

                if (toSend.Count > 0)
                {
                    OpenSurgeryUI(actor.playerSession);
                    UpdateSurgeryUIBodyPartRequest(actor.playerSession, toSend);
                    PerformerCache = eventArgs.User;
                    BodyCache = body;
                }
                else
                {
                    NotUsefulPopup();
                }
            }
            else if (eventArgs.Target.TryGetComponent<IBodyPart>(out var part))
            {
                // Attempt surgery on a DroppedBodyPart - there's only one possible target so no need for selection UI
                PerformerCache = eventArgs.User;

                // If surgery can be performed...
                if (!part.SurgeryCheck(_surgeryType))
                {
                    NotUsefulPopup();
                    return true;
                }

                // ...do the surgery.
                if (part.AttemptSurgery(_surgeryType, part, this,
                    eventArgs.User))
                {
                    return true;
                }

                // Log error if the surgery fails somehow.
                Logger.Debug($"Error when trying to perform surgery on ${nameof(IBodyPart)} {eventArgs.User.Name}");
                throw new InvalidOperationException();
            }

            return true;
        }

        public float BaseOperationTime { get => _baseOperateTime; set => _baseOperateTime = value; }

        public void RequestMechanism(IEnumerable<IMechanism> options, ISurgeon.MechanismRequestCallback callback)
        {
            var toSend = new Dictionary<string, int>();

            foreach (var mechanism in options)
            {
                toSend.Add(mechanism.Name, _idHash++);
            }

            if (toSend.Count > 0 && PerformerCache != null)
            {
                OpenSurgeryUI(PerformerCache.GetComponent<BasicActorComponent>().playerSession);
                UpdateSurgeryUIMechanismRequest(PerformerCache.GetComponent<BasicActorComponent>().playerSession,
                    toSend);
                _callbackCache = callback;
            }
            else
            {
                Logger.Debug("Error on callback from mechanisms: there were no viable options to choose from!");
                throw new InvalidOperationException();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _surgeryType, "surgeryType", SurgeryType.Incision);
            serializer.DataField(ref _baseOperateTime, "baseOperateTime", 5);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }
        }

        private void OnUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case PartSelectedUIMessage msg:
                    OnReceivePart(msg.Slot);
                    break;
                case MechanismSelectedUIMessage msg:
                    OnReceiveMechanism(msg.Slot, msg.MechanismId);
                    break;
            }
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);

            var message = new SurgeryWindowOpenMessage(this);

            SendMessage(message);
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
        }

        private void UpdateSurgeryUIBodyPartRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
        }

        private void UpdateSurgeryUIMechanismRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestMechanismSurgeryUIMessage(options), session);
        }

        private void ClearUIData()
        {
            PerformerCache = null;
            BodyCache = null;
            _callbackCache = null;
        }

        private void CloseSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Close(session);
            ClearUIData();
        }

        public void CloseAllSurgeryUIs()
        {
            UserInterface?.CloseAll();
            ClearUIData();
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     <see cref="IBodyPart"/> that can be operated on.
        /// </summary>
        private void OnReceivePart(string slot)
        {
            if (PerformerCache == null ||
                !PerformerCache.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (BodyCache == null ||
                !BodyCache.TryGetPart(slot, out var part))
            {
                NotUsefulAnymorePopup();
                return;
            }

            if (!part.AttemptSurgery(_surgeryType, BodyCache, this, PerformerCache))
            {
                NotUsefulAnymorePopup();
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     <see cref="IMechanism"/>.
        /// </summary>
        private void OnReceiveMechanism(string slot, EntityUid mechanismId)
        {
            if (PerformerCache == null ||
                !PerformerCache.CanInteract() ||
                !PerformerCache.TryGetComponent(out IActorComponent? actor) ||
                BodyCache == null ||
                !PerformerCache.InRangeUnobstructed(BodyCache) ||
                !BodyCache.TryGetPart(slot, out var part) ||
                !Owner.EntityManager.TryGetEntity(mechanismId, out var mechanismEntity) ||
                !mechanismEntity.TryGetComponent(out IMechanism? mechanism) ||
                !part.HasMechanism(mechanism))
            {
                NotUsefulAnymorePopup();
                return;
            }

            CloseSurgeryUI(actor.playerSession);
            _callbackCache?.Invoke(mechanism, BodyCache, this, PerformerCache);
        }

        private void NotUsefulPopup()
        {
            BodyCache?.Owner.PopupMessage(PerformerCache,
                Loc.GetString("You see no useful way to use {0:theName}.", Owner));
        }

        private void NotUsefulAnymorePopup()
        {
            BodyCache?.Owner.PopupMessage(PerformerCache,
                Loc.GetString("You see no useful way to use {0:theName} anymore.", Owner));
        }
    }
}
