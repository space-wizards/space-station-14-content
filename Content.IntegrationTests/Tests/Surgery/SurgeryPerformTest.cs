using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Surgery.Tool;
using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Operation.Effect;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.IntegrationTests.Tests.Surgery.SurgeryPrototypes;

namespace Content.IntegrationTests.Tests.Surgery
{
    [TestFixture]
    [TestOf(typeof(SharedSurgerySystem))]
    [TestOf(typeof(SurgeonComponent))]
    [TestOf(typeof(SurgeryTargetComponent))]
    [TestOf(typeof(SurgeryDrapesComponent))]
    public class SurgeryPerformTest : ContentIntegrationTest
    {
        private void AssertValidSteps(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryToolComponent correct,
            params SurgeryToolComponent[] all)
        {
            foreach (var tool in all)
            {
                if (tool == correct)
                {
                    continue;
                }

                Assert.False(tool.Behavior!.CanPerform(surgeon, target));
                Assert.False(tool.Behavior!.Perform(surgeon, target));
            }

            Assert.True(correct.Behavior!.CanPerform(surgeon, target));
            Assert.True(correct.Behavior!.Perform(surgeon, target));
        }

        [Test]
        public async Task PerformAmputationTest()
        {
            var (_, server) = await StartConnectedServerClientPair(
                new ClientContentIntegrationOption {ExtraPrototypes = Prototypes},
                new ServerContentIntegrationOption {ExtraPrototypes = Prototypes});

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            SurgeonComponent sSurgeonComp = default!;

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            SurgeryTargetComponent sSurgeryTargetComp = default!;

            SurgeryDrapesComponent sDrapesComp = default!;

            SurgeryToolComponent sIncisionComp = default!;
            SurgeryToolComponent sVesselCompressionComp = default!;
            SurgeryToolComponent sRetractionComp = default!;
            SurgeryToolComponent sAmputationComp = default!;
            SurgeryToolComponent[] sAllToolComps = default!;

            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            SurgeryOperationPrototype sAmputationOperation = default!;

            await server.WaitPost(() =>
            {
                var sPlayer = sPlayerManager.GetAllPlayers().Single().AttachedEntity;
                sSurgeonComp = sPlayer!.EnsureComponent<SurgeonComponent>();

                var coordinates = sPlayer!.Transform.Coordinates;
                var sTarget = sEntityManager.SpawnEntity(null, coordinates);
                sSurgeryTargetComp = sTarget.EnsureComponent<SurgeryTargetComponent>();

                var sDrapes = sEntityManager.SpawnEntity(DrapesDummyId, coordinates);
                sDrapesComp = sDrapes.GetComponent<SurgeryDrapesComponent>();

                sIncisionComp = sEntityManager
                    .SpawnEntity(IncisionDummyId, coordinates)
                    .GetComponent<SurgeryToolComponent>();

                sVesselCompressionComp = sEntityManager
                    .SpawnEntity(VesselCompressionDummyId, coordinates)
                    .GetComponent<SurgeryToolComponent>();

                sRetractionComp = sEntityManager
                    .SpawnEntity(RetractionDummyId, coordinates)
                    .GetComponent<SurgeryToolComponent>();

                sAmputationComp = sEntityManager
                    .SpawnEntity(AmputationDummyId, coordinates)
                    .GetComponent<SurgeryToolComponent>();

                sAllToolComps = new[]
                {
                    sIncisionComp, sVesselCompressionComp, sRetractionComp, sAmputationComp
                };

                sAmputationOperation = sPrototypeManager.Index<SurgeryOperationPrototype>(TestAmputationOperationId);
            });

            await server.WaitAssertion(() =>
            {
                // Try to start an amputation operation, succeeds
                Assert.True(sDrapesComp.TryUse(sSurgeonComp, sSurgeryTargetComp, sAmputationOperation));

                // Try again, fails because an operation is already underway
                Assert.False(sDrapesComp.TryUse(sSurgeonComp, sSurgeryTargetComp, sAmputationOperation));

                // Incision goes first
                AssertValidSteps(sSurgeonComp, sSurgeryTargetComp, sIncisionComp, sAllToolComps);

                // Vessel compression succeeds
                AssertValidSteps(sSurgeonComp, sSurgeryTargetComp, sVesselCompressionComp, sAllToolComps);

                // Retraction succeeds
                AssertValidSteps(sSurgeonComp, sSurgeryTargetComp, sRetractionComp, sAllToolComps);

                // Amputation succeeds
                AssertValidSteps(sSurgeonComp, sSurgeryTargetComp, sAmputationComp, sAllToolComps);

                // Operation is complete
                Assert.True(sSurgeryTargetComp.Owner.GetComponent<TestAmputationComponent>().Amputated);
            });
        }

        [Test]
        public async Task CancelOperationTest()
        {
            var (_, server) = await StartConnectedServerClientPair(
                new ClientContentIntegrationOption {ExtraPrototypes = Prototypes},
                new ServerContentIntegrationOption {ExtraPrototypes = Prototypes});

            var sPlayerManager = server.ResolveDependency<IPlayerManager>();
            SurgeonComponent sSurgeonComp = default!;

            var sEntityManager = server.ResolveDependency<IEntityManager>();
            SurgeryTargetComponent sSurgeryTargetComp = default!;

            SurgeryDrapesComponent sDrapesComp = default!;

            SurgeryToolComponent sCauterizationComp = default!;

            var sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
            SurgeryOperationPrototype sAmputationOperation = default!;

            await server.WaitPost(() =>
            {
                var sPlayer = sPlayerManager.GetAllPlayers().Single().AttachedEntity;
                sSurgeonComp = sPlayer!.EnsureComponent<SurgeonComponent>();

                var coordinates = sPlayer!.Transform.Coordinates;
                var sTarget = sEntityManager.SpawnEntity(null, coordinates);
                sSurgeryTargetComp = sTarget.EnsureComponent<SurgeryTargetComponent>();

                var sDrapes = sEntityManager.SpawnEntity(DrapesDummyId, coordinates);
                sDrapesComp = sDrapes.GetComponent<SurgeryDrapesComponent>();

                sCauterizationComp = sEntityManager
                    .SpawnEntity(CauteryDummyId, coordinates)
                    .GetComponent<SurgeryToolComponent>();

                sAmputationOperation = sPrototypeManager.Index<SurgeryOperationPrototype>(TestAmputationOperationId);
            });

            await server.WaitAssertion(() =>
            {
                // No operation underway, cauterization fails
                Assert.False(sCauterizationComp.Behavior!.CanPerform(sSurgeonComp, sSurgeryTargetComp));
                Assert.False(sCauterizationComp.Behavior!.Perform(sSurgeonComp, sSurgeryTargetComp));

                // Try to start an amputation operation, succeeds
                Assert.True(sDrapesComp.TryUse(sSurgeonComp, sSurgeryTargetComp, sAmputationOperation));

                // Try again, fails because an operation is already underway
                Assert.False(sDrapesComp.TryUse(sSurgeonComp, sSurgeryTargetComp, sAmputationOperation));

                Assert.NotNull(sSurgeonComp.Target);
                Assert.NotNull(sSurgeonComp.SurgeryCancellation);

                // Operation underway, cauterization succeeds
                Assert.True(sCauterizationComp.Behavior.CanPerform(sSurgeonComp, sSurgeryTargetComp));
                Assert.True(sCauterizationComp.Behavior.Perform(sSurgeonComp, sSurgeryTargetComp));

                Assert.Null(sSurgeonComp.Target);
                Assert.Null(sSurgeonComp.Mechanism);
                Assert.Null(sSurgeonComp.SurgeryCancellation);
            });
        }
    }

    [RegisterComponent]
    public class TestAmputationComponent : Component
    {
        public override string Name => TestAmputationComponentId;

        public bool Amputated { get; set; }
    }

    [UsedImplicitly]
    public class TestAmputationEffect : IOperationEffect
    {
        public void Execute(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            target.Owner.EnsureComponent<TestAmputationComponent>().Amputated = true;
        }
    }
}
