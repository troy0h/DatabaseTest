using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace DatabaseTest
{
    class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello\n" +
                    "(L)ogin or (S)ignup?");
            while (true)
            {
                Console.WriteLine("Please enter an option");
                string opt = Console.ReadLine();
                switch (opt)
                {
                    case "L":
                        Console.Clear();
                        LogIn();
                        break;

                    case "l":
                        Console.Clear();
                        LogIn();
                        break;

                    case "S":
                        Console.Clear();
                        SignUp();
                        break;

                    case "s":
                        Console.Clear();
                        SignUp();
                        break;

                    default:
                        Console.WriteLine("That is not a valid option\n" +
                        "Enter either L or S");
                        break;
                }
            }
        }

        public static void SignUp()
        {
            // Connection String for database

            string username, passHash, salt, userID;
            username = userID = "";

            Console.WriteLine("Sign Up");

            // Username
            bool tooLong = true;
            Functions.conn.Open();
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
                    SqlCommand testUser = new SqlCommand($"SELECT * FROM Users WHERE Username = @userName;", Functions.conn);
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
                        Functions.conn.Close();
                        Console.WriteLine("Username accepted");
                    }
                }
            }

            // Password
            Console.WriteLine("Enter a password:");
            string inputString = Functions.InsPassword("*");
            Console.WriteLine("Please confirm your password");
            string confirm = Functions.InsPassword("*");
            if (inputString == confirm)
            {
                // Add salt to password encryption
                salt = Functions.Random(16, false);
                passHash = Functions.Sha256(inputString + salt);

                // Create User ID
                bool idUnique = true;
                while (idUnique == true)
                {
                    userID = Functions.Random(12, true);
                    SqlCommand testID = new SqlCommand($"SELECT UserID FROM Users WHERE UserID = @UserID;", Functions.conn);
                    testID.Parameters.Add(new SqlParameter("@UserID", userID));
                    Functions.conn.Open();
                    if (testID.ExecuteScalar() == null)
                    {
                        idUnique = false;
                        Functions.conn.Close();
                    }
                }
                
                // Make a new SQL command to create the user
                SqlCommand newUser = new SqlCommand($"INSERT INTO Users VALUES(@UserID, @UserName, @PassHash, @Salt);", Functions.conn);
                newUser.Parameters.Add(new SqlParameter("@UserID", userID));
                newUser.Parameters.Add(new SqlParameter("@UserName", username));
                newUser.Parameters.Add(new SqlParameter("@PassHash", passHash));
                newUser.Parameters.Add(new SqlParameter("@Salt", salt));
                // Execute the new command
                Functions.conn.Open();
                newUser.ExecuteNonQuery();
                Functions.conn.Close();
                Console.WriteLine($"Your account has been created, {username}");
                Console.WriteLine("Returning to the main menu...");
                Thread.Sleep(3000);
                Console.Clear();
                Main();

            }
            else
            {
                Console.WriteLine("Your passwords do not match");
            }
        }

        public static void LogIn()
        {
            string username, userID;
            userID = "";
            string password, passwordHash, salt;
            passwordHash = salt = string.Empty;

            Console.WriteLine("Log In");

            Console.WriteLine("Please enter your username:");
            username = Console.ReadLine();
            Console.WriteLine("Please enter your password:");
            password = Functions.InsPassword("");
            
            // Create new SQL command to find a user
            using SqlCommand findUser = new SqlCommand($"SELECT * FROM Users WHERE Username = @UserName;", Functions.conn);
            findUser.Parameters.Add(new SqlParameter("@UserName", username));
            // Find the line the username is on, and populate variables
            Functions.conn.Open();
            using SqlDataReader reader = findUser.ExecuteReader();
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
            // If userID is blank, user doesn't exist
            if (userID == "")
            {
                Console.WriteLine("Username does not exist");
                Functions.conn.Close();
                Main();
            }
            else
            {
                // Try the password
                password = Functions.Sha256(password + salt);
                if (password != passwordHash)
                {
                    Console.WriteLine("Password Incorrect\n");
                    Console.WriteLine("Returning to menu...");
                    Thread.Sleep(3000);
                    Console.Clear();
                    Main();
                }
                else
                {
                    Console.WriteLine("Password Accepted\n");
                }
            }
            Functions.conn.Close();

            Console.WriteLine($"UserID: {userID}");
            Console.WriteLine($"Username: {username}");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"Password Hash: {passwordHash}");
            Console.WriteLine($"Salt: {salt}");

            Console.WriteLine($"You have now logged in, {username}");
            Console.WriteLine("Please wait...");

            Functions.userID = userID;
            Functions.username = username;
            Functions.passwordHash = passwordHash;
            Functions.salt = salt;

            Thread.Sleep(3000);
            Console.Clear();
            LoggedInMenu();
        }

        public static void LoggedInMenu()
        {
            Console.WriteLine($"Hello, {Functions.username}");
            Console.WriteLine("Please enter an option:\n" +
                "X to sign out and return to the main menu\n" +
                "C to change your password");
            while (true)
            {
                switch (Console.ReadLine())
                {
                    case "X":
                        Functions.SignOut();
                        break;

                    case "x":
                        Functions.SignOut();
                        break;

                    case "C":
                        Functions.ChangePass();
                        break;

                    case "c":
                        Functions.ChangePass();
                        break;

                    default:
                        Console.WriteLine("That is not an option");
                        break;
                }
            }
        }
    }
}
