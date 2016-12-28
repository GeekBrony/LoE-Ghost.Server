namespace Ghost.Data.Entities.Mappings
{
    public class UserCharacterMap : BaseEntityMap<UserCharacter>
    {
        public UserCharacterMap() 
            : base("account_characters")
        {
            Map(x => x.Level, "level").Not.Nullable().Default("1");
            References(x => x.User, "user_id").Not.Nullable();
        }
    }
}