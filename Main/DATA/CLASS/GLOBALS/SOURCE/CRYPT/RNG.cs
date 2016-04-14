using System;
using System.Security.Cryptography;

/// <summary>
/// √енерируем по насто€щему случайные числа
/// </summary>
public static class RNG
{
    private static byte[] randb = new byte[4];
    private static RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

    /// <summary>
    /// √енерирует положительное случайное число  
    /// </summary>
    public static int Next()
    {
        // ѕолучаем случайную последовательность байтов
        rand.GetBytes(randb);
        //  онвертируем его в Int32
        int value = BitConverter.ToInt32(randb, 0);
        // ¬озвращаем модуль числа
        return Math.Abs(value);
    }
    /// <summary>
    /// √енерирует положительное случайное число
    /// с учетом максимума
    /// </summary>
    public static int Next(int max)
    {
        return Next() % (max + 1);
    }
    /// <summary>
    /// √енерирует положительное случайное число
    /// с учетом максимума и минимума
    /// </summary>
    public static int Next(int min, int max)
    {
        return Next(max - min) + min;
    }
}
