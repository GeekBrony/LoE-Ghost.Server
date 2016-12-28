using Ghost.Data.Utilities;

namespace Ghost.Data.Entities.Mappings
{
    class UserCharacterVisualMap : BaseEntityMap<UserCharacterVisual>
    {
        public UserCharacterVisualMap() 
            : base("account_character_visual")
        {
            Map(x => x.Name, "name")
                .Not.Nullable().Unique();
            Map(x => x.Race, "race")
                .Not.Nullable().Default("0");
            Map(x => x.Gender, "gender")
                .Not.Nullable().Default("0").CustomType<Gender>();
            Map(x => x.Eye, "eye")
                .Not.Nullable().Default("0");
            Map(x => x.EyeColor, "eye_color")
                .Not.Nullable().Default("0");
            Map(x => x.Mane, "mane")
                .Not.Nullable().Default("0");
            Map(x => x.Tail, "tail")
                .Not.Nullable().Default("0");
            Map(x => x.Hoof, "hoof")
                .Not.Nullable().Default("0");
            Map(x => x.HornSize, "hoof_size")
                .Not.Nullable().Default("0");
            Map(x => x.HoofColor, "hoof_color")
                .Not.Nullable().Default("0");
            Map(x => x.BodySize, "body_size")
                .Not.Nullable().Default("0");
            Map(x => x.BodyColor, "body_color")
                .Not.Nullable().Default("0");
            Map(x => x.CutieMark0, "cutie_mark_01")
                .Not.Nullable().Default("0");
            Map(x => x.CutieMark1, "cutie_mark_02")
                .Not.Nullable().Default("0");
            Map(x => x.CutieMark2, "cutie_mark_03")
                .Not.Nullable().Default("0");
            Map(x => x.HairColor0, "hair_color_01")
                .Not.Nullable().Default("0");
            Map(x => x.HairColor1, "hair_color_02")
                .Not.Nullable().Default("0");
            Map(x => x.HairColor2, "hair_color_03")
                .Not.Nullable().Default("0");
        }
    }
}