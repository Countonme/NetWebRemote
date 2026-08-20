using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRemote.Ssh
{
    public class SshSession
    {
        public string ConnectionId { get; set; }
          = string.Empty;

        public SshClient Client { get; set; }
            = null!;

        public ShellStream Shell { get; set; }
            = null!;

        public CancellationTokenSource
            CancellationTokenSource
        { get; }
            = new();

        public Task? ReaderTask { get; set; }
    }
}