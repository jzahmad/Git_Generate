using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReadMeGenie.Data
{
    public class Request
    {
        public required String Type { get; set; }
        public required String Name { get; set; }
        public required String User { get; set; }
    }
}