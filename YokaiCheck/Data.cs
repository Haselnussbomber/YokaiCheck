namespace YokaiCheck;

public readonly record struct MinionInfo(uint Minion, uint Achievement, uint Count, uint Item);
public readonly record struct WeaponInfo(uint Weapon, uint Subweapon, int Achievement, uint Count, uint Medal);

public static class Data
{
    public static int MINION_COUNT_ALL_13 = 510;
    public static int MINION_COUNT_ALL_17 = 870;
    public static int MINION_ACHIEVEMENT_ALL_17 = 2612;
    public static MinionInfo[] MINION_INFO_TABLE = [
        new (200, 1513, 447, 15195),
        new (201, 1514, 448, 15196),
        new (202, 1515, 449, 15197),
        new (203, 1516, 450, 15198),
        new (204, 1517, 451, 15199),
        new (205, 1518, 452, 15200),
        new (206, 1519, 453, 15201),
        new (207, 1520, 454, 15202),
        new (208, 1521, 455, 15203),
        new (209, 1522, 456, 15204),
        new (210, 1523, 457, 15205),
        new (211, 1524, 458, 15206),
        new (212, 1525, 459, 15207),
        new (390, 2608, 866, 30877),
        new (391, 2610, 868, 30878),
        new (392, 2609, 867, 30879),
        new (393, 2611, 869, 30880),
    ];
    public static int WEAPON_ACHIEVEMENT_ALL_13 = 1539;
    public static int WEAPON_ACHIEVEMENT_ALL_17 = 2617;
    public static int WEAPON_COUNT_ALL_13 = 489;
    public static int WEAPON_COUNT_ALL_17 = 875;
    public static WeaponInfo[] WEAPON_INFO_TABLE = [
        new (15208, 15221, 1535, 485, 15177),
        new (15210, 0, 1526, 476, 15168),
        new (15214, 0, 1536, 486, 15178),
        new (30809, 0, 2613, 871, 30805),
        new (15209, 0, 1538, 488, 15180),
        new (15211, 0, 1534, 484, 15176),
        new (15213, 0, 1530, 480, 15172),
        new (30807, 0, 2614, 872, 30803),
        new (15212, 0, 1528, 478, 15170),
        new (15215, 0, 1537, 487, 15179),
        new (30810, 0, 2616, 874, 30806),
        new (15217, 0, 1529, 479, 15171),
        new (15218, 0, 1532, 482, 15174),
        new (30808, 0, 2615, 873, 30804),
        new (15216, 0, 1527, 477, 15169),
        new (15219, 0, 1531, 481, 15173),
        new (15220, 0, 1533, 483, 15175),
    ];
    public static uint PORTRAIT_ITEM_CATALOG_ID = 41797;
    public static int PORTRAIT_MGP_CATALOG_ID = 29;
    public static int PORTRAIT_NEED_MGP = 20000;
}
