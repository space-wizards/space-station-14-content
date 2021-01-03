using System.Linq;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Atmos;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping.Scrubbers
{
    /// <summary>
    ///     Transfers gas from the tile it is on to a <see cref="PipeNode"/>.
    /// </summary>
    public abstract class BaseSiphonComponent : Component, IPipeNetUpdated
    {

        [ViewVariables]
        private PipeNode _scrubberOutlet;

        private AtmosphereSystem _atmosSystem;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool SiphonEnabled
        {
            get => _siphonEnabled;
            set
            {
                _siphonEnabled = value;
                UpdateAppearance();
            }
        }
        private bool _siphonEnabled = true;

        private AppearanceComponent _appearance;

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PipeNetDeviceComponent>();
            _atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            if (!Owner.TryGetComponent<NodeContainerComponent>(out var container))
            {
                //TODO: must stop updating
                Logger.Error($"{typeof(BaseSiphonComponent)} on entity {Owner.Uid} did not have a {nameof(NodeContainerComponent)}.");
                return;
            }
            _scrubberOutlet = container.Nodes.OfType<PipeNode>().FirstOrDefault();
            if (_scrubberOutlet == null)
            {
                //TODO: must stop updating
                Logger.Error($"{typeof(BaseSiphonComponent)} on entity {Owner.Uid} could not find compatible {nameof(PipeNode)}s on its {nameof(NodeContainerComponent)}.");
                return;
            }
            Owner.TryGetComponent(out _appearance);
            UpdateAppearance();
        }

        public void Update(PipeNetUpdateMessage message)
        {
            if (!SiphonEnabled)
                return;

            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere(Owner.EntityManager);
            if (tileAtmos == null)
                return;
            ScrubGas(tileAtmos.Air, _scrubberOutlet.Air);
            tileAtmos.Invalidate();
        }

        protected abstract void ScrubGas(GasMixture inletGas, GasMixture outletGas);

        private void UpdateAppearance()
        {
            _appearance?.SetData(SiphonVisuals.VisualState, new SiphonVisualState(SiphonEnabled));
        }
    }
}
