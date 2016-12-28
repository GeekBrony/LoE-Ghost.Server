namespace Ghost.Data.Entities
{
    public class LootTemplateReference : BaseEntity
    {
        public virtual float Chance
        {
            get;
            set;
        }

        public virtual LootTemplate Loot
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