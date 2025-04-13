using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportJobRunner
{
    public class AppSettings
    {
        public required string SecretKey { get; set; }
        public required string ConnectionString { get; set; }
        public required string APIPrefix { get; set; }
    }
}
