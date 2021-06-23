using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler
{
    public class Customer
    {
        public int customerId { get; set; }
        public int addressId { get; set; }
        public int active { get; set; }
        public string customerName { get; set; }
        public string address { get; set; }
        public string address2 { get; set; }
        public string postalCode { get; set; }
        public string phone { get; set; }
        public DateTime createDate { get; set; }
        public string createdBy { get; set; }
        public DateTime lastUpdate { get; set; }
        public string lastUpdateBy { get; set; }
    }
}
