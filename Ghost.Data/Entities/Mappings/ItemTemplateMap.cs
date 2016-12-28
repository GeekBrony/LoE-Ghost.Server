using Ghost.Data.Utilities;

namespace Ghost.Data.Entities.Mappings
{
    public class ItemTemplateMap : BaseEntityMap<ItemTemplate>
    {
        public ItemTemplateMap()
            : base("item_templates")
        {
            Map(x => x.Name, "name");
            Map(x => x.Price, "price")
                .Not.Nullable().Default("0");
            Map(x => x.Stack, "stack")
                .Not.Nullable().Default("1");
            Map(x => x.Level, "level")
                .Not.Nullable().Default("0");
            Map(x => x.Color, "color")
                .Not.Nullable().Default("4294967295");
            Map(x => x.Sockets, "sockets")
                .Not.Nullable().Default("0");
            Map(x => x.Flags, "flags").CustomType<ItemFlags>()
                .Not.Nullable().Default("0");
            Map(x => x.RequiredLevel, "required_level")
                .Not.Nullable().Default("0");
            Map(x => x.Slots, "slots").CustomType<WearableSlot>()
                .Not.Nullable().Default("0");
        }
    }
}