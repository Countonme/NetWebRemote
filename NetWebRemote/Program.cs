using NetWebRemote.Api.Hubs;
using WebRemote.Core.Interfaces;
using WebRemote.Core.Services;
using WebRemote.Ssh;

namespace NetWebRemote
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // Controllers
            // ==========================================

            builder.Services.AddControllers();

            // ==========================================
            // Razor Pages
            // ==========================================

            builder.Services.AddRazorPages();

            // ==========================================
            // Swagger
            // ==========================================

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ==========================================
            // SignalR
            // ==========================================

            builder.Services.AddSignalR();

            // ==========================================
            // Session Manager
            // ==========================================

            builder.Services.AddSingleton<
                ISessionManager,
                SessionManager>();

            // ==========================================
            // SSH Service
            // ==========================================

            builder.Services.AddSingleton<
                ISessionProvider,
                SshService>();

            // Terminal Output
            builder.Services.AddSingleton<
                        ITerminalOutput,
                        SignalRTerminalOutput>();
            var app = builder.Build();

            // ==========================================
            // Development
            // ==========================================

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI();
            }

            // ==========================================
            // HTTPS
            // ==========================================

            app.UseHttpsRedirection();

            // ==========================================
            // Static Files
            // ==========================================

            app.UseStaticFiles();

            // ==========================================
            // Authorization
            // ==========================================

            app.UseAuthorization();

            // ==========================================
            // Controllers
            // ==========================================

            app.MapControllers();

            // ==========================================
            // Razor Pages
            // ==========================================

            app.MapRazorPages();

            // ==========================================
            // SignalR
            // ==========================================

            app.MapHub<TerminalHub>(
                "/sshTerminal");

            // ==========================================
            // Run
            // ==========================================

            app.Run();
        }
    }
}