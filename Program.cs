using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Audicob.Data;
using Audicob.Models;
using Audicob.Data.SeedData;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace Audicob
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 🔌 DB: PostgreSQL
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Errores detallados de DB (solo dev)
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // 🔐 Identity (usuarios/roles)
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // MVC + Razor Pages
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // 🧾 DinkToPdf: convertidor PDF compartido (para Cobranza y Abono)
            builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));

            var app = builder.Build();

            // 🌍 Pipeline HTTP
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Rutas MVC por defecto
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            // 🌱 Seed inicial (roles/usuarios de prueba, etc.)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    await SeedData.InitializeAsync(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Hubo un problema al inicializar los datos de la base de datos.");
                }
            }

            app.Run();
        }
    }
}
