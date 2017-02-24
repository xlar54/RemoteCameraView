using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteCameraView
{
    public class UpcDatabaseResponse
    {
        public string valid { get; set; }
        public string number { get; set; }
        public string itemname { get; set; }
        public string alias { get; set; }
        public string description { get; set; }
        public string avg_price { get; set; }
        public int rate_up { get; set; }
        public int rate_down { get; set; }

        public string reason { get; set; }
    }
}
