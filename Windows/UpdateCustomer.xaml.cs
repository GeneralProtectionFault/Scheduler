using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Scheduler.Windows
{
    /// <summary>
    /// Interaction logic for UpdateCustomer.xaml
    /// </summary>
    public partial class UpdateCustomer : Window
    {
        private Dictionary<string, int> currentCountries = new Dictionary<string, int>();
        private Dictionary<string, int> currentCities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static EventHandler CustomerUpdated;


        public UpdateCustomer()
        {
            InitializeComponent();


            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            {
                // Populate country box w/ available countries.
                using (OdbcCommand populateCountries = conn.CreateCommand())
                {
                    conn.Open();

                    populateCountries.CommandText = @"select * from country;";

                    OdbcDataReader reader = populateCountries.ExecuteReader();

                    while (reader.Read())
                    {
                        currentCountries.Add(reader.GetString(1), reader.GetInt32(0));
                        cmbUpdateCustomerCountry.Items.Add(reader.GetString(1));
                    }
                }

                // Populate city dictionary w/ available cities.
                using (OdbcCommand populateCities = conn.CreateCommand())
                {
                    populateCities.CommandText = @"select * from city;";

                    OdbcDataReader reader2 = populateCities.ExecuteReader();

                    while (reader2.Read())
                    {
                        currentCities.Add(reader2.GetString(1), reader2.GetInt32(0));
                        // cmbUpdateCustomerCity.Items.Add(reader2.GetString(1));
                    }
                }


                // Populate customer name (we're editing THIS customer, don't allow changing, just display it
                using (OdbcCommand populateCustomerName = conn.CreateCommand())
                {
                    populateCustomerName.CommandText = $"select customerName from customer WHERE customerId = {MainWindow.customerId};";
                    MainWindow.customerName = populateCustomerName.ExecuteScalar().ToString();
                    txtUpdateCustomerName.Text = MainWindow.customerName;
                }
            }
        }



        private void btnUpdateCustomerCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        private void btnUpdateCustomerConfirm_Click(object sender, RoutedEventArgs e)
        {
            // Validation ************************
            if (cmbUpdateCustomerCountry.SelectedItem == null ||
                String.IsNullOrEmpty(txtUpdateCustomerCity.Text) ||
                String.IsNullOrEmpty(txtUpdateCustomerStreet.Text) ||
                String.IsNullOrEmpty(txtUpdateCustomerPostal.Text) ||
                String.IsNullOrEmpty(txtUpdateCustomerPhone.Text))
            {
                MessageBox.Show("Please fill out all fields before confirming.");
                return;
            }

            long phoneNumber;
            if (!long.TryParse(txtUpdateCustomerPhone.Text, out phoneNumber) || txtUpdateCustomerPhone.Text.Length != 10)
            {
                MessageBox.Show("Please enter a valid, unformatted (no dashes, parenthesis, etc.) phone number.");
                return;
            }

            int zip;
            if (!int.TryParse(txtUpdateCustomerPostal.Text, out zip) || txtUpdateCustomerPostal.Text.Length != 5)
            {
                MessageBox.Show("Please enter a valid postal code (5-digit).");
                return;
            }



            // Check if the city is already in the database
            int enteredCityIndex = -1;
            
            if (currentCities.ContainsKey(txtUpdateCustomerCity.Text))
            {
                enteredCityIndex = currentCities[txtUpdateCustomerCity.Text];
            }


            // If the city isn't already in the database, add it!
            if (enteredCityIndex == -1)
            {
                using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
                {
                    using (OdbcCommand insertCity = conn.CreateCommand())
                    {
                        conn.Open();

                        insertCity.CommandText = $"INSERT INTO city (city, countryId, createDate, createdBy, lastUpdate, lastUpdateBy) " +
                            $"VALUES('{txtUpdateCustomerCity.Text}', {currentCountries[cmbUpdateCustomerCountry.SelectedItem.ToString()]}, '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', " +
                            $"'{MainWindow.userName}', '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}'); ";


                        insertCity.ExecuteNonQuery();
                    }

                    // Grab the Id of the newly entered city!
                    using (OdbcCommand getCityId = conn.CreateCommand())
                    {
                        getCityId.CommandText = $"select MAX(cityId) from city;";
                        enteredCityIndex = (int)getCityId.ExecuteScalar();
                    }
                }
            }




            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            {
                conn.Open();

                // Do the update in the address table...
                using (OdbcCommand updateAddress = conn.CreateCommand())
                {
                    updateAddress.CommandText = $"update address SET address = '{txtUpdateCustomerStreet.Text}', address2 = '{txtUpdateCustomerStreet2.Text}', cityId = {enteredCityIndex}, " +
                        $"postalCode = '{txtUpdateCustomerPostal.Text}', phone = '{txtUpdateCustomerPhone.Text}', lastUpdate = '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', lastUpdateBy = '{MainWindow.userName}' " +
                        $"WHERE addressId = {MainWindow.addressId};";

                    updateAddress.ExecuteNonQuery();
                }


                int enableCustomer = (bool)chkUpdateCustomerEnable.IsChecked ? 1 : 0;

                using (OdbcCommand updateCustomer = conn.CreateCommand())
                {
                    updateCustomer.CommandText = $"update customer SET customerName = '{txtUpdateCustomerName.Text}', active = {enableCustomer}, lastUpdate = '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', lastUpdateBy = '{MainWindow.userName}' " +
                        $"WHERE customerId = {MainWindow.customerId};";

                    updateCustomer.ExecuteNonQuery();
                }


            }

            CustomerUpdated?.Invoke(this, e);

            Close();
        }
    }
}
