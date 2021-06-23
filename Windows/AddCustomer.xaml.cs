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
    public class Country
    {
        public int countryId { get; set; }
        public string country { get; set; }
    }

    public class City
    {
        public int cityId { get; set; }
        public string city { get; set; }
    }


    /// <summary>
    /// Interaction logic for AddCustomer.xaml
    /// </summary>
    public partial class AddCustomer : Window
    {
        private Dictionary<string, int> currentCountries = new Dictionary<string, int>();
        private Dictionary<string, int> currentCities = new Dictionary<string, int>();

        public static event EventHandler CustomerAdded;

        public AddCustomer()
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
                        cmbCountry.Items.Add(reader.GetString(1));
                    }
                }

                // Populate city box w/ available cities.
                using (OdbcCommand populateCities = conn.CreateCommand())
                {
                    populateCities.CommandText = @"select * from city;";

                    OdbcDataReader reader2 = populateCities.ExecuteReader();

                    while (reader2.Read())
                    {
                        currentCities.Add(reader2.GetString(1), reader2.GetInt32(0));
                        cmbCity.Items.Add(reader2.GetString(1));
                    }
                }

            }
        }

        private void btnAddCustomerConfirm_Click(object sender, RoutedEventArgs e)
        {
            // Validation ************************
            if(cmbCountry.SelectedItem == null ||
                cmbCity.SelectedItem == null ||
                String.IsNullOrEmpty(txtAddCustomerName.Text) ||
                String.IsNullOrEmpty(txtAddCustomerPhone.Text) ||
                String.IsNullOrEmpty(txtAddCustomerStreet.Text) ||
                String.IsNullOrEmpty(txtAddCustomerPostal.Text))
            {
                MessageBox.Show("Please fill out all fields before confirming.");
                return;
            }

            long phoneNumber;
            if(!long.TryParse(txtAddCustomerPhone.Text, out phoneNumber) || txtAddCustomerPhone.Text.Length != 10)
            {
                MessageBox.Show("Please enter a valid, unformatted (no dashes, parenthesis, etc.) phone number.");
                return;
            }

            int zip;
            if(!int.TryParse(txtAddCustomerPostal.Text, out zip) || txtAddCustomerPostal.Text.Length != 5 )
            {
                MessageBox.Show("Please enter a valid postal code (5-digit).");
                return;
            }



            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            {
                conn.Open();

                // Check city vs. country
                using (OdbcCommand countryCheck = conn.CreateCommand())
                {
                    countryCheck.CommandText = $"select countryId from city WHERE city = '{cmbCity.SelectedItem}';";
                    var result = countryCheck.ExecuteScalar();

                    if(result == null)
                    {
                        MessageBox.Show("City is not from selected country.  Please mitigate.");
                        return;
                    }
                }




                using (OdbcCommand addAddress = conn.CreateCommand())
                {
                    addAddress.CommandText = $"INSERT INTO address (address, address2, cityId, postalCode, phone, createDate, createdBy, lastUpdate, lastUpdateBy) VALUES " +
                        $"('{txtAddCustomerStreet.Text}', '{txtAddCustomerStreet2.Text}', {currentCities[cmbCity.SelectedItem.ToString()]}, '{txtAddCustomerPostal.Text}', '{txtAddCustomerPhone.Text}', " +
                        $"'{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}', '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}')";

                    addAddress.ExecuteNonQuery();                    
                }

                
                // Get the auto-increment addressId from the record we just inserted above
                int addressId = 0;

                using (OdbcCommand getAddressId = conn.CreateCommand())
                {
                    getAddressId.CommandText = $"select MAX(addressId) from address;";
                    addressId = (int)getAddressId.ExecuteScalar();
                }


                using (OdbcCommand addCustomer = conn.CreateCommand())
                {
                    addCustomer.CommandText = $"INSERT INTO customer (customerName, addressId, active, createDate, createdBy, lastUpdate, lastUpdateBy) VALUES " +
                        $"('{txtAddCustomerName.Text}', {addressId}, 1, '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}', '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}');";

                    addCustomer.ExecuteNonQuery();
                }


            }

            CustomerAdded?.Invoke(this, e);

            Close();
        }





        private void btnAddCustomerCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }



    
}
