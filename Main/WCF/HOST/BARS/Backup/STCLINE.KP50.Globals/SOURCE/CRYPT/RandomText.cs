using System;
using System.Text;

/// <summary>
/// Генерирует случайный  текст
/// </summary>
public static class RandomText
{
    /// <summary>
    /// Генерирует от 4 до 6 букв случайного текста
    /// </summary>
    public static string Generate()
    {
        // Буквы из которых будем выбирать
        // удалены буквы x, o маленькие и цифра 0,
        // т.к. их сложно отличить от большой X и буквы O
        // также как маленькая l и 1.
        char[] chars = //"abcdefghijkmnpqrstuvwyzABCDEFGHIJKLMNPQRSTUVWXYZ123456789".ToCharArray();
            "abdefghjkmnqrtABDEFGHJKLMNQRT23456789".ToCharArray();
        // Выходная строка
        StringBuilder output = new StringBuilder(4);

        // Решаем сколько букв будем использовать
        int lenght = RNG.Next(4, 6);

        // Генерируем нужное число букв
        for (int i = 0; i < lenght; i++)
        {
            // Генерируем случайный номер буквы
            int randomIndex = RNG.Next(chars.Length - 1);
            // Добавляем в выходную строку
            output.Append(chars[randomIndex]);
        }
        // Результат
        return output.ToString();
    }

    public static string Generate(int length)
    {
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        string s = "";
        int randomIndex;
        for (int i = 0; i < length; i++)
        {
            // Генерируем случайный номер символа
            randomIndex = RNG.Next(chars.Length - 1);
            // Добавляем в выходную строку
            s += chars[randomIndex];
        }
        return s;
    }
}
