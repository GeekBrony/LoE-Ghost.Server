using Ghost.Core.Utilities;
using System;

namespace Ghost.Data.Entities
{
    public class UserAccount : BaseEntity
    {
        public virtual string Name
        {
            get;
            set;
        }

        public virtual string Email
        {
            get;
            set;
        }

        public virtual string PHash
        {
            get;
            set;
        }

        public virtual string Session
        {
            get;
            set;
        }

        public virtual AccessLevel Access
        {
            get;
            set;
        }

        public virtual DateTime? LastLogin
        {
            get;
            set;
        }
    }
}