#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Surgery
{
    public class SurgerySystem : SharedSurgerySystem, IResettingEntitySystem
    {
        private readonly Dictionary<IEntity, SurgeonData> _surgeonParts = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntityDeletedMessage>(HandleEntityDeleted);
        }

        private void HandleEntityDeleted(EntityDeletedMessage message)
        {
            _surgeonParts.Remove(message.Entity);
        }

        public void Reset()
        {
            _surgeonParts.Clear();
        }

        public SurgeonData? GetSurgeon(IEntity surgeon)
        {
            return _surgeonParts.GetValueOrDefault(surgeon);
        }

        public bool HasSurgeon(IEntity surgeon)
        {
            return _surgeonParts.ContainsKey(surgeon);
        }

        public bool TryGetSurgeon(IEntity surgeon, [NotNullWhen(true)] out SurgeonData? part)
        {
            return (part = GetSurgeon(surgeon)) != null;
        }

        public void SetSurgeon(IEntity surgeon, SurgeonData part)
        {
            _surgeonParts[surgeon] = part;
        }

        public bool RemoveSurgeon(IEntity surgeon, SurgeonData data)
        {
            if (_surgeonParts.TryGetValue(surgeon, out var old) &&
                old == data)
            {
                _surgeonParts.Remove(surgeon);
            }

            return true;
        }
    }
}
