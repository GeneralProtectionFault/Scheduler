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
        }



        public static event EventHandler AppointmentAdded;



        // LAMBDA =D
        // Very simply function, not worth writing out a full method
        Func<int, int> convert24HourTime = x => x + 12;

        private void btnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            // ******** ADD BUSINESS HOURS CHECK ******************




            bool startIsPM = cmbAddAppointmentStartAMPM.Text == "PM" ? true : false;
            bool endIsPM = cmbAddAppointmentEndAMPM.Text == "PM" ? true : false;

            int enteredStartHour = Convert.ToInt32(cmbAddAppointmentStartHour.Text);
            int enteredEndHour = Convert.ToInt32(cmbAddAppointmentEndHour.Text);

            string startHour = startIsPM ? convert24HourTime(enteredStartHour).ToString("00") : enteredStartHour.ToString("00");
            string endHour = endIsPM ? convert24HourTime(enteredEndHour).ToString("00") : enteredEndHour.ToString("00");


            string startDateString = $"{dateStart.SelectedDate.Value.ToString("yyyy-MM-dd")} {startHour}:{cmbAddAppointmentStartMinute.Text}";
            string endDateString = $"{dateEnd.SelectedDate.Value.ToString("yyyy-MM-dd")} {endHour}:{cmbAddAppointmentEndMinute.Text}";
            DateTime startDate24hr = DateTime.Parse(startDateString);
            DateTime endDate24hr = DateTime.Parse(endDateString);



            OdbcConnection conn = new OdbcConnection(MainWindow.MySQLConnectionString);

            // *** user ID, user name & customer ID stored in static variables in MainWindow
            
            using (conn)
            {
                conn.Open();

                // Check for overlapping appointments
                using (OdbcCommand overlapCheck = conn.CreateCommand())
                {
                    overlapCheck.CommandText = $"select * from appointment WHERE (start between '{dateStart.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}' and '{dateEnd.SelectedDate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")}') OR " +
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
