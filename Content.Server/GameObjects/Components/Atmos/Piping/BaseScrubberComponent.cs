﻿using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     Transfers gas from the tile it is on to a <see cref="Pipe"/>.
    /// </summary>
    public abstract class BaseScrubberComponent : Component
    {
        [ViewVariables]
        private PipeNode _scrubberOutlet;

        public override void Initialize()
        {
            base.Initialize();
            _scrubberOutlet = Owner.GetComponent<NodeContainerComponent>().Nodes.OfType<PipeNode>().First();
        }

        public void Update(float frameTime)
        {
            var gridPosition = Owner.Transform.GridPosition;
            var gridAtmos = EntitySystem.Get<AtmosphereSystem>()
                .GetGridAtmosphere(gridPosition.GridID);
            if (gridAtmos == null)
                return;
            var tile = gridAtmos.GetTile(gridPosition);
            if (tile == null)
                return;
            ScrubGas(tile.Air, _scrubberOutlet.Air, frameTime);
        }

        protected abstract void ScrubGas(GasMixture inletGas, GasMixture outletGas, float frameTime);
    }
}
