using System;
using System.ComponentModel;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Enums
{
    
    [Flags]
    public enum EBlockItemType
    {
        [Description(Locales.BLOCKITEMTYPEAMMO)]
        Ammo = 1,
        [Description(Locales.BLOCKITEMTYPEBARTER)]
        Barter = 2,
        [Description(Locales.BLOCKITEMTYPECONTAINER)]
        Container = 4,
        [Description(Locales.BLOCKITEMTYPEFOOD)]
        Food = 8,
        [Description(Locales.BLOCKITEMTYPEBACKPACK)]
        Backpack = 16,
        [Description(Locales.BLOCKITEMTYPEGOGGLES)]
        Goggles = 32,
        [Description(Locales.BLOCKITEMTYPERIG)]
        Rig = 64,
        [Description(Locales.BLOCKITEMTYPEARMOR)]
        Armor = 128,
        [Description(Locales.BLOCKITEMTYPEEQUIPMENT)]
        Equipment = 256,
        [Description(Locales.BLOCKITEMTYPEGRENADE)]
        Grenade = 512,
        [Description(Locales.BLOCKITEMTYPEINFO)]
        Info = 1024,
        [Description(Locales.BLOCKITEMTYPEKEYS)]
        Keys = 2048,
        [Description(Locales.BLOCKITEMTYPEKNIFE)]
        Knife = 4096,
        [Description(Locales.BLOCKITEMTYPEMAGAZINE)]
        Magazine = 8192,
        [Description(Locales.BLOCKITEMTYPEMEDS)]
        Meds = 16384,
        [Description(Locales.BLOCKITEMTYPEMOD)]
        Mod = 32768,
        [Description(Locales.BLOCKITEMTYPESPECIAL)]
        Special = 65536,
        [Description(Locales.BLOCKITEMTYPEWEAPON)]
        Weapon = 131072,
        [Description(Locales.BLOCKITEMTYPEOTHER)]
        Other = 262144,
        [Description(Locales.BLOCKITEMTYPEALL)]
        All = Ammo | Barter | Container | Food | Backpack | Goggles | Rig | Armor | Equipment | Grenade | Info | Keys | Knife | Magazine | Meds | Mod | Special | Weapon | Other
    }
}
