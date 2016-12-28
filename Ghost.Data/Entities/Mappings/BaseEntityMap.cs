using FluentNHibernate.Mapping;

namespace Ghost.Data.Entities.Mappings
{
    public abstract class BaseEntityMap<T> : ClassMap<T>
        where T : BaseEntity
    {
        public BaseEntityMap(string tableName)
        {
            Table("loe_" + tableName);
            Id(x => x.Id, "id");
        }
    }
}