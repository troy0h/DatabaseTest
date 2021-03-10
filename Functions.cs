using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace DatabaseTest
{
    class Functions
    {
        static readonly string ConnectionString =
                        "Data Source=desktop-sa03gi7;" +
                        "Initial Catalog=TestLogin;" +
                        "Integrated Security=SSPI;";
        public static SqlConnection conn = new SqlConnection(ConnectionString);

        public static string Sha256(string randomString)
        {
            // Encrypt given string to SHA256
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string Random(int length, bool numeric)
        {
            // Random numbers of length length
            // Used for UserID
            if (numeric == true)
            {
                const string numbers = "1234567890";
                var res = new StringBuilder(length);
                using (var rng = new RNGCryptoServiceProvider())
                {
                    int count = (int)Math.Ceiling(Math.Log(numbers.Length, 2) / 8.0);
                    Debug.Assert(count <= sizeof(uint));
                    int offset = BitConverter.IsLittleEndian ? 0 : sizeof(uint) - count;
                    int max = (int)(Math.Pow(2, count * 8) / numbers.Length) * numbers.Length;
                    byte[] uintBuffer = new byte[sizeof(uint)];
                    while (res.Length < length)
                    {
                        rng.GetBytes(uintBuffer, offset, count);
                        uint num = BitConverter.ToUInt32(uintBuffer, 0);
                        if (num < max)
                        {
                            res.Append(numbers[(int)(num % numbers.Length)]);
                        }
                    }
                }
                return res.ToString();
            }

            // Random characters of length length 
            // Used for password salt
            else if (numeric == false)
            {
                const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
                var res = new StringBuilder(length);
                using (var rng = new RNGCryptoServiceProvider())
                {
                    int count = (int)Math.Ceiling(Math.Log(alphabet.Length, 2) / 8.0);
                    Debug.Assert(count <= sizeof(uint));
                    int offset = BitConverter.IsLittleEndian ? 0 : sizeof(uint) - count;
                    int max = (int)(Math.Pow(2, count * 8) / alphabet.Length) * alphabet.Length;
                    byte[] uintBuffer = new byte[sizeof(uint)];
                    while (res.Length < length)
                    {
                        rng.GetBytes(uintBuffer, offset, count);
                        uint num = BitConverter.ToUInt32(uintBuffer, 0);
                        if (num < max)
                        {
                            res.Append(alphabet[(int)(num % alphabet.Length)]);
                        }
                    }
                }
                return res.ToString();
            }
            else
            {
                // If no bool given, return nothing
                string res = "";
                return res;
            }
        }
    }
}
