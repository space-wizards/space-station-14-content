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

        public string SurgeonBeginPopup(IEntity user, IEntity? target, IEntity part)
        {
            var id = ID.ToLowerInvariant();

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

        public string TargetBeginPopup(IEntity user, IEntity part)
        {
            var id = ID.ToLowerInvariant();
            var locId = $"surgery-step-{id}-begin-target-popup";
            return Loc.GetString(locId, ("user", user), ("part", part));
        }

        public string OutsiderBeginPopup(IEntity user, IEntity? target, IEntity part)
        {
            var id = ID.ToLowerInvariant();

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

        public string SurgeonSuccessPopup(IEntity user, IEntity? target, IEntity part)
        {
            var id = ID.ToLowerInvariant();

            if (target == null)
            {
                var locId = $"step-surgery-{id}-success-no-zone-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"step-surgery-{id}-success-self-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
            else
            {
                var locId = $"step-surgery-{id}-success-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }

        public string TargetSuccessPopup(IEntity user, IEntity part)
        {
            var id = ID.ToLowerInvariant();
            var locId = $"surgery-step-{id}-success-target-popup";
            return Loc.GetString(locId, ("user", user), ("part", part));
        }

        public string OutsiderSuccessPopup(IEntity user, IEntity? target, IEntity part)
        {
            var id = ID.ToLowerInvariant();

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
    }
}
