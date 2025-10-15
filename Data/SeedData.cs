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

            // AGREGAR CLIENTES ADICIONALES DE MORA (preservando existentes)
            await AgregarClientesMoraSiNoExisten(db, clienteUser, asesorUser, supervisorUser);
            
            // Crear asignaciones de asesores a clientes
            await CrearAsignacionesAsesores(db, asesorUser);
        }

        private static async Task AgregarClientesMoraSiNoExisten(ApplicationDbContext db, ApplicationUser clienteUser, ApplicationUser? asesorUser, ApplicationUser? supervisorUser)
        {
            // Verificar si ya existen clientes base (los originales del proyecto)
            var clienteExiste1 = await db.Clientes.AnyAsync(c => c.Documento == "12345678");
            var clienteExiste2 = await db.Clientes.AnyAsync(c => c.Documento == "87654321");
            var clienteExiste3 = await db.Clientes.AnyAsync(c => c.Documento == "11122233");

            // Si no existen los clientes originales, crearlos primero
            if (!clienteExiste1 || !clienteExiste2 || !clienteExiste3)
            {
                await CrearClientesOriginales(db, clienteUser, asesorUser, supervisorUser);
            }

            // Verificar si existen los nuevos clientes de mora
            var nuevosClientesExisten = await db.Clientes.AnyAsync(c => 
                c.Documento == "23456789" || c.Documento == "34567890" || c.Documento == "45678901");

            if (!nuevosClientesExisten)
            {
                await CrearNuevosClientesMora(db);
            }
        }

        private static async Task CrearClientesOriginales(ApplicationDbContext db, ApplicationUser clienteUser, ApplicationUser? asesorUser, ApplicationUser? supervisorUser)
        {
            var clientesOriginales = new List<Cliente>
            {
                new Cliente
                {
                    Documento = "12345678",
                    Nombre = "Cliente Demo",
                    IngresosMensuales = 3500,
                    DeudaTotal = 2500,
                    EstadoMora = "Al día",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "87654321",
                    Nombre = "María García López",
                    IngresosMensuales = 2200,
                    DeudaTotal = 3800,
                    EstadoMora = "Temprana",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "11122233",
                    Nombre = "Carlos Mendoza Silva",
                    IngresosMensuales = 4500,
                    DeudaTotal = 1200,
                    EstadoMora = "Al día",
                    FechaActualizacion = DateTime.UtcNow
                }
            };

            db.Clientes.AddRange(clientesOriginales);
            await db.SaveChangesAsync();

            // Crear deudas para clientes originales
            var deudasOriginales = new List<Deuda>
            {
                new Deuda
                {
                    ClienteId = clientesOriginales[0].Id,
                    Monto = 2500,
                    Intereses = 50,
                    PenalidadCalculada = 25,
                    TotalAPagar = 2575,
                    FechaVencimiento = DateTime.UtcNow.AddDays(-30)
                },
                new Deuda
                {
                    ClienteId = clientesOriginales[1].Id,
                    Monto = 3800,
                    Intereses = 76,
                    PenalidadCalculada = 38,
                    TotalAPagar = 3914,
                    FechaVencimiento = DateTime.UtcNow.AddDays(-45)
                },
                new Deuda
                {
                    ClienteId = clientesOriginales[2].Id,
                    Monto = 1200,
                    Intereses = 24,
                    PenalidadCalculada = 12,
                    TotalAPagar = 1236,
                    FechaVencimiento = DateTime.UtcNow.AddDays(-10)
                }
            };

            db.Deudas.AddRange(deudasOriginales);

            // Crear pagos para clientes originales
            var pagosOriginales = new List<Pago>
            {
                new Pago 
                { 
                    ClienteId = clientesOriginales[0].Id, 
                    Fecha = DateTime.UtcNow.AddDays(-20), 
                    Monto = 500, 
                    Validado = true, 
                    Estado = "Cancelado", 
                    Observacion = "Pago parcial" 
                },
                new Pago 
                { 
                    ClienteId = clientesOriginales[1].Id, 
                    Fecha = DateTime.UtcNow.AddDays(-50), 
                    Monto = 1000, 
                    Validado = true, 
                    Estado = "Cancelado", 
                    Observacion = "Abono anterior" 
                }
            };

            db.Pagos.AddRange(pagosOriginales);
            await db.SaveChangesAsync();

            Console.WriteLine("✅ Clientes originales restaurados exitosamente");
        }

        private static async Task CrearNuevosClientesMora(ApplicationDbContext db)
        {
            var nuevosClientes = new List<Cliente>
            {
                // MORA TEMPRANA (1-30 días)
                new Cliente
                {
                    Documento = "23456789",
                    Nombre = "Sandra Ruiz Martín",
                    IngresosMensuales = 2800,
                    DeudaTotal = 1800,
                    EstadoMora = "Temprana",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "32145698",
                    Nombre = "Luis Herrera Vega",
                    IngresosMensuales = 3200,
                    DeudaTotal = 2100,
                    EstadoMora = "Temprana",
                    FechaActualizacion = DateTime.UtcNow
                },

                // MORA MODERADA (31-60 días)
                new Cliente
                {
                    Documento = "34567890",
                    Nombre = "Roberto Silva Castro",
                    IngresosMensuales = 4200,
                    DeudaTotal = 6500,
                    EstadoMora = "Moderada",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "45678901",
                    Nombre = "Patricia Herrera Cruz",
                    IngresosMensuales = 3100,
                    DeudaTotal = 4200,
                    EstadoMora = "Moderada",
                    FechaActualizacion = DateTime.UtcNow
                },

                // MORA GRAVE (61-90 días)
                new Cliente
                {
                    Documento = "56789012",
                    Nombre = "Miguel Torres Jiménez",
                    IngresosMensuales = 5000,
                    DeudaTotal = 8700,
                    EstadoMora = "Grave",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "67890123",
                    Nombre = "Carmen Delgado Ramos",
                    IngresosMensuales = 2900,
                    DeudaTotal = 5600,
                    EstadoMora = "Grave",
                    FechaActualizacion = DateTime.UtcNow
                },

                // MORA CRÍTICA (+90 días)
                new Cliente
                {
                    Documento = "78901234",
                    Nombre = "Fernando Castillo Vargas",
                    IngresosMensuales = 4800,
                    DeudaTotal = 15000,
                    EstadoMora = "Crítica",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "89012345",
                    Nombre = "Gloria Mendoza Santos",
                    IngresosMensuales = 3300,
                    DeudaTotal = 12300,
                    EstadoMora = "Crítica",
                    FechaActualizacion = DateTime.UtcNow
                },

                // CASOS EXTREMOS (+120 días)
                new Cliente
                {
                    Documento = "90123456",
                    Nombre = "Andrés Guerrero Lima",
                    IngresosMensuales = 6500,
                    DeudaTotal = 25000,
                    EstadoMora = "Crítica",
                    FechaActualizacion = DateTime.UtcNow
                },
                new Cliente
                {
                    Documento = "01234567",
                    Nombre = "Victoria Peña Moreno",
                    IngresosMensuales = 1800,
                    DeudaTotal = 7800,
                    EstadoMora = "Crítica",
                    FechaActualizacion = DateTime.UtcNow
                }
            };

            db.Clientes.AddRange(nuevosClientes);
            await db.SaveChangesAsync();

            // Crear deudas con diferentes niveles de mora
            var deudas = new List<Deuda>
            {
                // MORA TEMPRANA - Sandra (25 días)
                new Deuda { ClienteId = nuevosClientes[0].Id, Monto = 1800, Intereses = 108, PenalidadCalculada = 54, TotalAPagar = 1962, FechaVencimiento = DateTime.UtcNow.AddDays(-25) },
                // MORA TEMPRANA - Luis (18 días)
                new Deuda { ClienteId = nuevosClientes[1].Id, Monto = 2100, Intereses = 84, PenalidadCalculada = 42, TotalAPagar = 2226, FechaVencimiento = DateTime.UtcNow.AddDays(-18) },

                // MORA MODERADA - Roberto (42 días)
                new Deuda { ClienteId = nuevosClientes[2].Id, Monto = 6500, Intereses = 650, PenalidadCalculada = 325, TotalAPagar = 7475, FechaVencimiento = DateTime.UtcNow.AddDays(-42) },
                // MORA MODERADA - Patricia (35 días)
                new Deuda { ClienteId = nuevosClientes[3].Id, Monto = 4200, Intereses = 504, PenalidadCalculada = 252, TotalAPagar = 4956, FechaVencimiento = DateTime.UtcNow.AddDays(-35) },

                // MORA GRAVE - Miguel (72 días)
                new Deuda { ClienteId = nuevosClientes[4].Id, Monto = 8700, Intereses = 1305, PenalidadCalculada = 652.50m, TotalAPagar = 10657.50m, FechaVencimiento = DateTime.UtcNow.AddDays(-72) },
                // MORA GRAVE - Carmen (65 días)
                new Deuda { ClienteId = nuevosClientes[5].Id, Monto = 5600, Intereses = 896, PenalidadCalculada = 448, TotalAPagar = 6944, FechaVencimiento = DateTime.UtcNow.AddDays(-65) },

                // MORA CRÍTICA - Fernando (105 días)
                new Deuda { ClienteId = nuevosClientes[6].Id, Monto = 15000, Intereses = 3000, PenalidadCalculada = 1500, TotalAPagar = 19500, FechaVencimiento = DateTime.UtcNow.AddDays(-105) },
                // MORA CRÍTICA - Gloria (98 días)
                new Deuda { ClienteId = nuevosClientes[7].Id, Monto = 12300, Intereses = 2214, PenalidadCalculada = 1107, TotalAPagar = 15621, FechaVencimiento = DateTime.UtcNow.AddDays(-98) },

                // CASOS EXTREMOS - Andrés (150 días)
                new Deuda { ClienteId = nuevosClientes[8].Id, Monto = 25000, Intereses = 6250, PenalidadCalculada = 3125, TotalAPagar = 34375, FechaVencimiento = DateTime.UtcNow.AddDays(-150) },
                // CASOS EXTREMOS - Victoria (180 días)
                new Deuda { ClienteId = nuevosClientes[9].Id, Monto = 7800, Intereses = 2106, PenalidadCalculada = 1053, TotalAPagar = 10959, FechaVencimiento = DateTime.UtcNow.AddDays(-180) }
            };

            db.Deudas.AddRange(deudas);

            // Crear pagos variados
            var pagos = new List<Pago>
            {
                // Pagos recientes (mora temprana)
                new Pago { ClienteId = nuevosClientes[0].Id, Fecha = DateTime.UtcNow.AddDays(-20), Monto = 600, Validado = true, Estado = "Cancelado", Observacion = "Abono reciente" },
                new Pago { ClienteId = nuevosClientes[1].Id, Fecha = DateTime.UtcNow.AddDays(-15), Monto = 500, Validado = true, Estado = "Cancelado", Observacion = "Pago parcial" },

                // Pagos anteriores (mora moderada)
                new Pago { ClienteId = nuevosClientes[2].Id, Fecha = DateTime.UtcNow.AddDays(-50), Monto = 1200, Validado = true, Estado = "Cancelado", Observacion = "Pago anterior" },
                new Pago { ClienteId = nuevosClientes[3].Id, Fecha = DateTime.UtcNow.AddDays(-45), Monto = 900, Validado = true, Estado = "Cancelado", Observacion = "Último abono" },

                // Pagos esporádicos (mora grave)
                new Pago { ClienteId = nuevosClientes[4].Id, Fecha = DateTime.UtcNow.AddDays(-80), Monto = 2000, Validado = true, Estado = "Cancelado", Observacion = "Pago significativo anterior" },

                // Pagos pendientes
                new Pago { ClienteId = nuevosClientes[5].Id, Fecha = DateTime.UtcNow.AddDays(-5), Monto = 1500, Validado = false, Estado = "Pendiente", Observacion = "Pendiente validación" },
                new Pago { ClienteId = nuevosClientes[6].Id, Fecha = DateTime.UtcNow.AddDays(-2), Monto = 3000, Validado = false, Estado = "Pendiente", Observacion = "Pago grande pendiente" },

                // Casos sin pagos recientes (extremos)
                new Pago { ClienteId = nuevosClientes[8].Id, Fecha = DateTime.UtcNow.AddDays(-200), Monto = 5000, Validado = true, Estado = "Cancelado", Observacion = "Último pago hace mucho" }
            };

            db.Pagos.AddRange(pagos);
            await db.SaveChangesAsync();

            Console.WriteLine("✅ Nuevos datos de mora creados exitosamente:");
            Console.WriteLine($"   - 10 Clientes adicionales con diferentes niveles de mora");
            Console.WriteLine($"   - 10 Deudas (Temprana: 2, Moderada: 2, Grave: 2, Crítica: 2, Extrema: 2)");
            Console.WriteLine($"   - 8 Pagos variados (validados y pendientes)");
            Console.WriteLine($"   - Rango de mora: 18 a 180 días");
            Console.WriteLine($"   - Rango de montos: S/ 2,226 a S/ 34,375");
        }

        private static async Task CrearAsignacionesAsesores(ApplicationDbContext db, ApplicationUser? asesorUser)
        {
            if (asesorUser == null)
            {
                Console.WriteLine("⚠️ No se pudo crear asignaciones: Asesor no encontrado");
                return;
            }

            // Verificar si ya existen asignaciones
            var asignacionesExisten = await db.AsignacionesAsesores.AnyAsync(a => a.AsesorUserId == asesorUser.Id);
            if (asignacionesExisten)
            {
                Console.WriteLine("ℹ️ Las asignaciones de asesor ya existen");
                return;
            }

            // Obtener clientes para asignar al asesor
            var clientes = await db.Clientes
                .OrderBy(c => c.Id)
                .Take(8) // Asignar los primeros 8 clientes al asesor
                .ToListAsync();

            if (!clientes.Any())
            {
                Console.WriteLine("⚠️ No hay clientes disponibles para asignar");
                return;
            }

            var asignaciones = clientes.Select(cliente => new AsignacionAsesor
            {
                ClienteId = cliente.Id,
                AsesorUserId = asesorUser.Id,
                AsesorNombre = asesorUser.FullName ?? "Asesor de Cobranza",
                FechaAsignacion = DateTime.UtcNow
            }).ToList();

            db.AsignacionesAsesores.AddRange(asignaciones);
            await db.SaveChangesAsync();

            Console.WriteLine($"✅ Asignaciones creadas exitosamente:");
            Console.WriteLine($"   - Asesor: {asesorUser.Email}");
            Console.WriteLine($"   - Clientes asignados: {clientes.Count}");
            foreach (var cliente in clientes)
            {
                Console.WriteLine($"     • {cliente.Nombre} ({cliente.Documento}) - Estado: {cliente.EstadoMora}");
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