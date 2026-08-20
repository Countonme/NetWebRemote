using Microsoft.AspNetCore.SignalR;
using WebRemote.Core.Interfaces;
using WebRemote.Core.Models;

namespace NetWebRemote.Api.Hubs
{
    public class TerminalHub : Hub
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<TerminalHub> _logger;

        public TerminalHub(
            ISessionManager sessionManager,
            ILogger<TerminalHub> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public async Task ConnectSsh(WebRemote.Core.Models.ConnectionInfo? connection)
        {
            try
            {
                if (connection == null)
                {
                    throw new HubException(
                        "Connection information cannot be null.");
                }

                if (string.IsNullOrWhiteSpace(connection.Host))
                {
                    throw new HubException(
                        "Host cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(connection.Username))
                {
                    throw new HubException(
                        "Username cannot be empty.");
                }

                if (connection.Port <= 0)
                {
                    throw new HubException(
                        "Invalid SSH port.");
                }

                if (string.IsNullOrWhiteSpace(connection.Type))
                {
                    connection.Type = "SSH";
                }

                _logger.LogInformation(
                    "[SSH] Connect request: {Host}:{Port}, User={User}, Type={Type}, ConnectionId={ConnectionId}",
                    connection.Host,
                    connection.Port,
                    connection.Username,
                    connection.Type,
                    Context.ConnectionId);

                await _sessionManager.ConnectAsync(
                    Context.ConnectionId,
                    connection);
            }
            catch (HubException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SSH] ConnectSsh failed. ConnectionId={ConnectionId}",
                    Context.ConnectionId);

                throw new HubException(
                    $"SSH connection failed: {ex.Message}");
            }
        }

        public async Task SendInput(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return;

                await _sessionManager.SendAsync(
                    Context.ConnectionId,
                    data);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SSH] SendInput failed. ConnectionId={ConnectionId}",
                    Context.ConnectionId);

                throw new HubException(
                    $"Send input failed: {ex.Message}");
            }
        }

        public async Task DisconnectSsh()
        {
            try
            {
                await _sessionManager.DisconnectAsync(
                    Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SSH] Disconnect failed. ConnectionId={ConnectionId}",
                    Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            try
            {
                await _sessionManager
                    .DisconnectInternalAsync(
                        Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[SSH] Internal disconnect failed. ConnectionId={ConnectionId}",
                    Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(
                exception);
        }
    }
}