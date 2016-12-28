namespace Ghost.Data.Entities.Mappings
{
    public class LootTemplateReferenceMap : BaseEntityMap<LootTemplateReference>
    {
        public LootTemplateReferenceMap()
            : base("loot_template_references")
        {
            Map(x => x.Chance, "chance").Not.Nullable().Default("0");
            References(x => x.Loot, "loot_id").Not.Nullable();
            References(x => x.Condition, "condition_id");
        }
    }
}