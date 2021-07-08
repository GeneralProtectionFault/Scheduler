using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Scheduler.Windows
{
    /// <summary>
    /// Interaction logic for AddAppointment.xaml
    /// </summary>
    public partial class AddAppointment : Window
    {
        public AddAppointment()
        {
            InitializeComponent();

            // Populate time selection dropdowns
            for (int i = 1; i <= 12; i++)
            {
                cmbAddAppointmentStartHour.Items.Add(i.ToString("00"));
                cmbAddAppointmentEndHour.Items.Add(i.ToString("00"));
            }

            for (int i = 0; i <= 59; i++)
            {
                cmbAddAppointmentStartMinute.Items.Add(i.ToString("00"));
                cmbAddAppointmentEndMinute.Items.Add(i.ToString("00"));
            }

            cmbAddAppointmentStartAMPM.Items.Add("AM");
            cmbAddAppointmentStartAMPM.Items.Add("PM");
            cmbAddAppointmentEndAMPM.Items.Add("AM");
            cmbAddAppointmentEndAMPM.Items.Add("PM");
        }



        public static event EventHandler AppointmentAdded;



        // LAMBDA =D
        // Very simply function, not worth writing out a full method
        Func<int, int> convert24HourTime = x => x + 12;

        private void btnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dateStart.SelectedDate == null || cmbAddAppointmentStartHour.SelectedItem == null || cmbAddAppointmentStartMinute.SelectedItem == null ||cmbAddAppointmentStartAMPM.SelectedItem == null ||
                cmbAddAppointmentEndHour.SelectedItem == null || cmbAddAppointmentEndMinute.SelectedItem == null || cmbAddAppointmentEndAMPM.SelectedItem == null)
            {
                MessageBox.Show("Please select a full date and time for both start and end of appointment.");
                return;
            }

            bool convertStartTo24Hour = cmbAddAppointmentStartAMPM.Text == "PM" && cmbAddAppointmentStartHour.Text != "12" ? true : false;
            bool convertEndTo24Hour = cmbAddAppointmentEndAMPM.Text == "PM" && cmbAddAppointmentEndHour.Text != "12" ? true : false;

            int enteredStartHour = Convert.ToInt32(cmbAddAppointmentStartHour.Text);
            int enteredEndHour = Convert.ToInt32(cmbAddAppointmentEndHour.Text);

            string startHour = convertStartTo24Hour ? convert24HourTime(enteredStartHour).ToString("00") : enteredStartHour.ToString("00");
            string endHour = convertEndTo24Hour ? convert24HourTime(enteredEndHour).ToString("00") : enteredEndHour.ToString("00");
            
            string startDateString = $"{dateStart.SelectedDate.Value.ToString("yyyy-MM-dd")} {startHour}:{cmbAddAppointmentStartMinute.Text}";
            string endDateString = $"{dateEnd.SelectedDate.Value.ToString("yyyy-MM-dd")} {endHour}:{cmbAddAppointmentEndMinute.Text}";
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

            OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString);

            // *** user ID, user name & customer ID stored in static variables in MainWindow
            
            using (conn)
            {
                conn.Open();

                // Check for overlapping appointments
                using (OdbcCommand overlapCheck = conn.CreateCommand())
                {
                    overlapCheck.CommandText = $"select * from appointment WHERE (start between '{startDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}' and '{endDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}') OR " +
                        $"(end between '{dateStart.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}' and '{dateEnd.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}');";

                    OdbcDataReader reader = overlapCheck.ExecuteReader();
                    if (reader.HasRows)
                    {
                        MessageBox.Show("This appointment would conflict with an existing appointment");
                        return;
                    }
                }


                // Add the new appointment
                using (OdbcCommand addAppointment = conn.CreateCommand())
                {
                    addAppointment.CommandText = $"INSERT INTO appointment (customerId, userId, title, description, location, contact, type, url, start, end, createDate, createdBy, lastUpdate, lastUpdateBy)" +
                                                $"VALUES({MainWindow.customerId}, {MainWindow.userId}, '{txtTitle.Text}', '{txtDescription.Text}', '{txtLocation.Text}', '{txtContact.Text}', '{txtType.Text}', '{txtURL.Text}', '{startDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{endDate24hr.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}'," +
                                                $"'{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}', '{DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}', '{MainWindow.userName}');";

                    addAppointment.ExecuteNonQuery();
                }

                // Update the data for the datagrid on the Dash
                AppointmentAdded?.Invoke(this, e);
            }


            Close();
        }

        private void btnCancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
