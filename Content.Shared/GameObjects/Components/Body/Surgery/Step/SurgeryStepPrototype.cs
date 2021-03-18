using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Step
{
    [Prototype("surgeryStep")]
    public class SurgeryStepPrototype : IPrototype
    {
        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        public string SurgeonBeginPopup(IEntity user, IEntity? target, IEntity part)
        {
            var id = ID.ToLowerInvariant();

            if (target == null)
            {
                var locId = $"surgery-step-{id}-begin-no-target-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"surgery-step-{id}-begin-surgeon-self-popup";
                return Loc.GetString(locId, ("part", part));
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
                var locId = $"surgery-step-{id}-begin-no-target-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else if (user == target)
            {
                var locId = $"surgery-step-{id}-begin-outsider-self-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else
            {
                var locId = $"surgery-step-{id}-begin-outsider-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }

        public string SurgeonSuccessPopup(IEntity user, IEntity? target, IEntity part)
        {
            if (target == null)
            {
                var locId = $"step-surgery-{ID.ToLowerInvariant()}-no-target-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("part", part));
            }
            else
            {
                var locId = $"step-surgery-{ID.ToLowerInvariant()}-surgeon-popup";
                return Loc.GetString(locId, ("user", user), ("target", target), ("part", part));
            }
        }
    }
}
