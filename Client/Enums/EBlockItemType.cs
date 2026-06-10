using System;
using System.ComponentModel;
using MiyakoCarryService.Client.Utils;

namespace MiyakoCarryService.Client.Enums
{
    [Flags]
    public enum EBlockItemType
    {
        [Description(Locales.BLOCKITEMTYPEAMMO)]
        Ammo = 1 << 0,
        [Description(Locales.BLOCKITEMTYPEBARTER)]
        Barter = 1 << 1,
        [Description(Locales.BLOCKITEMTYPECONTAINER)]
        Container = 1 << 2,
        [Description(Locales.BLOCKITEMTYPEFOOD)]
        Food = 1 << 3,
        [Description(Locales.BLOCKITEMTYPEBACKPACK)]
        Backpack = 1 << 4,
        [Description(Locales.BLOCKITEMTYPEGOGGLES)]
        Goggles = 1 << 5,
        [Description(Locales.BLOCKITEMTYPERIG)]
        Rig = 1 << 6,
        [Description(Locales.BLOCKITEMTYPEARMOR)]
        Armor = 1 << 7,
        [Description(Locales.BLOCKITEMTYPEHEADPHONE)]
        Headphone = 1 << 8,
        [Description(Locales.BLOCKITEMTYPETACTICALVEST)]
        TacticalVest = 1 << 9,
        [Description(Locales.BLOCKITEMTYPEGRENADE)]
        Grenade = 1 << 10,
        [Description(Locales.BLOCKITEMTYPEINFO)]
        Info = 1 << 11,
        [Description(Locales.BLOCKITEMTYPEKEYS)]
        Keys = 1 << 12,
        [Description(Locales.BLOCKITEMTYPEKNIFE)]
        Knife = 1 << 13,
        [Description(Locales.BLOCKITEMTYPEMAGAZINE)]
        Magazine = 1 << 14,
        [Description(Locales.BLOCKITEMTYPEMEDS)]
        Meds = 1 << 15,
        [Description(Locales.BLOCKITEMTYPEMOD)]
        Mod = 1 << 16,
        [Description(Locales.BLOCKITEMTYPESPECIAL)]
        Special = 1 << 17,
        [Description(Locales.BLOCKITEMTYPEWEAPON)]
        Weapon = 1 << 18,
        [Description(Locales.BLOCKITEMTYPEOTHER)]
        Other = 1 << 19,
        [Description(Locales.BLOCKITEMTYPEALL)]
        All = Ammo | Barter | Container | Food | Backpack | Goggles | Rig | Armor | Headphone | TacticalVest | Grenade | Info | Keys | Knife | Magazine | Meds | Mod | Special | Weapon | Other
    }
}
