using Audicob.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Data.SeedData
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Crear roles si no existen
            string[] roles = { "Administrador", "Supervisor", "AsesorCobranza", "Cliente" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var identityRole = new IdentityRole(role);
                    var roleResult = await roleManager.CreateAsync(identityRole);
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($"Error al crear el rol {role}: {error.Description}");
                        }
                    }
                }
            }

            // Crear usuarios de prueba si no existen
            var adminUser = await CreateUserAsync(userManager, "admin@audicob.com", "Admin123!", "Administrador", "Administrador General");
            var supervisorUser = await CreateUserAsync(userManager, "supervisor@audicob.com", "Supervisor123!", "Supervisor", "Supervisor Principal");
            var asesorUser = await CreateUserAsync(userManager, "asesor@audicob.com", "Asesor123!", "AsesorCobranza", "Asesor de Cobranza");
            var clienteUser = await CreateUserAsync(userManager, "cliente@audicob.com", "Cliente123!", "Cliente", "Cliente Demo");

            // Insertar clientes de ejemplo si no existen
            if (!await db.Clientes.AnyAsync())
            {
                // Cliente vinculado con usuario
                var cliente1 = new Cliente
                {
                    UserId = clienteUser.Id,
                    Documento = "12345678",
                    Nombre = "Cliente Demo",
                    IngresosMensuales = 2500,
                    DeudaTotal = 1200
                };

                var cliente2 = new Cliente
                {
                    Documento = "87654321",
                    Nombre = "María López",
                    IngresosMensuales = 3200,
                    DeudaTotal = 800
                };

                var cliente3 = new Cliente
                {
                    Documento = "11223344",
                    Nombre = "Carlos Ruiz",
                    IngresosMensuales = 2800,
                    DeudaTotal = 1500
                };

                db.Clientes.AddRange(cliente1, cliente2, cliente3);
                await db.SaveChangesAsync();

                // Insertar deuda para cliente1
                var deuda1 = new Deuda
                {
                    ClienteId = cliente1.Id,
                    Monto = 1200,
                    Intereses = 0,
                    PenalidadCalculada = 0,
                    TotalAPagar = 1200,
                    FechaVencimiento = DateTime.UtcNow.AddDays(-30)
                };

                var deuda2 = new Deuda
                {
                    ClienteId = cliente2.Id,
                    Monto = 800,
                    Intereses = 0,
                    PenalidadCalculada = 0,
                    TotalAPagar = 800,
                    FechaVencimiento = DateTime.UtcNow.AddDays(-15)
                };

                var deuda3 = new Deuda
                {
                    ClienteId = cliente3.Id,
                    Monto = 1500,
                    Intereses = 0,
                    PenalidadCalculada = 0,
                    TotalAPagar = 1500,
                    FechaVencimiento = DateTime.UtcNow.AddDays(-45)
                };

                db.Deudas.AddRange(deuda1, deuda2, deuda3);

                // Insertar pagos de ejemplo
                db.Pagos.AddRange(
                    new Pago { ClienteId = cliente1.Id, Fecha = DateTime.UtcNow.AddMonths(-1), Monto = 200, Validado = true, Estado = "Cancelado" },
                    new Pago { ClienteId = cliente1.Id, Fecha = DateTime.UtcNow.AddMonths(-3), Monto = 150, Validado = false, Estado = "Pendiente" },
                    new Pago { ClienteId = cliente2.Id, Fecha = DateTime.UtcNow.AddMonths(-2), Monto = 300, Validado = true, Estado = "Cancelado" },
                    new Pago { ClienteId = cliente3.Id, Fecha = DateTime.UtcNow.AddDays(-5), Monto = 500, Validado = false, Estado = "Pendiente" }
                );

                // Insertar evaluaciones de ejemplo
                db.Evaluaciones.AddRange(
                    new EvaluacionCliente { ClienteId = cliente1.Id, Estado = "Pendiente", Responsable = "Sistema" },
                    new EvaluacionCliente { ClienteId = cliente2.Id, Estado = "Pendiente", Responsable = "Sistema" },
                    new EvaluacionCliente { ClienteId = cliente3.Id, Estado = "Marcado", Responsable = supervisorUser.FullName, Comentario = "Cliente con buen historial", Fecha = DateTime.UtcNow.AddDays(-10) }
                );

                // NUEVO: Insertar transacciones de ejemplo
                db.Transacciones.AddRange(
                    new Transaccion
                    {
                        ClienteId = cliente1.Id,
                        NumeroTransaccion = "TRX-001",
                        Fecha = DateTime.UtcNow.AddMonths(-1),
                        Monto = 200,
                        Estado = "Completado",
                        Descripcion = "Pago mensual",
                        MetodoPago = "Transferencia bancaria"
                    },
                    new Transaccion
                    {
                        ClienteId = cliente1.Id,
                        NumeroTransaccion = "TRX-002",
                        Fecha = DateTime.UtcNow.AddDays(-15),
                        Monto = 150,
                        Estado = "Completado",
                        Descripcion = "Abono parcial",
                        MetodoPago = "Efectivo"
                    }
                );

                // NUEVO: Asignar clientes al asesor
                if (asesorUser != null)
                {
                    db.AsignacionesAsesores.AddRange(
                        new AsignacionAsesor
                        {
                            AsesorUserId = asesorUser.Id,
                            AsesorNombre = asesorUser.FullName,
                            ClienteId = cliente1.Id,
                            FechaAsignacion = DateTime.UtcNow
                        },
                        new AsignacionAsesor
                        {
                            AsesorUserId = asesorUser.Id,
                            AsesorNombre = asesorUser.FullName,
                            ClienteId = cliente2.Id,
                            FechaAsignacion = DateTime.UtcNow
                        },
                        new AsignacionAsesor
                        {
                            AsesorUserId = asesorUser.Id,
                            AsesorNombre = asesorUser.FullName,
                            ClienteId = cliente3.Id,
                            FechaAsignacion = DateTime.UtcNow
                        }
                    );
                }

                await db.SaveChangesAsync();
                
                Console.WriteLine("✅ Datos de ejemplo creados exitosamente:");
                Console.WriteLine($"   - 3 Clientes");
                Console.WriteLine($"   - 3 Deudas");
                Console.WriteLine($"   - 4 Pagos");
                Console.WriteLine($"   - 3 Evaluaciones");
                Console.WriteLine($"   - 2 Transacciones");
                Console.WriteLine($"   - 3 Asignaciones de asesor");
            }
        }

        private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string role, string fullName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    Console.WriteLine($"✅ Usuario creado: {email} - Rol: {role}");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"❌ Error al crear el usuario {email}: {error.Description}");
                    }
                }
            }
            return user;
        }
    }
}