using System;
using System.Security.Cryptography;

/// <summary>
/// ���������� �� ���������� ��������� �����
/// </summary>
public static class RNG
{
    private static byte[] randb = new byte[4];
    private static RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

    /// <summary>
    /// ���������� ������������� ��������� �����  
    /// </summary>
    public static int Next()
    {
        // �������� ��������� ������������������ ������
        rand.GetBytes(randb);
        // ������������ ��� � Int32
        int value = BitConverter.ToInt32(randb, 0);
        // ���������� ������ �����
        return Math.Abs(value);
    }
    /// <summary>
    /// ���������� ������������� ��������� �����
    /// � ������ ���������
    /// </summary>
    public static int Next(int max)
    {
        return Next() % (max + 1);
    }
    /// <summary>
    /// ���������� ������������� ��������� �����
    /// � ������ ��������� � ��������
    /// </summary>
    public static int Next(int min, int max)
    {
        return Next(max - min) + min;
    }
}
