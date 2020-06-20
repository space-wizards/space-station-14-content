﻿using System;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    /// <summary>
    /// This is used for the lasergun / flash rechargers
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IInteractUsing))]
    public sealed class WeaponCapacitorChargerComponent : BaseCharger, IActivate, IInteractUsing
    {
        public override string Name => "WeaponCapacitorCharger";
        public override double CellChargePercent => _container.ContainedEntity != null ?
            _container.ContainedEntity.GetComponent<PowerCellComponent>().CurrentCharge /
            _container.ContainedEntity.GetComponent<PowerCellComponent>().MaxCharge * 100 : 0.0f;

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var result = TryInsertItem(eventArgs.Using);
            if (!result)
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                eventArgs.User.PopupMessage(Owner, localizationManager.GetString("Unable to insert capacitor"));
            }

            return result;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            RemoveItemToHand(eventArgs.User);
        }

        [Verb]
        private sealed class InsertVerb : Verb<WeaponCapacitorChargerComponent>
        {
            protected override void GetData(IEntity user, WeaponCapacitorChargerComponent component, VerbData data)
            {
                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (handsComponent.GetActiveHand == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = "Insert";
                    return;
                }

                if (component._container.ContainedEntity != null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                }

                data.Text = $"Insert {handsComponent.GetActiveHand.Owner.Name}";
            }

            protected override void Activate(IEntity user, WeaponCapacitorChargerComponent component)
            {
                if (!user.TryGetComponent(out HandsComponent handsComponent))
                {
                    return;
                }

                if (handsComponent.GetActiveHand == null)
                {
                    return;
                }
                var userItem = handsComponent.GetActiveHand.Owner;
                handsComponent.Drop(userItem);
                component.TryInsertItem(userItem);
            }
        }

        [Verb]
        private sealed class EjectVerb : Verb<WeaponCapacitorChargerComponent>
        {
            protected override void GetData(IEntity user, WeaponCapacitorChargerComponent component, VerbData data)
            {
                if (component._container.ContainedEntity == null)
                {
                    data.Visibility = VerbVisibility.Disabled;
                    data.Text = "Eject";
                    return;
                }

                data.Text = $"Eject {component._container.ContainedEntity.Name}";
            }

            protected override void Activate(IEntity user, WeaponCapacitorChargerComponent component)
            {
                component.RemoveItem();
            }
        }

        public bool TryInsertItem(IEntity entity)
        {
            if (!entity.HasComponent<PowerCellComponent>() ||
                _container.ContainedEntity != null)
            {
                return false;
            }

            HeldItem = entity;

            if (!_container.Insert(HeldItem))
            {
                return false;
            }
            UpdateStatus();
            return true;
        }

        protected override CellChargerStatus GetStatus()
        {
            if (!_powerReceiver.Powered)
            {
                return CellChargerStatus.Off;
            }

            if (_container.ContainedEntity == null)
            {
                return CellChargerStatus.Empty;
            }

            if (_container.ContainedEntity.TryGetComponent(out PowerCellComponent component) &&
                Math.Abs(component.MaxCharge - component.CurrentCharge) < 0.01)
            {
                return CellChargerStatus.Charged;
            }

            return CellChargerStatus.Charging;
        }

        protected override void TransferPower(float frameTime)
        {
            // Two numbers: One for how much power actually goes into the device (chargeAmount) and
            // chargeLoss which is how much is drawn from the powernet
            _container.ContainedEntity.TryGetComponent(out PowerCellComponent weaponCapacitorComponent);
            var chargeLoss = Math.Min(ChargeRate * frameTime, weaponCapacitorComponent.MaxCharge - weaponCapacitorComponent.CurrentCharge) * _transferRatio;
            _powerReceiver.Load = chargeLoss;

            if (!_powerReceiver.Powered)
            {
                // No power: Event should update to Off status
                return;
            }

            var chargeAmount = chargeLoss * _transferEfficiency;

            weaponCapacitorComponent.CurrentCharge += chargeAmount;
            // Just so the sprite won't be set to 99.99999% visibility
            if (weaponCapacitorComponent.MaxCharge - weaponCapacitorComponent.CurrentCharge < 0.01)
            {
                weaponCapacitorComponent.CurrentCharge = weaponCapacitorComponent.MaxCharge;
            }
            UpdateStatus();
        }

    }
}
