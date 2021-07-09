using System;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

            // Timer for 15-minute alerts
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Start();

            // For updating the collection whenever an appointment is added
            AddAppointment.AppointmentAdded += UpdateAppointmentsFromEvent;
            AddCustomer.CustomerAdded += UpdateCustomersFromEvent;
            UpdateAppointment.AppointmentUpdated += UpdateAppointmentsFromEvent;
            UpdateCustomer.CustomerUpdated += UpdateCustomersFromEvent;

            if (calendar.SelectedDate == null)
                calendar.SelectedDate = DateTime.Now.Date;

            // Default to the current week on startup
            var query = $"select * from appointment WHERE WEEK(start) = WEEK('{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}');";
            // Start/default to all appointments
            CreateDataCollection(query);
            CreateCustomerCollection();

            cmbReports.Items.Add("Appointment Types");
            cmbReports.Items.Add("Consultant Schedules");
            cmbReports.Items.Add("Past/Expired Appointments");
        }


        /// <summary>
        /// Check for a meeting that might come up in the next 15 minutes!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Timer tick!!!");

            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            using (OdbcCommand reminderCheck = conn.CreateCommand())
            {
                conn.Open();

                var queryString = $"select * from appointment WHERE userId = {MainWindow.userId} AND TIMESTAMPDIFF(MINUTE,'{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}',start) between 0 and 15;";
                Debug.WriteLine(queryString);
                reminderCheck.CommandText = queryString;

                using (OdbcDataReader reader = reminderCheck.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return;

                    StringBuilder messageText = new StringBuilder();
                    messageText.Append("You have the following appointments soon:\n\n");

                    while(reader.Read())
                    {
                        messageText.Append($"{reader.GetString(3)}, {reader.GetString(4)}, {reader.GetString(5)}, {reader.GetString(6)}, {reader.GetString(7)}, {reader.GetString(8)}, {reader.GetDateTime(9).ToLocalTime().ToString()} - {reader.GetDateTime(10).ToLocalTime().ToString()}\n");
                    }

                    MessageBox.Show(messageText.ToString());
                }
            }
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

                    populateCustomers.CommandText = @"select c.customerId, c.addressId, c.active, c.customerName, a.address, a.address2, ct.city, a.postalCode, a.phone, c.createDate, c.createdBy, c.lastUpdate, c.lastUpdateBy
                                                        from customer c
                                                        JOIN address a on c.addressId = a.addressId
                                                        JOIN city ct on a.cityId = ct.cityId;";

                    OdbcDataReader reader = populateCustomers.ExecuteReader();

                    if (reader.HasRows)
                    {
                        while(reader.Read())
                        {
                            dashModel.customers.Add(
                                new Customer()
                                {
                                    customerId = reader.GetInt32(0),
                                    addressId = reader.GetInt32(1),
                                    active = reader.GetInt32(2),
                                    customerName = reader.GetString(3),
                                    address = reader.GetString(4),
                                    address2 = reader.GetString(5),
                                    city = reader.GetString(6),
                                    postalCode = reader.GetString(7),
                                    phone = reader.GetString(8),
                                    createDate = reader.GetDateTime(9).ToLocalTime(),
                                    createdBy = reader.GetString(10),
                                    lastUpdate = reader.GetDateTime(11).ToLocalTime(),
                                    lastUpdateBy = reader.GetString(12)
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
            if (radWeek.IsChecked == true)
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

            MainWindow.appointmentId = Convert.ToInt32(appointmentCellContent.Text);
            var updateWindow = new UpdateAppointment();
            updateWindow.Show();
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


            try
            {
                OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString);

                using (conn)
                using (OdbcCommand deleteAppointment = conn.CreateCommand())
                {
                    conn.Open();

                    deleteAppointment.CommandText = $"delete from appointment WHERE appointmentId = {appointmentId};";
                    deleteAppointment.ExecuteNonQuery();
                }

                UpdateDates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error - {ex.Message}");
            }
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

            var customerAddressCell = dataCustomers.Columns[1].GetCellContent(row) as TextBlock;
            MainWindow.addressId = Convert.ToInt32(customerAddressCell.Text);
            

            // Open Update Customer Window...
            var updateWindow = new UpdateCustomer();
            updateWindow.Show();
        }




        async private void btnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dataCustomers.SelectedItem == null)
                {
                    MessageBox.Show("Please select a customer to delete.");
                    return;
                }

                DataGridRow row = (DataGridRow)dataCustomers.ItemContainerGenerator.ContainerFromItem(dataCustomers.SelectedItem);
                var customerCellContent = dataCustomers.Columns[0].GetCellContent(row) as TextBlock;
                var customerId = Convert.ToInt32(customerCellContent.Text);

                MessageBoxResult result = MessageBox.Show($"Are you sure you wish to delete customer ID: {customerId}?\nAll appointments for this customer will also be deleted.", "Confirm delete", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;


                using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
                {
                    conn.Open();

                    // First, delete all appointments for this customer to prevent foreign key violations

                    await Task.Run(() =>
                    {
                        using (OdbcCommand deleteCustomerAppointments = conn.CreateCommand())
                        {
                            deleteCustomerAppointments.CommandText = $"delete from appointment WHERE customerId = {customerId};";
                            deleteCustomerAppointments.ExecuteNonQuery();
                        }
                    });


                    await Task.Run(() =>
                    {
                        using (OdbcCommand deleteCustomer = conn.CreateCommand())
                        {
                            deleteCustomer.CommandText = $"delete from customer WHERE customerId = {customerId};";
                            deleteCustomer.ExecuteNonQuery();
                        }
                    });
                }

                UpdateDates();
                CreateCustomerCollection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: \n\n {ex.Message}");
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


        /// <summary>
        /// Show the report corresponding to the selected item in the nearby dropdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            if (cmbReports.SelectedItem == null)
            {
                MessageBox.Show("Please select a report from the dropdown.");
                return;
            }


            try
            {

                using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
                {
                    conn.Open();

                    if (cmbReports.SelectedItem.ToString() == "Appointment Types")
                    {
                        var selectedMonth = calendar.SelectedDate.Value.Month;

                        using (OdbcCommand appointmentTypesMonth = conn.CreateCommand())
                        {
                            appointmentTypesMonth.CommandText = $"select distinct type from appointment WHERE MONTH(start) = {selectedMonth};";

                            StringBuilder appointmentTypes = new StringBuilder();

                            using (OdbcDataReader reader = appointmentTypesMonth.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    MessageBox.Show("There are no appointments in the selected month");
                                    return;
                                }

                                appointmentTypes.Append("There are the following appointment types for the selected month:\n\n");

                                while (reader.Read())
                                {
                                    appointmentTypes.Append($"{reader.GetString(0)}\n");
                                }

                                MessageBox.Show(appointmentTypes.ToString());
                            }
                        }
                    }
                    else if (cmbReports.SelectedItem.ToString() == "Consultant Schedules")
                    {
                        using (OdbcCommand consultantSchedules = conn.CreateCommand())
                        {
                            consultantSchedules.CommandText = $"select userName, a.* from appointment a " +
                                $"JOIN user u on a.userId = u.userId " +
                                $"WHERE start >= '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}' " +
                                $"ORDER BY u.userName;";

                            using (OdbcDataReader reader = consultantSchedules.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    MessageBox.Show("No unexpired appointments");
                                    return;
                                }

                                StringBuilder consultantAppointments = new StringBuilder();
                                consultantAppointments.Append("Following is a list of future appointments for all users:\n--------------------------------------------------------\n\n");


                                var counter = 0;
                                var currentUser = "";

                                while (reader.Read())
                                {
                                    // First row
                                    if (counter == 0)
                                    {
                                        currentUser = reader.GetString(0);
                                        consultantAppointments.Append($"{reader.GetString(0)}:\n\n");
                                    }
                                    // If we've reached a new user, display the name and separate to show their appointments
                                    else if (currentUser != reader.GetString(0))
                                    {
                                        currentUser = reader.GetString(0);
                                        consultantAppointments.Append($"{reader.GetString(0)}:\n\n");
                                    }

                                    consultantAppointments.Append($"{reader.GetString(4)}, {reader.GetString(5)}: {reader.GetDateTime(10).ToLocalTime().ToString()}\n");
                                    counter++;
                                }

                                MessageBox.Show(consultantAppointments.ToString());
                            }
                        }
                    }
                    else if (cmbReports.SelectedItem.ToString() == "Past/Expired Appointments")
                    {
                        using (OdbcCommand expiredAppointments = conn.CreateCommand())
                        {
                            expiredAppointments.CommandText = $"select * from appointment WHERE end < '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}';";

                            using (OdbcDataReader reader = expiredAppointments.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    MessageBox.Show("There are no expired appointments.");
                                    return;
                                }

                                StringBuilder expired = new StringBuilder();
                                expired.Append("The following appointments have expired and you may wish to delete:\n\n");

                                while (reader.Read())
                                {
                                    expired.Append($"{reader.GetString(3)}, {reader.GetString(4)}: {reader.GetDateTime(9).ToLocalTime()} - {reader.GetDateTime(10).ToLocalTime()}\n\n");
                                }

                                MessageBox.Show(expired.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error - {ex.Message}");
            }
        }
    }
}
