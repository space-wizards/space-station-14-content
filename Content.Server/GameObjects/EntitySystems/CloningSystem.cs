using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Medical;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems.Medical;
using Content.Server.Mobs;
using Content.Shared.GameTicking;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using static Content.Shared.GameObjects.Components.Medical.SharedCloningPodComponent;

namespace Content.Server.GameObjects.EntitySystems
{
    internal sealed class CloningSystem : EntitySystem, IResettingEntitySystem
    {
        public readonly Dictionary<int, Mind> Minds = new();
        public readonly Dictionary<Mind, EntityUid> Clones = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ActivateInWorldMessage>(HandleActivate);
            SubscribeLocalEvent<CloningPodComponent, CloneMindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloningPodComponent, CloneMindRemovedMessage>(HandleMindRemoved);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<CloningPodComponent, ActivateInWorldMessage>(HandleActivate);
            UnsubscribeLocalEvent<CloningPodComponent, CloneMindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloningPodComponent, CloneMindRemovedMessage>(HandleMindRemoved);
        }

        internal void TransferMindToClone(Mind mind)
        {
            if (!Clones.TryGetValue(mind, out var entityUid) ||
                !EntityManager.TryGetEntity(entityUid, out var entity) ||
                !entity.TryGetComponent(out MindComponent? mindComp) ||
                mindComp.Mind != null)
                return;

            mind?.TransferTo(entity);
            mind?.UnVisit();
        }

        private void HandleActivate(EntityUid uid, CloningPodComponent component, ActivateInWorldMessage args)
        {
            if (!component.Powered ||
                !args.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            component.UserInterface?.Open(actor.playerSession);
        }

        private void HandleMindAdded(EntityUid uid, CloningPodComponent component, CloneMindAddedMessage message)
        {
            if (message.Uid != component.BodyContainer?.ContainedEntity?.Uid)
                return;

            component.UpdateStatus(CloningPodStatus.Cloning);
        }

        private void HandleMindRemoved(EntityUid uid, CloningPodComponent component, CloneMindRemovedMessage message)
        {
            if (message.Uid != component.BodyContainer?.ContainedEntity?.Uid)
                return;

            component.UpdateStatus(CloningPodStatus.NoMind);
        }

        public override void Update(float frameTime)
        {
            foreach (var (cloning, power) in ComponentManager.EntityQuery<CloningPodComponent, PowerReceiverComponent>(true))
            {
                if (!power.Powered)
                    return;

                if (cloning.BodyContainer.ContainedEntity != null)
                {
                    cloning.CloningProgress += frameTime;
                    cloning.CloningProgress = MathHelper.Clamp(cloning.CloningProgress, 0f, cloning.CloningTime);
                }

                if (cloning.CapturedMind?.Session?.AttachedEntity == cloning.BodyContainer.ContainedEntity)
                {
                    cloning.Eject();
                }

                UpdateUserInterface(cloning);
            }
        }

        public void UpdateUserInterface(CloningPodComponent comp)
        {
            var idToUser = GetIdToUser();
            comp.UserInterface?.SetState(
                new CloningPodBoundUserInterfaceState(
                    idToUser,
                    comp.CloningProgress,
                    comp.Status == CloningPodStatus.Cloning));
        }

        public void AddToDnaScans(Mind mind)
        {
            if (!Minds.ContainsValue(mind))
            {
                Minds.Add(Minds.Count, mind);
            }
        }

        public bool HasDnaScan(Mind mind)
        {
            return Minds.ContainsValue(mind);
        }

        public Dictionary<int, string?> GetIdToUser()
        {
            return Minds.ToDictionary(m => m.Key, m => m.Value.CharacterName);
        }

        public void Reset()
        {
            Minds.Clear();
            Clones.Clear();
        }
    }
}
