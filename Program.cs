using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DatabaseTest
{
    class Program
    {
        static string sha256(string randomString)
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

        public static string random(int length, bool numeric)
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

        static void Main()
        {
            Console.WriteLine("Hello\n" +
                    "(L)ogin or (S)ignup?");
            while (true)
            {
                Console.WriteLine("Please enter an option");
                string LorS = Console.ReadLine();
                if (LorS == "L")
                {
                    Console.Clear();
                    logIn();
                }
                else if (LorS == "S")
                {
                    Console.Clear();
                    signUp();
                }
                else
                {
                    Console.WriteLine("That is not a valid option\n" +
                        "Enter either L or S");
                }
            }
        }

        static void signUp()
        {
            // Connection String for database
            string ConnectionString =
                        "Data Source=desktop-sa03gi7;" +
                        "Initial Catalog=TestLogin;" +
                        "Integrated Security=SSPI;";
            SqlConnection conn = new SqlConnection(ConnectionString);

            string username = "";
            string passHash = "";
            string salt = "";
            string userID = "";

            // Username
            bool tooLong = true;
            conn.Open();
            while (tooLong == true)
            {
                Console.WriteLine("Enter a username:");
                username = Console.ReadLine();
                if (username.Length > 64)
                {
                    Console.WriteLine("Username too long");
                    tooLong = true;
                }
                else if (username.Length == 0)
                {
                    Console.WriteLine("Username cannot be blank");
                    tooLong = true;
                }
                else
                {
                    SqlCommand testUser = new SqlCommand($"SELECT * FROM Users WHERE Username = @userName;", conn);
                    testUser.Parameters.Add("@UserName", SqlDbType.NChar);
                    testUser.Parameters["@UserName"].Value = username;
                    object testUserResult = testUser.ExecuteScalar();
                    if (testUserResult != null)
                    {
                        Console.WriteLine("Username already in use");
                    }
                    else
                    {
                        tooLong = false;
                        conn.Close();
                        Console.WriteLine("Username accepted");
                    }
                }
            }

            // Password
            Console.WriteLine("Enter a password:");
            string inputString = Console.ReadLine();
            Console.WriteLine("Please confirm your password");
            string confirm = Console.ReadLine();
            if (inputString == confirm)
            {
                // Add salt to password encryption
                salt = random(16, false);
                passHash = sha256(inputString + salt);

                // Create User ID
                bool idUnique = true;
                while (idUnique == true)
                {
                    userID = random(12, true);
                    SqlCommand testID = new SqlCommand($"SELECT UserID FROM Users WHERE UserID = @UserID;", conn);
                    testID.Parameters.Add("@UserID", SqlDbType.VarChar);
                    testID.Parameters["@UserID"].Value = userID;
                    conn.Open();
                    if (testID.ExecuteScalar() == null)
                    {
                        idUnique = false;
                        conn.Close();
                    }
                }
                
                SqlCommand newUser = new SqlCommand($"INSERT INTO Users VALUES(@UserID, @UserName, @PassHash, @Salt);", conn);
                newUser.Parameters.Add("@UserID", SqlDbType.NChar);
                newUser.Parameters["@UserID"].Value = userID;
                newUser.Parameters.Add("@UserName", SqlDbType.NChar);
                newUser.Parameters["@UserName"].Value = username;
                newUser.Parameters.Add("@PassHash", SqlDbType.NChar);
                newUser.Parameters["@PassHash"].Value = passHash;
                newUser.Parameters.Add("@Salt", SqlDbType.NChar);
                newUser.Parameters["@Salt"].Value = salt;
                conn.Open();
                newUser.ExecuteNonQuery();
                conn.Close();
                Console.WriteLine($"Your account has been created, {username}");
                Console.WriteLine("Returning to the main menu...");
                Thread.Sleep(1000);
                Console.Clear();
                Main();

            }
            else
            {
                Console.WriteLine("Your passwords do not match");
            }
        }

        static void logIn()
        {
            string ConnectionString =
                        "Data Source=desktop-sa03gi7;" +
                        "Initial Catalog=TestLogin;" +
                        "Integrated Security=SSPI;";
            SqlConnection conn = new SqlConnection(ConnectionString);

            string username = "";
            string password = string.Empty;
            string passwordHash = string.Empty;
            string salt = string.Empty;
            string userID = "";

            Console.WriteLine("Please enter your username:");
            username = Console.ReadLine();
            Console.WriteLine("Please enter your password:");
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
                    Console.Write("");
                    password += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            conn.Open();
            using (SqlCommand findUser = new SqlCommand($"SELECT * FROM Users WHERE Username = @UserName;", conn))
            {
                findUser.Parameters.Add(new SqlParameter("@UserName", username));
                using (SqlDataReader reader = findUser.ExecuteReader())
                {
                    int count = reader.FieldCount;
                    while (reader.Read())
                    {
                        for (int i = 0; i < count; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    userID = reader.GetValue(i).ToString();
                                    break;

                                case 1:
                                    break;

                                case 2:
                                    passwordHash = reader.GetValue(i).ToString();
                                    break;

                                case 3:
                                    salt = reader.GetValue(i).ToString();
                                    break;
                            }
                        }
                    }
                    if (userID == "")
                    {
                        Console.WriteLine("Username does not exist");
                        Main();
                    }
                    else
                    {
                        password = sha256(password + salt);
                        if (password != passwordHash)
                        {
                            Console.WriteLine("Password Incorrect\n");
                        }
                        else
                        {
                            Console.WriteLine("Password Accepted\n");
                        }
                    }

                    Console.WriteLine($"UserID: {userID}");
                    Console.WriteLine($"Username: {username}");
                    Console.WriteLine($"Password: {password}");
                    Console.WriteLine($"Password Hash: {passwordHash}");
                    Console.WriteLine($"Salt: {salt}");
                }
            }
        }
    }
}
