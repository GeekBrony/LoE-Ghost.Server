using Ghost.Data.Utilities;

namespace Ghost.Data.Entities.Mappings
{
    public class ConditionTemplateMap : BaseEntityMap<ConditionTemplate>
    {
        public ConditionTemplateMap()
            : base("condition_templates")
        {
            Map(x => x.Type, "type").CustomType<ConditionType>();
            Map(x => x.Operand01, "operand_01")
                .Not.Nullable().Default("-1");
            Map(x => x.Operand02, "operand_02")
                .Not.Nullable().Default("-1");
            Map(x => x.Operand03, "operand_03")
                .Not.Nullable().Default("-1");
            Map(x => x.Operand04, "operand_04")
                .Not.Nullable().Default("-1");
            References(x => x.Script, "script_id");
        }
    }
}