namespace STCLINE.KP50.DataBase
{
    using System.Text.RegularExpressions;

    public static class StringExtensions
    {
        public static string ReplaceOccurences(this string value, string pattern, string replace, int occurences)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.Replace(value, replace, occurences);
        }

        public static string ReplaceFirstOccurence(this string value, string pattern, string replace)
        {
            return ReplaceOccurences(value, pattern, replace, 1);
        }
    }
}