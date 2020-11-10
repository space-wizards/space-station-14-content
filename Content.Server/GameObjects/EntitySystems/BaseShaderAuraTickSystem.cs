﻿using Content.Server.GameObjects.Components.Effects;
using Content.Server.GameObjects.Components.Kitchen;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BaseShaderAuraTickSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var shaderAura in ComponentManager.EntityQuery<BaseShaderAuraComponent>())
            {
                shaderAura.OnTick();
            }
            foreach (var shaderAura in ComponentManager.EntityQuery<SingularityShaderAuraComponent>()) //TODO: replace this with a way to get the children of a class in EntityQuery
            {
                shaderAura.OnTick();
            }
        }
    }
}
