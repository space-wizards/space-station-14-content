﻿using System;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds
{
    [Flags, FlagsFor(typeof(ActsFlags))]
    [Serializable]
    public enum ThresholdActs
    {
        None = 0,
        Breakage,
        Destruction
    }
}
