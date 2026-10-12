using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Kztek.Tool.Cryptors
{
    public class CryptographyEnginePassword
    {
        [Obsolete("Obsolete")]
        public static string Hash(string plaintext)
        {
            var messageBytes = new UnicodeEncoding().GetBytes(plaintext);
            var hash = new SHA512Managed();
            return Convert.ToBase64String(hash.ComputeHash(messageBytes));
        }
    }
}

