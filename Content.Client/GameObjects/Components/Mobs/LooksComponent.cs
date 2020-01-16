using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Appearance;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class LooksComponent : SharedLooksComponent
    {
        public override HumanoidCharacterAppearance Appearance
        {
            get => base.Appearance;
            set
            {
                base.Appearance = value;
                UpdateLooks();
            }
        }

        public override Sex Sex
        {
            get => base.Sex;
            set
            {
                base.Sex = value;
                UpdateLooks();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateLooks();
        }

        private void UpdateLooks()
        {
            var sprite = Owner.GetComponent<SpriteComponent>();

            sprite.LayerSetColor(HumanoidVisualLayers.Hair, Appearance.HairColor);
            sprite.LayerSetColor(HumanoidVisualLayers.FacialHair, Appearance.FacialHairColor);

            sprite.LayerSetState(HumanoidVisualLayers.Body, Sex == Sex.Male ? "male" : "female");

            var hairStyle = Appearance.HairStyleName;
            if (string.IsNullOrWhiteSpace(hairStyle) || !HairStyles.HairStylesMap.ContainsKey(hairStyle))
                hairStyle = HairStyles.DefaultHairStyle;
            sprite.LayerSetState(HumanoidVisualLayers.Hair,
                HairStyles.HairStylesMap[hairStyle]);

            var facialHairStyle = Appearance.FacialHairStyleName;
            if (string.IsNullOrWhiteSpace(facialHairStyle) || !HairStyles.FacialHairStylesMap.ContainsKey(facialHairStyle))
                facialHairStyle = HairStyles.DefaultFacialHairStyle;
            sprite.LayerSetState(HumanoidVisualLayers.FacialHair,
                HairStyles.FacialHairStylesMap[facialHairStyle]);
        }
    }
}
