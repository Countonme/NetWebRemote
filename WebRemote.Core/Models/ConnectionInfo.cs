using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRemote.Core.Models
{
    public class ConnectionInfo
    {
        public string Type { get; set; } = "SSH";

        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 22;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public int Columns { get; set; } = 120;

        public int Rows { get; set; } = 30;
    }
}