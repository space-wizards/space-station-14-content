using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Respiratory
{
    [RegisterComponent]
    public class LungComponent : Component, IGasMixtureHolder
    {
        public override string Name => "Lung";

        private float _accumulatedFrameTime;

        /// <summary>
        ///     The pressure that this lung exerts on the air around it
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float Pressure { get; set; }

        [ViewVariables] public GasMixture Air { get; set; }

        [ViewVariables] public LungStatus Status { get; set; }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            Air = new GasMixture();

            serializer.DataReadWriteFunction(
                "volume",
                6,
                vol => Air.Volume = vol,
                () => Air.Volume);
            serializer.DataField(this, l => l.Pressure, "pressure", 100);
        }

        public void Update(float frameTime)
        {
            if (Status == LungStatus.None)
            {
                Status = LungStatus.Inhaling;
            }

            _accumulatedFrameTime += Status switch
            {
                LungStatus.Inhaling => frameTime,
                LungStatus.Exhaling => -frameTime,
                _ => throw new ArgumentOutOfRangeException()
            };

            var absoluteTime = Math.Abs(_accumulatedFrameTime);
            if (absoluteTime < 2)
            {
                return;
            }

            switch (Status)
            {
                case LungStatus.Inhaling:
                    Inhale(absoluteTime);
                    Status = LungStatus.Exhaling;
                    break;
                case LungStatus.Exhaling:
                    Exhale(absoluteTime);
                    Status = LungStatus.Inhaling;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _accumulatedFrameTime = absoluteTime - 2;
        }

        public void Inhale(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            GasMixture air = null;
            // if (Owner.TryGetComponent(out InventoryComponent inventory) &&
            //     inventory.TryGetSlotItem<ItemComponent>(EquipmentSlotDefines.Slots.MASK, out var maskItem) &&
            //     maskItem.Owner.TryGetComponent<BreathToolComponent>(out var mask) && mask.IsConnected())
            // {
            //     air = mask.ConnectedGasTank.GetComponent<IGasMixtureHolder>().Air;
            // }

            // just get air from tile
            if (air == null)
            {
                Owner.Transform.Coordinates.TryGetTileAir(out air);
            }

            // no matter what there are no available air sources
            if (air == null)
            {
                return;
            }

            var amount = Atmospherics.BreathVolume * frameTime / air.Volume;
            var temp = air.Remove(amount);

            temp.PumpGasTo(Air, Pressure);
            Air.PumpGasTo(bloodstream.Air, Pressure);
            air.Merge(temp);
        }

        public void Exhale(float frameTime)
        {
            if (!Owner.TryGetComponent(out BloodstreamComponent bloodstream))
            {
                return;
            }

            if (!Owner.Transform.Coordinates.TryGetTileAir(out var tileAir))
            {
                return;
            }

            bloodstream.PumpToxins(Air, Pressure);

            var amount = Atmospherics.BreathPercentage * frameTime;
            var volumeRatio = amount / tileAir.Volume;
            var temp = tileAir.RemoveRatio(volumeRatio);

            temp.PumpGasTo(tileAir, Pressure);
            Air.Merge(temp);
        }
    }

    public enum LungStatus
    {
        None = 0,
        Inhaling,
        Exhaling
    }
}
