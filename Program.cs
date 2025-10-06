using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Audicob.Data;
using Audicob.Models;
using Audicob.Data.SeedData;

namespace Audicob
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args); // Aquí comienza la creación del builder

            // 🔐 Configuración de conexión a PostgreSQL
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Configuración del contexto de la base de datos con PostgreSQL
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // 🧪 Página de errores detallados en desarrollo
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // 🔐 Configuración de Identity con roles y confirmación de cuenta
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false; // Permitir login sin necesidad de confirmar la cuenta
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6; // Longitud mínima de contraseña
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>() // Configura Entity Framework como proveedor de almacenamiento
            .AddDefaultTokenProviders(); // Añadir soporte de generación de tokens

            // 🌐 MVC + Razor Pages
            builder.Services.AddControllersWithViews(); // Configuración de controladores y vistas
            builder.Services.AddRazorPages(); // Configuración de Razor Pages

            // 🔧 Configuración del middleware de la aplicación
            var app = builder.Build();

            // 🌍 Configuración del pipeline HTTP para desarrollo y producción
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint(); // Si el entorno es de desarrollo, habilita las migraciones en el pipeline
            }
            else
            {
                app.UseExceptionHandler("/Home/Error"); // Para manejo de excepciones en producción
                app.UseHsts(); // Seguridad adicional para producción (HTTP Strict Transport Security)
            }

            app.UseHttpsRedirection(); // Redirige las peticiones HTTP a HTTPS
            app.UseStaticFiles(); // Habilita el manejo de archivos estáticos (CSS, JS, imágenes)

            app.UseRouting(); // Habilita el enrutamiento de las rutas

            // 🔐 Activar autenticación y autorización
            app.UseAuthentication();
            app.UseAuthorization();

            // 🧭 Definir rutas principales y personalizadas
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"); // Ruta por defecto

            // Ruta personalizada para la vista de "Cobranza/Cliente"
            app.MapControllerRoute(
                name: "cobranza_cliente",
                pattern: "Cobranza/Cliente",
                defaults: new { controller = "Cobranza", action = "Cliente" }
            );

            app.MapRazorPages(); // Habilita las páginas Razor

            // 🌱 Seed de roles y usuarios iniciales
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    // Inicializa los datos por defecto (roles, usuarios, etc.)
                    await SeedData.InitializeAsync(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Hubo un problema al inicializar los datos de la base de datos.");
                }
            }

            app.Run(); // Ejecuta la aplicación web
        }
    }
}
