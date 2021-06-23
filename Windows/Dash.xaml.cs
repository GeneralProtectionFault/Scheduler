using System;
using System.Data.Odbc;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Scheduler.Windows
{
    /// <summary>
    /// Interaction logic for Dash.xaml
    /// </summary>
    public partial class Dash : Window
    {
        public static DashViewModel dashModel = new DashViewModel();


        public Dash()
        {
            InitializeComponent();
            DataContext = dashModel;

            // For updating the collection whenever an appointment is added
            AddAppointment.AppointmentAdded += UpdateAppointmentsFromEvent;
            AddCustomer.CustomerAdded += UpdateCustomersFromEvent;
            UpdateAppointment.AppointmentUpdated += UpdateAppointmentsFromEvent;
            UpdateCustomer.CustomerUpdated += UpdateCustomersFromEvent;

            if (calendar.SelectedDate == null)
                calendar.SelectedDate = DateTime.Now.Date;

            var query = $"select * from appointment WHERE DATE(start) = DATE('{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}');";
            // Start/default to all appointments
            CreateDataCollection(query);
            CreateCustomerCollection();
        }



        internal void CreateCustomerCollection()
        {
            dashModel.customers.Clear();

            OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString);

            using (conn)
            {
                using (OdbcCommand populateCustomers = conn.CreateCommand())
                {
                    conn.Open();

                    populateCustomers.CommandText = @"select c.customerId, c.addressId, c.active, c.customerName, a.address, a.address2, a.postalCode, a.phone, c.createDate, c.createdBy, c.lastUpdate, c.lastUpdateBy
                                                        from customer c
                                                        JOIN address a on c.addressId = a.addressId;";

                    OdbcDataReader reader = populateCustomers.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while(reader.Read())
                        {
                            //var customerId = reader.GetInt32(0);
                            //var addressId = reader.GetInt32(1);
                            //var active = reader.GetInt32(2);
                            //var customerName = reader.GetString(3);
                            //var address = reader.GetString(4);
                            //var address2 = reader.GetString(5);
                            //var postalCode = reader.GetString(6);
                            //var createDate = reader.GetDateTime(7).ToLocalTime();
                            //var createdBy = reader.GetString(8);
                            //var lastUpdate = reader.GetDateTime(9).ToLocalTime();
                            //var lastUpdateBy = reader.GetString(10);

                            dashModel.customers.Add(
                                new Customer()
                                {
                                    customerId = reader.GetInt32(0),
                                    addressId = reader.GetInt32(1),
                                    active = reader.GetInt32(2),
                                    customerName = reader.GetString(3),
                                    address = reader.GetString(4),
                                    address2 = reader.GetString(5),
                                    postalCode = reader.GetString(6),
                                    phone = reader.GetString(7),
                                    createDate = reader.GetDateTime(8).ToLocalTime(),
                                    createdBy = reader.GetString(9),
                                    lastUpdate = reader.GetDateTime(10).ToLocalTime(),
                                    lastUpdateBy = reader.GetString(11)
                                }); ;
                        }
                    }
                }
            }
        }



        private void CreateDataCollection(string queryString)
        {
            dashModel.appointments.Clear();
            cmbEvent.Items.Clear();

            OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString);

            using (conn)
            {
                using (OdbcCommand populateAppointments = conn.CreateCommand())
                {
                    conn.Open();

                    populateAppointments.CommandText = queryString;

                    OdbcDataReader reader = populateAppointments.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            dashModel.appointments.Add(
                                new Appointment()
                                {
                                    appointmentId = reader.GetInt32(0),
                                    customerId = reader.GetInt32(1),
                                    userId = reader.GetInt32(2),
                                    title = reader.GetString(3),
                                    description = reader.GetString(4),
                                    location = reader.GetString(5),
                                    contact = reader.GetString(6),
                                    type = reader.GetString(7),
                                    url = reader.GetString(8),
                                    start = reader.GetDateTime(9).ToLocalTime(),
                                    end = reader.GetDateTime(10).ToLocalTime(),
                                    createDate = reader.GetDateTime(11).ToLocalTime(),
                                    createdBy = reader.GetString(12),
                                    lastUpdate = reader.GetDateTime(13).ToLocalTime(),
                                    lastUpdatedBy = reader.GetString(14)
                                }
                                );


                            // Prevent duplication
                            if (cmbEvent.Items.Contains(reader.GetString(7)))
                                continue;
                            else
                                cmbEvent.Items.Add(reader.GetString(7));

                        }
                    }
                }
            }
        }



        /// <summary>
        /// Modify the collection so only appointments of a given day are selected
        /// </summary>
        public void SelectDay()
        {
            // First, get the day selected from the calendar
            var calendarDate = calendar.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
           
            // Debug.WriteLine(calendarDate);

            if (calendarDate != null)
                CreateDataCollection($"select * from appointment WHERE DATE(start) = DATE('{calendarDate}')");
        }


        private void SelectWeek()
        {
            // First, get the day selected from the calendar
            var calendarDate = calendar.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");

            // Debug.WriteLine(calendarDate);

            if (calendarDate != null)
                CreateDataCollection($"select * from appointment WHERE WEEK(start) = WEEK('{calendarDate}')");
        }


        private void SelectMonth()
        {
            // First, get the day selected from the calendar
            var calendarDate = calendar.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");

            // Debug.WriteLine(calendarDate);

            if (calendarDate != null)
                CreateDataCollection($"select * from appointment WHERE MONTH(start) = MONTH('{calendarDate}')");
        }




        private void radDay_Click(object sender, RoutedEventArgs e)
        {
            SelectDay();
        }

        private void radWeek_Click(object sender, RoutedEventArgs e)
        {
            SelectWeek();
        }

        private void radMonth_Click(object sender, RoutedEventArgs e)
        {
            SelectMonth();
        }


        internal void UpdateDates()
        {
            if (radDay.IsChecked == true)
                SelectDay();
            else if (radWeek.IsChecked == true)
                SelectWeek();
            else if (radMonth.IsChecked == true)
                SelectMonth();
        }



        private void UpdateCustomersFromEvent(object sender, EventArgs e)
        {
            CreateCustomerCollection();
        }


        private void UpdateAppointmentsFromEvent(object sender, EventArgs e)
        {
            UpdateDates();
        }



        private void calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDates();
        }




        /// <summary>
        /// Whenever a selection in the combo box changes, highlight corresponding rows in the appointment grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbEvent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Iterate through all rows (appointments)
            for (int i = 0; i < dataAppointments.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)dataAppointments.ItemContainerGenerator.ContainerFromIndex(i);
                var descriptionCellContent = dataAppointments.Columns[7].GetCellContent(row) as TextBlock;
                
                // If the type column matches the selected item in the combobox, focus & highlight
                if (descriptionCellContent.Text == cmbEvent.SelectedItem.ToString())
                {
                    //row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    row.Background = new SolidColorBrush(Colors.LightBlue);
                }
                // Otherwise, set back to previous color
                else
                    row.Background = new SolidColorBrush(Colors.Transparent);
            }
        }



        /// <summary>
        /// When a selection in the datagrid changes, select the corresponding item in the dropdown (combobox)
        /// Also, select the customer record corresponding to the appointment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataAppointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataAppointments.SelectedItem == null)
                return;

            DataGridRow row = (DataGridRow)dataAppointments.ItemContainerGenerator.ContainerFromItem(dataAppointments.SelectedItem);
            var typeCellContent = dataAppointments.Columns[7].GetCellContent(row) as TextBlock;

            // Set ComboBox
            foreach (var item in cmbEvent.Items)
            {
                if (item.ToString() == typeCellContent.Text)
                {
                    cmbEvent.SelectedValue = typeCellContent.Text;
                    break;
                }
            }

            // customer ID from APPOINTMENT table
            var appointmentCustomerId = dataAppointments.Columns[1].GetCellContent(row) as TextBlock;

            // Set Customer
            for (int i = 0; i < dataCustomers.Items.Count; i++)
            {
                DataGridRow row2 = (DataGridRow)dataCustomers.ItemContainerGenerator.ContainerFromIndex(i);
                var customerTableCustomerId = dataCustomers.Columns[0].GetCellContent(row2) as TextBlock;

                // If the customerId column matches the selected item in the appointments, focus & highlight
                if (customerTableCustomerId.Text == appointmentCustomerId.Text)
                {
                    //row2.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    row2.Background = new SolidColorBrush(Colors.LightBlue);
                }
                // Otherwise, set back to previous color
                else
                    row2.Background = new SolidColorBrush(Colors.Transparent);
            }
        }




        #region AppointmentButtons

        private void btnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dataCustomers.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer to associate with this appointment.");
                return;
            }

            // Get the customer ID from the one that is selected
            DataGridRow row = (DataGridRow)dataCustomers.ItemContainerGenerator.ContainerFromItem(dataCustomers.SelectedItem);
            var customerCellContent = dataCustomers.Columns[0].GetCellContent(row) as TextBlock;
            var customerNameCellContent = dataCustomers.Columns[1].GetCellContent(row) as TextBlock;

            MainWindow.customerId = Convert.ToInt32(customerCellContent.Text);

            var appointmentWindow = new AddAppointment();
            appointmentWindow.Show();
        }



        /// <summary>
        /// Change an existing appointment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dataAppointments.SelectedItem == null)
            {
                MessageBox.Show("Please select an appointment record to update.");
                return;
            }

            DataGridRow row = (DataGridRow)dataAppointments.ItemContainerGenerator.ContainerFromItem(dataAppointments.SelectedItem);
            var appointmentCellContent = dataAppointments.Columns[0].GetCellContent(row) as TextBlock;
            var appointmentId = Convert.ToInt32(appointmentCellContent.Text);


        }



        /// <summary>
        /// Deletes...the selected appointment!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dataAppointments.SelectedItem == null)
            {
                MessageBox.Show("Please select an appointment to delete.");
                return;
            }

            DataGridRow row = (DataGridRow)dataAppointments.ItemContainerGenerator.ContainerFromItem(dataAppointments.SelectedItem);
            var appointmentCellContent = dataAppointments.Columns[0].GetCellContent(row) as TextBlock;
            var appointmentId = Convert.ToInt32(appointmentCellContent.Text);

            MessageBoxResult result = MessageBox.Show($"Are you sure you wish to delete appointment ID: {appointmentId}?", "Confirm delete", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString);

            using(conn)
            using(OdbcCommand deleteAppointment = conn.CreateCommand())
            {
                conn.Open();

                deleteAppointment.CommandText = $"delete from appointment WHERE appointmentId = {appointmentId};";
                deleteAppointment.ExecuteNonQuery();
            }

            UpdateDates();
        }


        #endregion



        #region CustomerButtons

        private void btnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var customerWindow = new AddCustomer();
            customerWindow.Show();
        }



        private void btnUpdateCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dataCustomers.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer to update.");
                return;
            }

            // Store these Ids in static variables for updating

            DataGridRow row = (DataGridRow)dataCustomers.ItemContainerGenerator.ContainerFromItem(dataCustomers.SelectedItem);
            var customerIdCell = dataCustomers.Columns[0].GetCellContent(row) as TextBlock;
            MainWindow.customerId = Convert.ToInt32(customerIdCell.Text);

            var customerAddressCell = dataCustomers.Columns[2].GetCellContent(row) as TextBlock;
            MainWindow.addressId = Convert.ToInt32(customerAddressCell.Text);
            

            // Open Update Customer Window...
            var updateWindow = new UpdateCustomer();
            updateWindow.Show();
        }




        private void btnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dataCustomers.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer to delete.");
                return;
            }

            DataGridRow row = (DataGridRow)dataCustomers.ItemContainerGenerator.ContainerFromItem(dataCustomers.SelectedItem);
            var customerCellContent = dataCustomers.Columns[0].GetCellContent(row) as TextBlock;
            var customerId = Convert.ToInt32(customerCellContent.Text);

            MessageBoxResult result = MessageBox.Show($"Are you sure you wish to delete custoemr ID: {customerId}?", "Confirm delete", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;


            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            using (OdbcCommand deleteCustomer = conn.CreateCommand())
            {
                conn.Open();

                deleteCustomer.CommandText = $"delete from customer WHERE appointmentId = {customerId};";
                deleteCustomer.ExecuteNonQuery();
            }
        }



        #endregion

        private void btnUpdateAppointment_Click_1(object sender, RoutedEventArgs e)
        {
            if (dataAppointments.SelectedItem == null)
            {
                MessageBox.Show("Please select an appointment to update.");
                return;
            }

            DataGridRow row = (DataGridRow)dataAppointments.ItemContainerGenerator.ContainerFromItem(dataAppointments.SelectedItem);
            var appointmentCellContent = dataAppointments.Columns[0].GetCellContent(row) as TextBlock;

            MainWindow.appointmentId = Convert.ToInt32(appointmentCellContent.Text);

            var updateWindow = new UpdateAppointment();
            updateWindow.Show();
        }
    }
}
