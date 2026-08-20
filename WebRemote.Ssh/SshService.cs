using System.Collections.Concurrent;
using System.Text;
using Renci.SshNet;
using WebRemote.Core.Interfaces;
using WebRemote.Core.Models;

namespace WebRemote.Ssh
{
    public class SshService : ISessionProvider
    {
        private readonly ITerminalOutput _output;

        private readonly ConcurrentDictionary<
            string,
            SshSession> _sessions = new();

        public string Type => "SSH";

        public SshService(
            ITerminalOutput output)
        {
            _output = output
                ?? throw new ArgumentNullException(
                    nameof(output));
        }

        // =========================================================
        // Connect
        // =========================================================

        public async Task ConnectAsync(
            string connectionId, WebRemote.Core.Models.ConnectionInfo connection)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentException(
                    "ConnectionId cannot be empty.",
                    nameof(connectionId));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(
                    nameof(connection));
            }

            Console.WriteLine(
                $"[SSH] Connecting {connection.Host}:{connection.Port}");

            // -----------------------------------------------------
            // 如果已经存在旧连接，先清理
            // -----------------------------------------------------

            await DisconnectAsync(
                connectionId);

            SshClient? client = null;

            try
            {
                // -------------------------------------------------
                // 创建 SSH Client
                // -------------------------------------------------

                client = new SshClient(
                    connection.Host,
                    connection.Port,
                    connection.Username,
                    connection.Password);

                client.KeepAliveInterval =
                    TimeSpan.FromSeconds(30);

                // -------------------------------------------------
                // Connect
                // -------------------------------------------------

                client.Connect();

                if (!client.IsConnected)
                {
                    throw new Exception(
                        "SSH connection failed.");
                }

                Console.WriteLine(
                    $"[SSH] Authentication successful: " +
                    $"{connection.Host}:{connection.Port}");

                // -------------------------------------------------
                // 创建 PTY / Shell
                // -------------------------------------------------

                var columns =
                    connection.Columns > 0
                        ? connection.Columns
                        : 120;

                var rows =
                    connection.Rows > 0
                        ? connection.Rows
                        : 30;

                var shell =
                    client.CreateShellStream(
                        terminalName: "xterm",
                        columns: (uint)columns,
                        rows: (uint)rows,
                        width: (uint)(columns * 8),
                        height: (uint)(rows * 16),
                        bufferSize: 8192);

                if (shell == null)
                {
                    throw new Exception(
                        "Failed to create SSH ShellStream.");
                }

                Console.WriteLine(
                    "[SSH] ShellStream created.");

                // -------------------------------------------------
                // Session
                // -------------------------------------------------

                var session =
                    new SshSession
                    {
                        ConnectionId =
                            connectionId,

                        Client =
                            client,

                        Shell =
                            shell
                    };

                if (!_sessions.TryAdd(
                        connectionId,
                        session))
                {
                    shell.Dispose();
                    client.Dispose();

                    throw new Exception(
                        "Failed to register SSH session.");
                }

                // -------------------------------------------------
                // 启动读取线程
                // -------------------------------------------------

                session.ReaderTask =
                    Task.Run(
                        () => ReadOutputAsync(
                            session));

                Console.WriteLine(
                    "[SSH] Shell reader started.");

                // -------------------------------------------------
                // 通知前端
                // -------------------------------------------------

                await _output.SendAsync(
                    connectionId,
                    "Connected",
                    null);

                Console.WriteLine(
                    $"[SSH] Connected successfully: " +
                    $"{connectionId}");
            }
            catch
            {
                try
                {
                    client?.Disconnect();
                }
                catch
                {
                }

                try
                {
                    client?.Dispose();
                }
                catch
                {
                }

                throw;
            }
        }

        // =========================================================
        // Read Shell Output
        // =========================================================

        private async Task ReadOutputAsync(
            SshSession session)
        {
            var buffer =
                new byte[8192];

            try
            {
                while (
                    !session.CancellationTokenSource
                        .IsCancellationRequested)
                {
                    if (!session.Client.IsConnected)
                    {
                        break;
                    }

                    var shell =
                        session.Shell;

                    // ------------------------------------------------
                    // ShellStream DataAvailable
                    // ------------------------------------------------

                    if (shell.DataAvailable)
                    {
                        var count =
                            shell.Read(
                                buffer,
                                0,
                                buffer.Length);

                        if (count > 0)
                        {
                            var text =
                                Encoding.UTF8.GetString(
                                    buffer,
                                    0,
                                    count);

                            Console.WriteLine(
                                $"[SSH] RX {count} bytes");

                            await _output.SendAsync(
                                session.ConnectionId,
                                "Receive",
                                text);
                        }
                    }

                    await Task.Delay(
                        10,
                        session.CancellationTokenSource
                            .Token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常断开
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[SSH] Read error: {ex}");

                try
                {
                    await _output.SendAsync(
                        session.ConnectionId,
                        "Error",
                        ex.Message);
                }
                catch
                {
                }
            }
        }

        // =========================================================
        // Send Input
        // =========================================================

        public async Task SendAsync(
            string connectionId,
            string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            if (!_sessions.TryGetValue(
                    connectionId,
                    out var session))
            {
                throw new Exception(
                    "SSH session not found.");
            }

            if (!session.Client.IsConnected)
            {
                throw new Exception(
                    "SSH connection closed.");
            }

            if (session.Shell == null)
            {
                throw new Exception(
                    "SSH ShellStream is null.");
            }

            try
            {
                var bytes =
                    Encoding.UTF8.GetBytes(
                        data);

                session.Shell.Write(
                    bytes,
                    0,
                    bytes.Length);

                session.Shell.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[SSH] Send error: {ex}");

                throw;
            }

            await Task.CompletedTask;
        }

        // =========================================================
        // Disconnect
        // =========================================================

        public async Task DisconnectAsync(
            string connectionId)
        {
            if (!_sessions.TryRemove(
                    connectionId,
                    out var session))
            {
                return;
            }

            Console.WriteLine(
                $"[SSH] Disconnecting: {connectionId}");

            try
            {
                session.CancellationTokenSource
                    .Cancel();
            }
            catch
            {
            }

            try
            {
                session.Shell.Close();
            }
            catch
            {
            }

            try
            {
                session.Shell.Dispose();
            }
            catch
            {
            }

            try
            {
                if (session.Client.IsConnected)
                {
                    session.Client.Disconnect();
                }
            }
            catch
            {
            }

            try
            {
                session.Client.Dispose();
            }
            catch
            {
            }

            Console.WriteLine(
                $"[SSH] Disconnected: {connectionId}");

            await Task.CompletedTask;
        }
    }
}