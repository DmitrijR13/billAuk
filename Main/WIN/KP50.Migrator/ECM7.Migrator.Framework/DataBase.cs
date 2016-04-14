using System;

namespace KP50.DataBase.Migrator.Framework
{
    [Flags]
    public enum DataBase
    {
        CentralBank = 1,
        LocalBank = 2,
        Charge = 4,
        Fin = 8,
        Web = 16
    }

    [Flags]
    public enum Bank
    {
        Kernel = 1,
        Data = 2,
        Upload = 4,
        Supg = 8,
        Debt = 16,
    }
}
