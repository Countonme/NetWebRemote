using System.Collections.Concurrent;
using WebRemote.Core.Interfaces;
using WebRemote.Core.Models;

namespace WebRemote.Core.Services
{
    public class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<
            string,
            ISessionProvider> _sessions = new();

        private readonly IEnumerable<ISessionProvider> _providers;

        public SessionManager(
            IEnumerable<ISessionProvider> providers)
        {
            _providers = providers
                ?? throw new ArgumentNullException(nameof(providers));
        }

        public async Task<string> ConnectAsync(
            string connectionId,
            ConnectionInfo connection)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException(
                    "ConnectionId cannot be empty.",
                    nameof(connectionId));

            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            if (string.IsNullOrWhiteSpace(connection.Type))
                connection.Type = "SSH";

            var provider = _providers.FirstOrDefault(
                x => x.Type.Equals(
                    connection.Type,
                    StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new Exception(
                    $"Unsupported connection type: {connection.Type}");
            }

            Console.WriteLine(
                $"[SessionManager] Provider = {provider.Type}");

            Console.WriteLine(
                $"[SessionManager] ConnectionId = {connectionId}");

            Console.WriteLine(
                $"[SessionManager] Target = {connection.Host}:{connection.Port}");

            // 如果已存在
            if (_sessions.TryGetValue(
                connectionId,
                out var oldProvider))
            {
                Console.WriteLine(
                    "[SessionManager] Disconnect old session.");

                try
                {
                    await oldProvider.DisconnectAsync(
                        connectionId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[SessionManager] Disconnect old session failed: {ex.Message}");
                }

                _sessions.TryRemove(
                    connectionId,
                    out _);
            }

            try
            {
                Console.WriteLine(
                    "[SessionManager] >>> Provider.ConnectAsync");

                await provider.ConnectAsync(
                    connectionId,
                    connection);

                Console.WriteLine(
                    "[SessionManager] <<< Provider.ConnectAsync");

                _sessions[connectionId] =
                    provider;

                Console.WriteLine(
                    $"[SessionManager] Session stored. Count={_sessions.Count}");

                return connectionId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[SessionManager] ERROR TYPE: {ex.GetType().FullName}");

                Console.WriteLine(
                    $"[SessionManager] ERROR: {ex.Message}");

                Console.WriteLine(
                    ex.ToString());

                throw;
            }
        }

        public async Task SendAsync(
            string connectionId,
            string data)
        {
            if (!_sessions.TryGetValue(
                connectionId,
                out var provider))
            {
                throw new Exception(
                    "Session not found.");
            }

            await provider.SendAsync(
                connectionId,
                data);
        }

        public async Task DisconnectAsync(
            string connectionId)
        {
            if (!_sessions.TryRemove(
                connectionId,
                out var provider))
            {
                return;
            }

            try
            {
                await provider.DisconnectAsync(
                    connectionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[SessionManager] Disconnect error: {ex.Message}");
            }
        }

        public async Task DisconnectInternalAsync(
            string connectionId)
        {
            await DisconnectAsync(connectionId);
        }
    }
}