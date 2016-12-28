namespace Ghost.Data.Entities.Mappings
{
    public class LootTemplateMap : BaseEntityMap<LootTemplate>
    {
        public LootTemplateMap()
            : base("loot_templates")
        {
            Map(x => x.MinBits, "min_bits").Not.Nullable().Default("0");
            Map(x => x.MaxBits, "max_bits").Not.Nullable().Default("0");
            Map(x => x.MinSlots, "min_slots").Not.Nullable().Default("0");
            Map(x => x.MaxSlots, "max_slots").Not.Nullable().Default("0");
            Map(x => x.MinItems, "min_items").Not.Nullable().Default("0");
            Map(x => x.MaxItems, "max_items").Not.Nullable().Default("0");
        }
    }
}