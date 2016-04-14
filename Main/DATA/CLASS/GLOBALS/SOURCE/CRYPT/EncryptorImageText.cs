using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

public static class EncryptorImageText
{
    private static string Password = "NenCrhsnGfhjkm";

    private static byte[] Salt
    {
        get
        {
            return Convert.FromBase64String("NenCfkmn");
        }
    }

    public static string Encrypt(string imageText)
    {
        return Encryptor.Encrypt(imageText, Password, Salt);
    }

    public static string Decrypt(string imageText)
    {
        // если в зашифрованной строке был знак +,
        // то в URL он заменится на пробел, поэтому
        // его нужно восстановить.
        string decode = imageText.Replace(' ','+');
        return Encryptor.Decrypt(decode, Password, Salt);
    }
}
