using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DatabaseTest
{
    class Functions
    {
        static readonly string ConnectionString =
                        "Data Source=desktop-sa03gi7;" +
                        "Initial Catalog=TestLogin;" +
                        "Integrated Security=SSPI;";
        public static SqlConnection conn = new SqlConnection(ConnectionString);

        public static string userID;
        public static string username;
        public static string passwordHash;
        public static string salt;

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

        public static string InsPassword(string toBeOutput)
        {
            string password = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    Console.Write("\b \b");
                    password = password[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write(toBeOutput);
                    password += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            if (toBeOutput == "")
            { }
            else
            {
                Console.WriteLine("\n");
            }
            return password;
        }

        public static void SignOut()
        {
            userID = "";
            username = "";
            passwordHash = "";
            salt = "";
            Console.WriteLine("Signing Out...");
            Thread.Sleep(3000);
            Console.Clear();
            Program.Main();
        }

        public static void ChangePass()
        {
            Console.WriteLine($"Enter the current password for the user account \"{username}\"");
            string passConfirm = InsPassword("*");
            if (Sha256(passConfirm + salt) != passwordHash)
            {
                Console.WriteLine("That password is incorrect\n" +
                    "Returning to menu...");
                Thread.Sleep(3000);
                Program.LoggedInMenu();
            }
            else
            {
                Console.WriteLine("Password accepted\n" +
                    "Please enter your new password");
                string newPass = InsPassword("*");
                Console.WriteLine("Please confirm your password");
                string newPassConfirm = InsPassword("*");
                if (newPass != newPassConfirm)
                {
                    Console.WriteLine("Those passwords do not match");
                    Console.WriteLine("Returning to menu...");
                    Thread.Sleep(3000);
                    Console.Clear();
                    Program.LoggedInMenu();
                }
                else
                {
                    Console.WriteLine("New password accepted");
                    string salt = Random(16, false);
                    string passHash = Sha256(newPass + salt);

                    conn.Open();
                    SqlCommand changePass = new SqlCommand($"UPDATE Users SET UserID=@UserID, Username=@UserName, Password=@PassHash, Salt=@Salt WHERE Username=@UserName", Functions.conn);
                    changePass.Parameters.Add(new SqlParameter("@UserID", userID));
                    changePass.Parameters.Add(new SqlParameter("@UserName", username));
                    changePass.Parameters.Add(new SqlParameter("@PassHash", passHash));
                    changePass.Parameters.Add(new SqlParameter("@Salt", salt));
                    changePass.ExecuteNonQuery();
                    conn.Close();

                    Console.WriteLine("Password change successful");
                    Console.WriteLine("Signing out...\n" +
                        "Please login with your new password");
                    Thread.Sleep(3000);
                    userID = "";
                    username = "";
                    passwordHash = "";
                    Console.Clear();
                    Program.Main();
                }
            }
        }
    }
}
