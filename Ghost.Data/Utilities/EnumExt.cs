using System;

namespace Ghost.Data.Utilities
{
    public enum ConditionType
    {
        Always,
        Scripted
    }

    public enum ScriptType
    {
        None,
        Condition
    }
    

    public enum Stats : byte
    {
        None,
        Health,
        HealthRegen,
        Energy,
        EnergyRegen,
        Attack,
        Dodge,
        Armor,
        MagicResist,
        Resiliance,
        Fortitude,
        Evade,
        Tension,
        Speed,
        MagicCasting,
        PhysicalCasting,
        Taunt,
        Unspecified = 255
    }

    public enum Gender : byte
    {
        Filly,
        Colt,
        Mare,
        Stallion,
        BatStallion,
        BatMare,
        CrystalMare,
        CrystalStallion
    }

    public enum CharacterType : byte
    {
        None,
        EarthPony,
        Unicorn,
        Pegasus,
        Moose,
        Gryphon
    }

    [Flags]
    public enum WearableSlot : uint
    {
        None = 0,
        Tail = 1,
        Pants = 2,
        FrontSocks = 4,
        BackSocks = 8,
        FrontShoes = 16,
        BackShoes = 32,
        Saddle = 64,
        Shirt = 128,
        Necklace = 256,
        Mouth = 512,
        Mask = 1024,
        Eyes = 2048,
        Ears = 4096,
        FrontKnees = 8192,
        BackKnees = 16384, 
        SaddleBags = 1073741824,
        Hat = 2147483648
    }

    [Flags]
    public enum ItemFlags : byte
    {
        None, Stackable, Salable, Stats = 4, Usable = 8,
        Color01 = 64, Color02 = 128
    }
    public static class EnumExt
    {

    }
}