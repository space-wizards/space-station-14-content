﻿using System;
using System.Linq;
using Content.Server.GameObjects.Components.Sound;
using Microsoft.EntityFrameworkCore.Query;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that connects to the powernet
    /// </summary>
    [RegisterComponent]
    public class PowerNodeComponent : Component
    {
        public override string Name => "PowerNode";

        /// <summary>
        /// The powernet this node is connected to
        /// </summary>
        [ViewVariables]
        public Powernet Parent { get; set; }

        /// <summary>
        /// An event handling when this node connects to a powernet
        /// </summary>
        public event EventHandler<PowernetEventArgs> OnPowernetConnect;

        /// <summary>
        /// An event handling when this node disconnects from a powernet
        /// </summary>
        public event EventHandler<PowernetEventArgs> OnPowernetDisconnect;

        /// <summary>
        /// An event that registers us to a regenerating powernet
        /// </summary>
        public event EventHandler<PowernetEventArgs> OnPowernetRegenerate;

        public PowerTransferComponent.WireType NodeWireType { get => _nodewiretype; set => _nodewiretype = value; }
        public float Range { get => _range; set => _range = value; }

        private PowerTransferComponent.WireType _nodewiretype;
        private float _range;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _nodewiretype, "wiretype", PowerTransferComponent.WireType.HVWire);
            serializer.DataField(ref _range, "range", 5f);

        }

        protected override void Startup()
        {
            base.Startup();

            TryCreatePowernetConnection();
        }

        public override void OnRemove()
        {
            DisconnectFromPowernet();

            base.OnRemove();
        }

        /// <summary>
        /// Find a nearby wire which will have a powernet and connect ourselves to its powernet
        /// </summary>
        public void TryCreatePowernetConnection()
        {
            if (Parent != null)
            {
                return;
            }

            var position = Owner.Transform.WorldPosition;

            if(NodeWireType == PowerTransferComponent.WireType.HVWire)
            {
                var sgc = Owner.GetComponent<SnapGridComponent>();
                var wire = sgc.GetCardinalNeighborCells()
                    .SelectMany(x => x.GetLocal()).Distinct()
                    .Select(x => x.TryGetComponent<PowerTransferComponent>(out var c) ? c : null)
                    .Where(x => x != null).Distinct()
                    .ToArray()
                    .OrderByDescending(x => (x.Owner.Transform.WorldPosition - position).Length)
                    .FirstOrDefault();

                if (wire?.Parent != null)
                {
                    ConnectToPowernet(wire.Parent);
                }
            }

            else
            {
                var temp = IoCManager.Resolve<IEntityManager>().GetEntitiesInRange(Owner, Range);
                var wire = IoCManager.Resolve<IEntityManager>().GetEntitiesInRange(Owner, Range)
                    .Select(x => x.TryGetComponent<PowerTransferComponent>(out var c) ? c : null)
                    .Where(c => c != null && c.Type.Equals(PowerTransferComponent.WireType.MVWire))
                    .ToArray()
                    .OrderByDescending(x => (x.Owner.Transform.WorldPosition - position).Length)
                    .FirstOrDefault();

                if (wire?.Parent != null)
                {
                    ConnectToPowernet(wire.Parent);
                }
            }

        }

        /// <summary>
        /// Triggers event telling power components that we connected to a powernet
        /// </summary>
        /// <param name="toconnect"></param>
        public void ConnectToPowernet(Powernet toconnect)
        {
            Parent = toconnect;
            Parent.NodeList.Add(this);
            OnPowernetConnect?.Invoke(this, new PowernetEventArgs(Parent));
        }

        /// <summary>
        /// Triggers event telling power components that we haven't disconnected but have readded ourselves to a regenerated powernet
        /// </summary>
        /// <param name="toconnect"></param>
        public void RegeneratePowernet(Powernet toconnect)
        {
            //This removes the device from things that will be powernet disconnected when dirty powernet is killed
            Parent.NodeList.Remove(this);

            Parent = toconnect;
            Parent.NodeList.Add(this);
            OnPowernetRegenerate?.Invoke(this, new PowernetEventArgs(Parent));
        }

        /// <summary>
        /// Triggers event telling power components we have exited any powernets
        /// </summary>
        public void DisconnectFromPowernet()
        {
            if (Parent == null)
            {
                return;
            }
            Parent.NodeList.Remove(this);
            OnPowernetDisconnect?.Invoke(this, new PowernetEventArgs(Parent));
            Parent = null;
        }
    }

    public class PowernetEventArgs : EventArgs
    {
        public PowernetEventArgs(Powernet powernet)
        {
            Powernet = powernet;
        }

        public Powernet Powernet { get; }
    }
}
