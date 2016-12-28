using Ghost.Core.Utilities;

namespace Ghost.Data.Entities.Mappings
{
    public class UserAccountMap : BaseEntityMap<UserAccount>
    {
        public UserAccountMap() 
            : base("accounts")
        {
            Map(x => x.Access, "access").Not.Nullable().Default("1").CustomType<AccessLevel>();
            Map(x => x.Email, "email").Not.Nullable().Unique().Length(64);
            Map(x => x.LastLogin, "last_login");
            Map(x => x.Name, "name").Not.Nullable().Unique().Length(32);
            Map(x => x.PHash, "phash").Not.Nullable().Length(32);
            Map(x => x.Session, "session").Length(64);
        }
    }
}