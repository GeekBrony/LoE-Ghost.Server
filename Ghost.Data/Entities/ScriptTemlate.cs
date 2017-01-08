using Ghost.Data.Utilities;

namespace Ghost.Data.Entities
{
    public class ScriptTemlate : BaseEntity
    {
        public virtual ScriptType Type
        {
            get;
            set;
        }

        public virtual string Code
        {
            get;
            set;
        }

        public virtual string Comment
        {
            get;
            set;
        }
    }
}