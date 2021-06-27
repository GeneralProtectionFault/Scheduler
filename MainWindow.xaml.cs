using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.Odbc;
using System.Globalization;


namespace Scheduler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool isTagalog = false;

        internal static string MySQLConnectionString = @"DRIVER={MySQL ODBC 8.0 Unicode Driver};" +
                                            "SERVER=wgudb.ucertify.com;" +
                                            "Port=3306;" +
                                            "DATABASE=U066t8;" +
                                            "UID=U066t8;" +
                                            "PASSWORD=53688691784;" +
                                            "OPTION=3";
        // Track the user that logged in
        internal static int userId;
        internal static string userName;

        internal static int customerId;
        
        // Used to track the record when updating/editing
        internal static int appointmentId;
        internal static int addressId;
        


        public MainWindow()
        {
            InitializeComponent();

            //ConnectionTest();

            var location = GetUserLocation();

            // TESTING
            MessageBox.Show(location);

            if (location == "fil")
            {
                TagalogWindow();
            }
        }


        void ConnectionTest()
        {
            OdbcConnection conn = new OdbcConnection(MySQLConnectionString);

            using (conn)
            {
                conn.Open();

                MessageBox.Show("Connected!");
            }
            
        }




        string GetUserLocation()
        {
            string culture = CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
            return culture;
        }




        void TagalogWindow()
        {
            isTagalog = true;

            lblLogin.Content = "Mag-log in";
            lblPassword.Content = "Hudyat";
            btnSubmit.Content = "Lpasa";
            this.Title = "Window sa Pag-login";
        }



        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            var login = txtLogin.Text;
            var password = txtPassword.Password;

            OdbcConnection conn = new OdbcConnection(MySQLConnectionString);

            using (conn)
            {
                conn.Open();

                // Confirm user exists
                using (OdbcCommand userCheck = conn.CreateCommand())
                {
                    userCheck.CommandText = @"select * from user
                                        WHERE userName = ?;";
                    userCheck.Parameters.Add("@username", OdbcType.NVarChar).Value = login;
                    
                    OdbcDataReader reader = userCheck.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        if (isTagalog)
                            MessageBox.Show($"Walang user {login}");
                        else
                            MessageBox.Show($"User: {login} does not exist");

                        return;
                    }
                }

                // Confirm correct password
                using (OdbcCommand passwordCheck = conn.CreateCommand())
                {
                    passwordCheck.CommandText = @"select password from user WHERE userName = ?;";
                    passwordCheck.Parameters.Add("@username", OdbcType.NVarChar).Value = login;

                    var pass = "";

                    OdbcDataReader reader2 = passwordCheck.ExecuteReader();
                    while(reader2.Read())
                    {
                        pass = reader2.GetString("password");
                    }

                    if (pass != password)
                    {
                        if (isTagalog)
                            MessageBox.Show("Mali ang pagpasok at/o password.");
                        else
                            MessageBox.Show("You do not carry the membership.\nPassword is incorrect.");
                        
                        return;
                    }
                }
                // *** Login successful by this point ***
                // Store the userID for whatever the user adds/deletes/updates in the Dash window
                using (OdbcCommand setUserId = conn.CreateCommand())
                {
                    setUserId.CommandText = @"select userId from user WHERE userName = ?;";
                    setUserId.Parameters.Add("@username", OdbcType.NVarChar).Value = login;

                    userId = (int)setUserId.ExecuteScalar();
                }

                userName = login;
            }

            Logger.Log($"User {login} loggin in.");


            // Otherwise, cue the main application window and axe the login window
            var dashboard = new Windows.Dash();
            dashboard.Show();
            this.Close();

        }
    }
}
