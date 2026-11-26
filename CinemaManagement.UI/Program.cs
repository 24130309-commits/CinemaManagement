
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Dapper;
using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;
using CinemaManagement.UI.Forms;
using CinemaManagement.UI.Forms.Auth;
using CinemaManagement.UI.Forms.Catalog;
using CinemaManagement.UI.Forms.Sales;
using CinemaManagement.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CinemaManagement.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            using var provider = BuildServiceProvider();
            InitializeDatabase(provider);

            using var loginForm = provider.GetRequiredService<frmLogin>();
            if (loginForm.ShowDialog() != DialogResult.OK)
                return;

            Application.Run(provider.GetRequiredService<frmMain>());
        }
        catch (Exception ex)
        {
            Trace.TraceError("CinemaManagement startup failure: {0}", ex);
            MessageBox.Show(
                "Không thể khởi động Cinema Management. Hãy kiểm tra cấu hình và cơ sở dữ liệu rồi thử lại.",
                "Lỗi khởi động",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<SqliteConnectionFactory>();
        services.AddSingleton<CurrentUserSession>();
        services.AddSingleton<SalesOrderContext>();
        services.AddTransient<DbInitializer>();
        services.AddTransient<DbSeeder>();
        services.AddTransient<FormFactory>();

        services.AddTransient<UserValidator>();
        services.AddTransient<MovieValidator>();
        services.AddTransient<ShowtimeValidator>();

        services.AddTransient<IUnitOfWork, UnitOfWork>();

        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IMovieRepository, MovieRepository>();
        services.AddTransient<IGenreRepository, GenreRepository>();
        services.AddTransient<IRoomRepository, RoomRepository>();
        services.AddTransient<ISeatRepository, SeatRepository>();
        services.AddTransient<IShowtimeRepository, ShowtimeRepository>();
        services.AddTransient<ICustomerRepository, CustomerRepository>();
        services.AddTransient<IProductRepository, ProductRepository>();
        services.AddTransient<IPromotionRepository, PromotionRepository>();
        services.AddTransient<IPricingRuleRepository, PricingRuleRepository>();
        services.AddTransient<ISettingRepository, SettingRepository>();
        services.AddTransient<IAuditLogRepository, AuditLogRepository>();
        services.AddTransient<IReportRepository, ReportRepository>();
        services.AddTransient<IBookingRepository, BookingRepository>();
        services.AddTransient<ITicketRepository, TicketRepository>();
        services.AddTransient<ISeatHoldRepository, SeatHoldRepository>();
        services.AddTransient<IPaymentRepository, PaymentRepository>();

        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IAuditService, AuditService>();
        services.AddTransient<IGenreService, GenreService>();
        services.AddTransient<IMovieService, MovieService>();
        services.AddTransient<IRoomService, RoomService>();
        services.AddTransient<IShowtimesService, ShowtimeService>();
        services.AddTransient<ICustomerService, CustomerService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<IPromotionService, PromotionService>();
        services.AddTransient<IPricingService, PricingService>();
        services.AddTransient<ISettingService, SettingService>();
        services.AddTransient<IBookingService, BookingService>();
        services.AddTransient<ISeatHoldService, SeatHoldService>();
        services.AddTransient<ISeatMapService, SeatMapService>();
        services.AddTransient<ITicketPrintService, TicketPrintService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IBackupService, BackupService>();
        services.AddTransient<IExportService, ExportService>();
        services.AddTransient<IPaymentService, PaymentService>();

        RegisterForms(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private static void RegisterForms(IServiceCollection services)
    {
        var formTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                (typeof(Form).IsAssignableFrom(type) ||
                 (typeof(UserControl).IsAssignableFrom(type) &&
                  type.Namespace?.StartsWith(
                      "CinemaManagement.UI.Forms",
                      StringComparison.Ordinal) == true &&
                  type.Name.StartsWith("frm", StringComparison.Ordinal))) &&
                type != typeof(frmMovieDetails) &&
                type != typeof(AppMessageDialog));

        foreach (var formType in formTypes)
            services.AddTransient(formType);
    }

    private static void InitializeDatabase(IServiceProvider provider)
    {
        provider.GetRequiredService<DbInitializer>().Initialize();

        VerifyDatabase(provider);
    }

    private static void VerifyDatabase(IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<SqliteConnectionFactory>();

        using var connection = factory.CreateConnection();

        var integrityResult = connection.Query<string>(
            "PRAGMA integrity_check;").FirstOrDefault();

        if (!string.Equals(
            integrityResult,
            "ok",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database không vượt qua integrity_check.\nKết quả: {integrityResult}\nĐường dẫn: {factory.DatabasePath}");
        }

        var foreignKeyErrors = connection.Query(
            "PRAGMA foreign_key_check;").ToList();

        if (foreignKeyErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Database có {foreignKeyErrors.Count} lỗi khóa ngoại.\nĐường dẫn: {factory.DatabasePath}");
        }

        var expectedTables = new[]
        {
            "Users",
            "AuditLogs",
            "Settings",
            "Customers",
            "Genres",
            "Movies",
            "MovieGenres",
            "Rooms",
            "Seats",
            "Showtimes",
            "Products",
            "Promotions",
            "PricingRules",
            "Bookings",
            "Payments",
            "Tickets",
            "BookingProducts",
            "SeatHolds"
        };

        var actualTables = connection.Query<string>(
            """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%';
            """
        ).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingTables = expectedTables
            .Where(table => !actualTables.Contains(table))
            .ToArray();

        if (missingTables.Length > 0)
        {
            throw new InvalidOperationException(
                $"Database thiếu bảng: {string.Join(", ", missingTables)}\nĐường dẫn: {factory.DatabasePath}");
        }

        var expectedIndexes = new[]
        {
            "IX_Payments_BookingId",
            "IX_Payments_Status_PaidAt",
            "UX_Tickets_Active_Showtime_Seat",
            "IX_AuditLogs_User_CreatedAt",
            "IX_Showtimes_StartTime",
            "IX_Showtimes_Room_Start_End",
            "IX_Tickets_Showtime_Status",
            "IX_Bookings_CreatedAt_Status",
            "IX_SeatHolds_ExpiresAt",
            "IX_SeatHolds_SessionId"
        };

        var actualIndexes = connection.Query<string>(
            """
            SELECT name
            FROM sqlite_master
            WHERE type = 'index';
            """
        ).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingIndexes = expectedIndexes
            .Where(index => !actualIndexes.Contains(index))
            .ToArray();

        if (missingIndexes.Length > 0)
        {
            throw new InvalidOperationException(
                $"Database thiếu index: {string.Join(", ", missingIndexes)}\nĐường dẫn: {factory.DatabasePath}");
        }
    }
}
