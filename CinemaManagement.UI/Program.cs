using Microsoft.Extensions.DependencyInjection;
using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;
using CinemaManagement.UI.Forms.Auth;
using CinemaManagement.UI.Forms.Main;
using CinemaManagement.UI.Forms.Management;
using CinemaManagement.UI.Forms.Reports;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        try
        {
            provider.GetRequiredService<DbInitializer>().Initialize();
            provider.GetRequiredService<DbSeeder>().SeedIfEmpty();

            while (true)
            {
                using var login = provider.GetRequiredService<frmLogin>();

                if (login.ShowDialog() != DialogResult.OK)
                    break;

                using var main = provider.GetRequiredService<frmMain>();

                Application.Run(main);

                if (!main.LogoutRequested)
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Error(
                "Không thể khởi động Cinema Management.\n" + ex.Message,
                "Lỗi khởi động");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<DbInitializer>();
        services.AddSingleton<DbSeeder>();
        services.AddTransient<UnitOfWork>();

        services.AddSingleton<CurrentUserSession>();
        services.AddSingleton<UserValidator>();

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IAuditLogRepository, AuditLogRepository>();
        services.AddSingleton<ISettingRepository, SettingRepository>();
        services.AddSingleton<IReportRepository, ReportRepository>();

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IAuditService, AuditService>();
        services.AddSingleton<ISettingService, SettingService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IExportService, ExportService>();

        services.AddSingleton<FormFactory>();

        services.AddTransient<frmLogin>();
        services.AddTransient<frmMain>();
        services.AddTransient<frmChangePassword>();
        services.AddTransient<frmDashboard>();
        services.AddTransient<frmUserManage>();
        services.AddTransient<frmSettings>();
        services.AddTransient<frmRevenueReport>();
        services.AddTransient<frmOccupancyReport>();
    }
}