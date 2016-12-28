using Ghost.Data.Utilities;

namespace Ghost.Data.Entities
{
    public class ConditionTemplate : BaseEntity
    {
        public virtual ConditionType Type
        {
            get;
            set;
        }

        public virtual int Operand01
        {
            get;
            set;
        }

        public virtual int Operand02
        {
            get;
            set;
        }

        public virtual int Operand03
        {
            get;
            set;
        }

        public virtual int Operand04
        {
            get;
            set;
        }

        public virtual ScriptTemlate Script
        {
            get;
            set;
        }
    }
}