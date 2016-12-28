namespace Ghost.Data.Entities
{
    public class LootTemplateItem : BaseEntity
    {
        public virtual float Chance
        {
            get;
            set;
        }

        public virtual int MinCount
        {
            get;
            set;
        }

        public virtual int MaxCount
        {
            get;
            set;
        }

        public virtual ItemTemplate Item
        {
            get;
            set;
        }

        public virtual ConditionTemplate Condition
        {
            get;
            set;
        }
    }
}