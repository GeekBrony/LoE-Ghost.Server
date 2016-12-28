using Ghost.Data.Utilities;
using Ghost.Network;

namespace Ghost.Data.Entities
{
    public class UserCharacterVisual : BaseEntity, INetSerializable
    {
        public virtual short Eye
        {
            get;
            set;
        }

        public virtual byte Race
        {
            get;
            set;
        }

        public virtual short Mane
        {
            get;
            set;
        }

        public virtual short Tail
        {
            get;
            set;
        }

        public virtual short Hoof
        {
            get;
            set;
        }

        public virtual string Name
        {
            get;
            set;
        }

        public virtual int EyeColor
        {
            get;
            set;
        }

        public virtual Gender Gender
        {
            get;
            set;
        }

        public virtual int HoofColor
        {
            get;
            set;
        }

        public virtual int BodyColor
        {
            get;
            set;
        }

        public virtual int HairColor0
        {
            get;
            set;
        }

        public virtual int HairColor1
        {
            get;
            set;
        }

        public virtual int HairColor2
        {
            get;
            set;
        }

        public virtual float BodySize
        {
            get;
            set;
        }

        public virtual float HornSize
        {
            get;
            set;
        }

        public virtual int CutieMark0
        {
            get;
            set;
        }

        public virtual int CutieMark1
        {
            get;
            set;
        }

        public virtual int CutieMark2
        {
            get;
            set;
        }

        public virtual int AllocSize
        {
            get { return (Name?.Length ?? 0) * 2 + 48; }
        }

        public virtual void OnSerialize(INetMessage message)
        {
            //message.Write(Name);
            message.Write(Race);
            message.Write((byte)Gender);
            message.Write(CutieMark0);
            message.Write(CutieMark1);
            message.Write(CutieMark2);
            message.Write((byte)(HairColor0 & 0xFF)); message.Write((byte)((HairColor0 >> 8) & 0xFF)); message.Write((byte)((HairColor0 >> 16) & 0xFF));
            message.Write((byte)(HairColor1 & 0xFF)); message.Write((byte)((HairColor1 >> 8) & 0xFF)); message.Write((byte)((HairColor1 >> 16) & 0xFF));
            message.Write((byte)(HairColor2 & 0xFF)); message.Write((byte)((HairColor2 >> 8) & 0xFF)); message.Write((byte)((HairColor2 >> 16) & 0xFF));
            message.Write((byte)(BodyColor & 0xFF)); message.Write((byte)((BodyColor >> 8) & 0xFF)); message.Write((byte)((BodyColor >> 16) & 0xFF));
            message.Write((byte)(EyeColor & 0xFF)); message.Write((byte)((EyeColor >> 8) & 0xFF)); message.Write((byte)((EyeColor >> 16) & 0xFF));
            message.Write((byte)(HoofColor & 0xFF)); message.Write((byte)((HoofColor >> 8) & 0xFF)); message.Write((byte)((HoofColor >> 16) & 0xFF));
            //message.Write(Mane);
            //message.Write(Tail);
            //message.Write(Eye);
            //message.Write(Hoof);
            //message.Write(BodySize);
            //message.WriteRangedSingle(HornSize, 0f, 2f, 16);
        }

        public virtual void OnDeserialize(INetMessage message)
        {
            //Name = message.ReadString();
            Race = message.ReadByte();
            Gender = (Gender)message.ReadByte();
            CutieMark0 = message.ReadInt32();
            CutieMark1 = message.ReadInt32();
            CutieMark2 = message.ReadInt32();
            HairColor0 = message.ReadByte() | (message.ReadByte() << 8) | (message.ReadByte() << 16);
            HairColor1 = message.ReadByte() | (message.ReadByte() << 8) | (message.ReadByte() << 16);
            HairColor2 = message.ReadByte() | (message.ReadByte() << 8) | (message.ReadByte() << 16);
            BodyColor = message.ReadByte() | (message.ReadByte() << 8) | (message.ReadByte() << 16);
            EyeColor = message.ReadByte() | (message.ReadByte() << 8) | (message.ReadByte() << 16);
            HoofColor = message.ReadByte() | (message.ReadByte() << 8) | (message.ReadByte() << 16);
            //Mane = message.ReadInt16();
            //Tail = message.ReadInt16();
            //Eye = message.ReadInt16();
            //Hoof = message.ReadInt16();
            //BodySize = message.ReadFloat();
            //HornSize = message.ReadRangedSingle(0f, 2f, 16);
        }
    }
}