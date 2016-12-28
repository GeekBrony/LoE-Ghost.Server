namespace Ghost.Data.Entities
{
    public class LootTemplate : BaseEntity
    {
        public virtual int MinBits
        {
            get;
            set;
        }

        public virtual int MaxBits
        {
            get;
            set;
        }

        public virtual int MinItems
        {
            get;
            set;
        }

        public virtual int MaxItems
        {
            get;
            set;
        }

        public virtual int MinSlots
        {
            get;
            set;
        }

        public virtual int MaxSlots
        {
            get;
            set;
        }
    }
}