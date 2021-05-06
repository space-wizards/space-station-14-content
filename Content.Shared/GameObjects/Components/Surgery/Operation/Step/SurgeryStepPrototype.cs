using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Step
{
    [Prototype("surgeryStep")]
    public class SurgeryStepPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        public string LocId => ID.ToLowerInvariant();

        public static string SurgeonBeginPopup(IEntity user, IEntity? target, IEntity part, string id)
        {
            if (target == null)
            {
                var locId = $"surgery-step-{id}-begin-no-zone-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"surgery-step-{id}-begin-self-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-begin-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }

        public static string TargetBeginPopup(IEntity user, IEntity part, string id)
        {
            var locId = $"surgery-step-{id}-begin-target-popup";
            return Loc.GetString(locId, ("user", user), ("part", part));
        }

        public static string OutsiderBeginPopup(IEntity user, IEntity? target, IEntity part, string id)
        {
            if (target == null)
            {
                var locId = $"surgery-step-{id}-begin-no-zone-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"surgery-step-{id}-begin-self-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-begin-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }

        public static string SurgeonSuccessPopup(IEntity user, IEntity? target, IEntity part, string id)
        {
            if (target == null)
            {
                var locId = $"surgery-step-{id}-success-no-zone-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"surgery-step-{id}-success-self-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-success-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }

        public static string TargetSuccessPopup(IEntity user, IEntity part, string id)
        {
            var locId = $"surgery-step-{id}-success-target-popup";
            return Loc.GetString(locId, ("user", user), ("part", part));
        }

        public static string OutsiderSuccessPopup(IEntity user, IEntity? target, IEntity part, string id)
        {
            if (target == null)
            {
                var locId = $"surgery-step-{id}-success-no-zone-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"surgery-step-{id}-success-self-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-success-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }

        public string SurgeonBeginPopup(IEntity user, IEntity? target, IEntity part)
        {
            return SurgeonBeginPopup(user, target, part, LocId);
        }

        public string TargetBeginPopup(IEntity user, IEntity part)
        {
            return TargetBeginPopup(user, part, LocId);
        }

        public string OutsiderBeginPopup(IEntity user, IEntity? target, IEntity part)
        {
            return OutsiderBeginPopup(user, target, part, LocId);
        }

        public string SurgeonSuccessPopup(IEntity user, IEntity? target, IEntity part)
        {
            return SurgeonSuccessPopup(user, target, part, LocId);
        }

        public string TargetSuccessPopup(IEntity user, IEntity part)
        {
            return TargetSuccessPopup(user, part, LocId);
        }

        public string OutsiderSuccessPopup(IEntity user, IEntity? target, IEntity part)
        {
            return OutsiderSuccessPopup(user, target, part, LocId);
        }
    }
}
