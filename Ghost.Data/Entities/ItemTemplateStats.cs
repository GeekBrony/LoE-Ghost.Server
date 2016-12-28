using Ghost.Data.Utilities;

namespace Ghost.Data.Entities
{
    public class ItemTemplateStats : BaseEntity
    {
        public virtual ItemTemplate Item
        {
            get;
            set;
        }

        public virtual Stats Stat
        {

            get;
            set;

        }

        public virtual int Value
        {

            get;
            set;

        }
    }
}