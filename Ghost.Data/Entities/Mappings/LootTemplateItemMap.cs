namespace Ghost.Data.Entities.Mappings
{
    public class LootTemplateItemMap : BaseEntityMap<LootTemplateItem>
    {
        public LootTemplateItemMap()
            : base("loot_template_items")
        {
            Map(x => x.Chance, "chance")
                .Not.Nullable().Default("0");
            Map(x => x.MinCount, "min_count")
                .Not.Nullable().Default("0");
            Map(x => x.MaxCount, "max_count")
                .Not.Nullable().Default("0");
            References(x => x.Item, "item_id")
                .Not.Nullable();
            References(x => x.Condition, "condition_id");
        }
    }
}