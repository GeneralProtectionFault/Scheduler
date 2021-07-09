﻿using System;
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
    /// Interaction logic for UpdateAppointment.xaml
    /// </summary>
    public partial class UpdateAppointment : Window
    {
        public static EventHandler AppointmentUpdated;
        
        private Dictionary<string, int> currentCustomers = new Dictionary<string, int>();


        public UpdateAppointment()
        {
            InitializeComponent();

            // Populate time selection dropdowns
            for (int i = 1; i <= 12; i++)
            {
                cmbUpdateAppointmentStartHour.Items.Add(i.ToString("00"));
                cmbUpdateAppointmentEndHour.Items.Add(i.ToString("00"));
            }

            for (int i = 0; i <= 59; i++)
            {
                cmbUpdateAppointmentStartMinute.Items.Add(i.ToString("00"));
                cmbUpdateAppointmentEndMinute.Items.Add(i.ToString("00"));
            }

            cmbUpdateAppointmentStartAMPM.Items.Add("AM");
            cmbUpdateAppointmentStartAMPM.Items.Add("PM");
            cmbUpdateAppointmentEndAMPM.Items.Add("AM");
            cmbUpdateAppointmentEndAMPM.Items.Add("PM");


            // Populate available customers to dropdown
            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            {
                conn.Open();

                using (OdbcCommand populateCustomers = conn.CreateCommand())
                {
                    populateCustomers.CommandText = $"select customerId, customerName from customer;";

                    OdbcDataReader reader = populateCustomers.ExecuteReader();

                    while (reader.Read())
                    {
                        currentCustomers.Add(reader.GetString(1), reader.GetInt32(0));
                        cmbUpdateAppointmentCustomer.Items.Add(reader.GetString(1));
                    }
                }
            }
        }


        // LAMBDA =D
        // Very simply function, not worth writing out a full method
        Func<int, int> convert24HourTime = x => x + 12;
        


        private void btnUpdateConfirm_Click(object sender, RoutedEventArgs e)
        {
            // Ensure customer is selected in dropdown!
            // Ensure end comes after start!
            // Ensure dates and such are filled!
            MainWindow.customerId = currentCustomers[cmbUpdateAppointmentCustomer.Text];

            bool convertStartTo24Hour = cmbUpdateAppointmentStartAMPM.Text == "PM" && cmbUpdateAppointmentStartHour.Text != "12" ? true : false;
            bool convertEndTo24Hour = cmbUpdateAppointmentEndAMPM.Text == "PM" && cmbUpdateAppointmentEndHour.Text != "12" ? true : false;

            int enteredStartHour = Convert.ToInt32(cmbUpdateAppointmentStartHour.Text);
            int enteredEndHour = Convert.ToInt32(cmbUpdateAppointmentEndHour.Text);

            string startHour =  convertStartTo24Hour ? convert24HourTime(enteredStartHour).ToString("00") : enteredStartHour.ToString("00");
            string endHour = convertEndTo24Hour ? convert24HourTime(enteredEndHour).ToString("00") : enteredEndHour.ToString("00");


            string startDateString = $"{dateUpdateAppointmentStart.SelectedDate.Value.ToString("yyyy-MM-dd")} {startHour}:{cmbUpdateAppointmentStartMinute.Text}";
            string endDateString = $"{dateUpdateAppointmentEnd.SelectedDate.Value.ToString("yyyy-MM-dd")} {endHour}:{cmbUpdateAppointmentEndMinute.Text}";
            DateTime startDate24hr = DateTime.Parse(startDateString);
            DateTime endDate24hr = DateTime.Parse(endDateString);

            // Business hours check
            TimeSpan earlyLimit = new TimeSpan(8, 0, 0);
            TimeSpan lateLimit = new TimeSpan(17, 0, 0);
            if (startDate24hr.TimeOfDay < earlyLimit || startDate24hr.TimeOfDay > lateLimit ||
                endDate24hr.TimeOfDay < earlyLimit || endDate24hr.TimeOfDay > lateLimit)
            {
                MessageBox.Show("Please schedule appointment within local business hours: 8 AM - 5 PM.");
                return;
            }

            using (OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString))
            {
                conn.Open();

                // Check for overlapping appointments
                using (OdbcCommand overlapCheck = conn.CreateCommand())
                {
                    overlapCheck.CommandText = $"select * from appointment WHERE ('{startDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}' between start and end) OR " +
                        $"('{endDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}' between start and end);";

                    OdbcDataReader reader = overlapCheck.ExecuteReader();
                    if (reader.HasRows)
                    {
                        MessageBox.Show("This appointment would conflict with an existing appointment");
                        return;
                    }
                }

                using (OdbcCommand updateAppointment = conn.CreateCommand())
                {
                    updateAppointment.CommandText = $"update appointment SET customerId = {MainWindow.customerId}, title = '{txtUpdateAppointmentTitle.Text}', description = '{txtUpdateAppointmentDesc.Text}', location = '{txtUpdateAppointmentLocation.Text}', " +
                        $"contact = '{txtUpdateAppointmentContact.Text}', type = '{txtUpdateAppointmentType.Text}', url = '{txtUpdateAppointmentURL.Text}', start = '{startDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', end = '{endDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', " +
                        $"lastUpdate = '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', lastUpdateBy = '{MainWindow.userName}' WHERE appointmentId = {MainWindow.appointmentId}";

                    updateAppointment.ExecuteNonQuery();
                }
            }

            AppointmentUpdated?.Invoke(this, e);
            Close();
        }




        private void btnUpdateCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        
    }
}
