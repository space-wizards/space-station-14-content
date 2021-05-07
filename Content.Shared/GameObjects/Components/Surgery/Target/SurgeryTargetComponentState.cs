﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Surgery.Target
{
    [Serializable, NetSerializable]
    public class SurgeryTargetComponentState : ComponentState
    {
        public SurgeryTargetComponentState(EntityUid? surgeon, string? operation) : base(ContentNetIDs.SURGERY_TARGET)
        {
            Surgeon = surgeon;
            Operation = operation;
        }

        public EntityUid? Surgeon { get; }

        public string? Operation { get; }
    }
}
