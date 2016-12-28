using System;
using Ghost.Network;

namespace Ghost.Data.Entities
{
    public class UserCharacter : BaseEntity, INetSerializable
    {
 
        public virtual short Level
        {
            get;
            set;
        }

        public virtual UserAccount User
        {
            get;
            set;
        }

        public virtual UserCharacterVisual Visual
        {
            get;
            set;
        }

        public virtual int AllocSize
        {
            get
            {
                return (Visual?.AllocSize ?? 0) + 6;
            }
        }

        public virtual void OnDeserialize(INetMessage message)
        {
            Visual.OnDeserialize(message);
            //Level = message.ReadInt16();
            Id = message.ReadInt32();
        }

        public virtual void OnSerialize(INetMessage message)
        {
            Visual.OnSerialize(message);
            //message.Write(Level);
            message.Write(Id);
        }
    }
}