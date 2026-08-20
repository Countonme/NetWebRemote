using Microsoft.AspNetCore.SignalR;
using WebRemote.Core.Interfaces;

namespace NetWebRemote.Api.Hubs
{
    public class SignalRTerminalOutput : ITerminalOutput
    {
        private readonly IHubContext<TerminalHub> _hubContext;

        public SignalRTerminalOutput(
            IHubContext<TerminalHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendAsync(
            string connectionId,
            string method,
            object? data)
        {
            await _hubContext.Clients
                .Client(connectionId)
                .SendAsync(
                    method,
                    data);
        }
    }
}