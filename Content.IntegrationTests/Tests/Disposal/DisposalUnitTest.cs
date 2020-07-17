﻿using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Disposal;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Disposal
{
    [TestFixture]
    [TestOf(typeof(DisposableComponent))]
    [TestOf(typeof(DisposalEntryComponent))]
    [TestOf(typeof(DisposalUnitComponent))]
    public class DisposalUnitTest : ContentIntegrationTest
    {
        private void UnitInsert(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(unit.CanInsert(entity), Is.EqualTo(result));
                Assert.That(unit.TryInsert(entity), Is.EqualTo(result));

                if (result)
                {
                    Assert.True(entity.TryGetComponent(out DisposableComponent disposable));

                    // Not in a tube yet
                    Assert.False(disposable.InTube);
                    Assert.Null(disposable.PreviousTube);
                    Assert.Null(disposable.CurrentTube);
                    Assert.Null(disposable.NextTube);
                }
            }
        }

        private void UnitContains(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(unit.ContainedEntities.Contains(entity), Is.EqualTo(result));
            }
        }

        private void UnitInsertContains(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            UnitInsert(unit, result, entities);
            UnitContains(unit, result, entities);
        }

        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                var human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                var wrench = entityManager.SpawnEntity("Wrench", MapCoordinates.Nullspace);
                var disposalUnit = entityManager.SpawnEntity("DisposalUnit", MapCoordinates.Nullspace);
                var disposalTrunk = entityManager.SpawnEntity("DisposalTrunk", MapCoordinates.Nullspace);

                // Test for components existing
                Assert.True(human.TryGetComponent(out DisposableComponent _));
                Assert.True(wrench.TryGetComponent(out DisposableComponent _));
                Assert.True(disposalUnit.TryGetComponent(out DisposalUnitComponent unit));
                Assert.True(disposalTrunk.TryGetComponent(out DisposalEntryComponent _));

                // Can't insert, unanchored and unpowered
                UnitInsertContains(unit, false, human, wrench, disposalUnit, disposalTrunk);
                Assert.False(unit.Anchored);

                // Anchor the disposal unit
                Assert.True(disposalUnit.TryGetComponent(out AnchorableComponent anchorableUnit));
                Assert.True(anchorableUnit.TryAnchor(human, wrench));
                Assert.True(unit.Anchored);

                // Can't insert, unpowered
                UnitInsertContains(unit, false, human, wrench, disposalUnit, disposalTrunk);

                Assert.False(unit.Powered);

                Assert.True(disposalUnit.TryGetComponent(out PowerReceiverComponent power));

                power.NeedsPower = false;

                Assert.True(unit.Powered);

                // Can't insert the trunk or the unit into itself
                UnitInsertContains(unit, false, disposalUnit, disposalTrunk);

                // Can insert mobs and items
                UnitInsertContains(unit, true, human, wrench);
            });

            await server.WaitIdleAsync();
        }
    }
}
