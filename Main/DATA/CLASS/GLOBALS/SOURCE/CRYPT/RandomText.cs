using System;
using System.Text;

/// <summary>
/// ���������� ���������  �����
/// </summary>
public static class RandomText
{
    /// <summary>
    /// ���������� �� 4 �� 6 ���� ���������� ������
    /// </summary>
    public static string Generate()
    {
        // ����� �� ������� ����� ��������
        // ������� ����� x, o ��������� � ����� 0,
        // �.�. �� ������ �������� �� ������� X � ����� O
        // ����� ��� ��������� l � 1.
        char[] chars = //"abcdefghijkmnpqrstuvwyzABCDEFGHIJKLMNPQRSTUVWXYZ123456789".ToCharArray();
            "abdefghjkmnqrtABDEFGHJKLMNQRT23456789".ToCharArray();
        // �������� ������
        StringBuilder output = new StringBuilder(4);

        // ������ ������� ���� ����� ������������
        int lenght = RNG.Next(4, 6);

        // ���������� ������ ����� ����
        for (int i = 0; i < lenght; i++)
        {
            // ���������� ��������� ����� �����
            int randomIndex = RNG.Next(chars.Length - 1);
            // ��������� � �������� ������
            output.Append(chars[randomIndex]);
        }
        // ���������
        return output.ToString();
    }

    public static string Generate(int length)
    {
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        string s = "";
        int randomIndex;
        for (int i = 0; i < length; i++)
        {
            // ���������� ��������� ����� �������
            randomIndex = RNG.Next(chars.Length - 1);
            // ��������� � �������� ������
            s += chars[randomIndex];
        }
        return s;
    }
}
