using Ghost.Data.Utilities;

namespace Ghost.Data.Entities
{
    public class ItemTemplate : BaseEntity
    {
        public virtual int Price
        {
            get;
            set;
        }

        public virtual int Stack
        {
            get;
            set;
        }

        public virtual int Level
        {
            get;
            set;
        }

        public virtual uint Color
        {
            get;
            set;
        }

        public virtual string Name
        {
            get;
            set;
        }

        public virtual byte Sockets
        {
            get;
            set;
        }

        public virtual ItemFlags Flags
        {
            get;
            set;
        }

        public virtual int RequiredLevel
        {
            get;
            set;
        }

        public virtual WearableSlot Slots
        {
            get;
            set;
        }
    }
}