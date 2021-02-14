#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public class SurgerySystem : SharedSurgerySystem, IResettingEntitySystem
    {
        private readonly Dictionary<Mind, IBodyPart> PerformersParts = new();

        public void Reset()
        {
            PerformersParts.Clear();
        }

        public IBodyPart? GetPerformerPart(IEntity entity)
        {
            if (!entity.TryGetComponent(out MindComponent? mind) ||
                mind.Mind == null)
            {
                return null;
            }

            return PerformersParts.GetValueOrDefault(mind.Mind);
        }

        public bool TryGetPerformerPart(IEntity entity, [NotNullWhen(true)] out IBodyPart? part)
        {
            return (part = GetPerformerPart(entity)) != null;
        }

        public bool TrySetPerformer(IEntity performer, IBodyPart part)
        {
            if (!performer.TryGetComponent(out MindComponent? mind) ||
                mind.Mind == null)
            {
                return false;
            }

            PerformersParts[mind.Mind] = part;
            return true;
        }
    }
}
