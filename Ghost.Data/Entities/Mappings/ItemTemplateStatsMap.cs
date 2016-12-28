using Ghost.Data.Utilities;

namespace Ghost.Data.Entities.Mappings
{
    public class ItemTemplateStatsMap : BaseEntityMap<ItemTemplateStats>
    {
        public ItemTemplateStatsMap()
            : base("item_template_stats")
        {
            Map(x => x.Stat, "stat").CustomType<Stats>()
                .Not.Nullable().Default("0");
            Map(x => x.Value, "value")
                .Not.Nullable().Default("0");
            References(x => x.Item, "item_id")
                .Not.Nullable();
        }
    }
}