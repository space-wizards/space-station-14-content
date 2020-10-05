﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public abstract class SharedBodyPartComponent : Component, IBodyPart, ICanExamine, IShowContextMenu
    {
        public override string Name => "BodyPart";

        public override uint? NetID => ContentNetIDs.BODY_PART;

        private IBody? _body;

        // TODO BODY Remove
        private List<string> _mechanismIds = new List<string>();
        public IReadOnlyList<string> MechanismIds => _mechanismIds;

        [ViewVariables]
        private HashSet<IMechanism> _mechanisms = new HashSet<IMechanism>();

        [ViewVariables]
        public IBody? Body
        {
            get => _body;
            set
            {
                if (_body == value)
                {
                    return;
                }

                var old = _body;
                _body = value;

                if (value != null)
                {
                    foreach (var mechanism in _mechanisms)
                    {
                        mechanism.OnBodyAdd(old, value);
                    }
                }
                else if (old != null)
                {
                    foreach (var mechanism in _mechanisms)
                    {
                        mechanism.OnBodyRemove(old);
                    }
                }
            }
        }

        [ViewVariables] public BodyPartType PartType { get; private set; }

        [ViewVariables] public string Plural { get; private set; } = string.Empty;

        [ViewVariables] public int Size { get; private set; }

        [ViewVariables] public int SizeUsed { get; private set; }

        // TODO BODY size used
        // TODO BODY surgerydata
        // TODO BODY properties

        /// <summary>
        ///     What types of BodyParts this <see cref="IBodyPart"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; private set; }

        /// <summary>
        ///     Set of all <see cref="IMechanism"/> currently inside this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public IReadOnlyCollection<IMechanism> Mechanisms => _mechanisms;

        // TODO BODY Replace with a simulation of organs
        /// <summary>
        ///     Represents if body part is vital for creature.
        ///     If the last vital body part is removed creature dies
        /// </summary>
        [ViewVariables]
        public bool IsVital { get; private set; }

        // TODO BODY
        [ViewVariables]
        public SurgeryDataComponent? SurgeryDataComponent => Owner.GetComponentOrNull<SurgeryDataComponent>();

        protected virtual void OnAddMechanism(IMechanism mechanism)
        {
            var prototypeId = mechanism.Owner.Prototype!.ID;

            if (!_mechanismIds.Contains(prototypeId))
            {
                _mechanismIds.Add(prototypeId);
            }

            mechanism.Part = this;
            SizeUsed += mechanism.Size;

            Dirty();
        }

        protected virtual void OnRemoveMechanism(IMechanism mechanism)
        {
            _mechanismIds.Remove(mechanism.Owner.Prototype!.ID);
            mechanism.Part = null;
            SizeUsed -= mechanism.Size;

            Dirty();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO BODY serialize any changed properties?

            serializer.DataField(this, b => b.PartType, "partType", BodyPartType.Other);

            serializer.DataField(this, b => b.Plural, "plural", string.Empty);

            serializer.DataField(this, b => b.Size, "size", 1);

            serializer.DataField(this, b => b.Compatibility, "compatibility", BodyPartCompatibility.Universal);

            serializer.DataField(this, m => m.IsVital, "vital", false);

            serializer.DataField(ref _mechanismIds, "mechanisms", new List<string>());
        }

        public override ComponentState GetComponentState()
        {
            var mechanismIds = new EntityUid[_mechanisms.Count];

            var i = 0;
            foreach (var mechanism in _mechanisms)
            {
                mechanismIds[i] = mechanism.Owner.Uid;
                i++;
            }

            return new BodyPartComponentState(mechanismIds);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is BodyPartComponentState state))
            {
                return;
            }

            var newMechanisms = state.Mechanisms();

            foreach (var mechanism in _mechanisms.ToArray())
            {
                if (!newMechanisms.Contains(mechanism))
                {
                    RemoveMechanism(mechanism);
                }
            }

            foreach (var mechanism in newMechanisms)
            {
                if (!_mechanisms.Contains(mechanism))
                {
                    TryAddMechanism(mechanism, true);
                }
            }
        }

        public bool Drop()
        {
            Body = null;
            Owner.AttachToGrandparent();
            return true;
        }

        public bool SurgeryCheck(SurgeryType surgery)
        {
            return SurgeryDataComponent?.CheckSurgery(surgery) ?? false;
        }

        /// <summary>
        ///     Attempts to perform surgery on this <see cref="IBodyPart"/> with the given
        ///     tool.
        /// </summary>
        /// <returns>True if successful, false if there was an error.</returns>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, IEntity performer)
        {
            DebugTools.AssertNotNull(toolType);
            DebugTools.AssertNotNull(target);
            DebugTools.AssertNotNull(surgeon);
            DebugTools.AssertNotNull(performer);

            return SurgeryDataComponent?.PerformSurgery(toolType, target, surgeon, performer) ?? false;
        }

        public bool CanAttachPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            return SurgeryDataComponent?.CanAttachBodyPart(part) ?? false;
        }

        public bool CanAddMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            return SurgeryDataComponent != null &&
                   SizeUsed + mechanism.Size <= Size &&
                   SurgeryDataComponent.CanAddMechanism(mechanism);
        }

        /// <summary>
        ///     Tries to add a mechanism onto this body part.
        /// </summary>
        /// <param name="mechanism">The mechanism to try to add.</param>
        /// <param name="force">
        ///     Whether or not to check if the mechanism can be added.
        /// </param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="IBodyPart"/>).
        ///     Will return false even when forced if the mechanism is already
        ///     added in this <see cref="IBodyPart"/>.
        /// </returns>
        public bool TryAddMechanism(IMechanism mechanism, bool force = false)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!force && !CanAddMechanism(mechanism))
            {
                return false;
            }

            if (!_mechanisms.Add(mechanism))
            {
                return false;
            }

            OnAddMechanism(mechanism);

            return true;
        }

        public bool RemoveMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!_mechanisms.Remove(mechanism))
            {
                return false;
            }

            return true;
        }

        public bool RemoveMechanism(IMechanism mechanism, EntityCoordinates coordinates)
        {
            if (RemoveMechanism(mechanism))
            {
                mechanism.Owner.Transform.Coordinates = coordinates;
                return true;
            }

            return false;
        }

        public bool DeleteMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!RemoveMechanism(mechanism))
            {
                return false;
            }

            mechanism.Owner.Delete();
            return true;
        }

        public bool ShowContextMenu(IEntity examiner)
        {
            return Body == null;
        }

        public bool CanExamine(IEntity entity)
        {
            return Body == null;
        }
    }

    [Serializable, NetSerializable]
    public class BodyPartComponentState : ComponentState
    {
        private List<IMechanism>? _mechanisms;

        public readonly EntityUid[] MechanismIds;

        public BodyPartComponentState(EntityUid[] mechanismIds) : base(ContentNetIDs.BODY_PART)
        {
            MechanismIds = mechanismIds;
        }

        public List<IMechanism> Mechanisms(IEntityManager? entityManager = null)
        {
            if (_mechanisms != null)
            {
                return _mechanisms;
            }

            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var mechanisms = new List<IMechanism>(MechanismIds.Length);

            foreach (var id in MechanismIds)
            {
                if (!entityManager.TryGetEntity(id, out var entity))
                {
                    continue;
                }

                if (!entity.TryGetComponent(out IMechanism? mechanism))
                {
                    continue;
                }

                mechanisms.Add(mechanism);
            }

            return _mechanisms = mechanisms;
        }
    }
}
