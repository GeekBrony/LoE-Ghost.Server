namespace Ghost.Data.Entities
{
    public class BaseEntity : IEntity
    {
        public virtual int Id
        {
            get;
            set;
        }
    }
}