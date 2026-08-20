using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebRemote.Core.Models;

namespace WebRemote.Core.Interfaces
{
    public interface ISessionProvider
    {
        string Type { get; }

        Task ConnectAsync(
            string connectionId,
            ConnectionInfo connection);

        Task SendAsync(
            string connectionId,
            string data);

        Task DisconnectAsync(
            string connectionId);
    }
}