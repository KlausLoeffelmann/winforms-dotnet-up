using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WinForms;
using WarpToolkit.Microsoft.Extensions.Logging;
using WarpToolkit.WinForms.AppServices.ServiceExtensions;

namespace DotnetUp.WinFormsApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            WinFormsApplicationBuilder builder = WinFormsApplication.CreateBuilder();

            builder.Services.AddWinFormsUserSettingsService();
            builder.Services.AddWinFormsExceptionService();
            builder.Logging.UseWinFormsLogging();

            builder.UseStartupForm<MainForm>()
                .UseHighDpiMode(HighDpiMode.SystemAware)
                .UseColorMode(SystemColorMode.System)
                .UseTextRenderingV2()
                .UseLegacyVisualStyles()
                .UseFormRevealMode(FormRevealMode.Deferred)
                .UseVisualStylesMode(VisualStylesMode.Net11)
                ;

            WinFormsApplication app = builder.Build();
            app.Run();
        }
    }
}
