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
            await AgregarClientesMoraSiNoExisten(db, clienteUser, asesorUser);
        }

        private static async Task AgregarClientesMoraSiNoExisten(ApplicationDbContext db, ApplicationUser clienteUser, ApplicationUser? asesorUser)
        {
            var clientesNuevos = new List<Cliente>();
            
            // Lista de clientes a crear/verificar
            var datosClientes = new[]
            {
                new { Doc = "23456789", Nombre = "Sandra Ruiz Vega", Ingresos = 2800m, Deuda = 1800m },
                new { Doc = "34567890", Nombre = "Roberto Silva Muñoz", Ingresos = 4200m, Deuda = 6500m },
                new { Doc = "45678901", Nombre = "Patricia Herrera Cruz", Ingresos = 3100m, Deuda = 4200m },
                new { Doc = "56789012", Nombre = "Miguel Torres Jiménez", Ingresos = 5000m, Deuda = 8700m },
                new { Doc = "67890123", Nombre = "Carmen Delgado Ramos", Ingresos = 2900m, Deuda = 5600m },
                new { Doc = "78901234", Nombre = "Fernando Castillo Vargas", Ingresos = 4800m, Deuda = 15000m },
                new { Doc = "89012345", Nombre = "Gloria Mendoza Soto", Ingresos = 3300m, Deuda = 12300m },
                new { Doc = "90123456", Nombre = "Andrés Guerrero Lima", Ingresos = 6500m, Deuda = 25000m },
                new { Doc = "01234567", Nombre = "Victoria Peña Moreno", Ingresos = 1800m, Deuda = 7800m }
            };

            foreach (var datos in datosClientes)
            {
                var clienteExiste = await db.Clientes.AnyAsync(c => c.Documento == datos.Doc);
                if (!clienteExiste)
                {
                    var nuevoCliente = new Cliente
                    {
                        Documento = datos.Doc,
                        Nombre = datos.Nombre,
                        IngresosMensuales = datos.Ingresos,
                        DeudaTotal = datos.Deuda,
                        FechaActualizacion = DateTime.UtcNow
                    };
                    clientesNuevos.Add(nuevoCliente);
                }
            }

            if (clientesNuevos.Any())
            {
                db.Clientes.AddRange(clientesNuevos);
                await db.SaveChangesAsync();
                Console.WriteLine($"✅ Se agregaron {clientesNuevos.Count} nuevos clientes");

                // Crear deudas para los nuevos clientes
                await CrearDeudasParaNuevosClientes(db, clientesNuevos);
                
                // Crear pagos para los nuevos clientes  
                await CrearPagosParaNuevosClientes(db, clientesNuevos);

                Console.WriteLine("✅ Nuevos datos de mora creados exitosamente");
            }
            else
            {
                Console.WriteLine("ℹ️ Todos los clientes de mora ya existen");
            }
        }

        private static async Task CrearDeudasParaNuevosClientes(ApplicationDbContext db, List<Cliente> clientes)
        {
            var deudas = new List<Deuda>();
            var diasMora = new[] { -25, -42, -35, -72, -65, -105, -98, -150, -180 };
            var montos = new[] { 1800m, 6500m, 4200m, 8700m, 5600m, 15000m, 12300m, 25000m, 7800m };

            for (int i = 0; i < clientes.Count && i < diasMora.Length; i++)
            {
                var monto = montos[i];
                var intereses = monto * 0.15m;
                var penalidad = intereses * 0.5m;

                deudas.Add(new Deuda
                {
                    ClienteId = clientes[i].Id,
                    Monto = monto,
                    Intereses = intereses,
                    PenalidadCalculada = penalidad,
                    TotalAPagar = monto + intereses + penalidad,
                    FechaVencimiento = DateTime.UtcNow.AddDays(diasMora[i])
                });
            }

            if (deudas.Any())
            {
                db.Deudas.AddRange(deudas);
                await db.SaveChangesAsync();
            }
        }

        private static async Task CrearPagosParaNuevosClientes(ApplicationDbContext db, List<Cliente> clientes)
        {
            if (clientes.Count >= 3)
            {
                var pagos = new List<Pago>
                {
                    new Pago { ClienteId = clientes[0].Id, Fecha = DateTime.UtcNow.AddDays(-20), Monto = 600, Validado = true, Estado = "Cancelado" },
                    new Pago { ClienteId = clientes[1].Id, Fecha = DateTime.UtcNow.AddDays(-50), Monto = 1200, Validado = true, Estado = "Cancelado" },
                    new Pago { ClienteId = clientes[2].Id, Fecha = DateTime.UtcNow.AddDays(-5), Monto = 1500, Validado = false, Estado = "Pendiente" }
                };

                db.Pagos.AddRange(pagos);
                await db.SaveChangesAsync();
            }

                // MORA TEMPRANA (1-29 días)
                var cliente2 = new Cliente
                {
                    Documento = "23456789",
                    Nombre = "Sandra Ruiz Vega",
                    IngresosMensuales = 2800,
                    DeudaTotal = 1800
                };

                // MORA MODERADA (30-59 días)
                var cliente3 = new Cliente
                {
                    Documento = "34567890",
                    Nombre = "Roberto Silva Muñoz",
                    IngresosMensuales = 4200,
                    DeudaTotal = 6500
                };

                var cliente4 = new Cliente
                {
                    Documento = "45678901",
                    Nombre = "Patricia Herrera Cruz",
                    IngresosMensuales = 3100,
                    DeudaTotal = 4200
                };

                // MORA GRAVE (60-89 días)
                var cliente5 = new Cliente
                {
                    Documento = "56789012",
                    Nombre = "Miguel Torres Jiménez",
                    IngresosMensuales = 5000,
                    DeudaTotal = 8700
                };

                var cliente6 = new Cliente
                {
                    Documento = "67890123",
                    Nombre = "Carmen Delgado Ramos",
                    IngresosMensuales = 2900,
                    DeudaTotal = 5600
                };

                // MORA CRÍTICA (90+ días)
                var cliente7 = new Cliente
                {
                    Documento = "78901234",
                    Nombre = "Fernando Castillo Vargas",
                    IngresosMensuales = 4800,
                    DeudaTotal = 15000
                };

                var cliente8 = new Cliente
                {
                    Documento = "89012345",
                    Nombre = "Gloria Mendoza Soto",
                    IngresosMensuales = 3300,
                    DeudaTotal = 12300
                };

                // CASOS EXTREMOS
                var cliente9 = new Cliente
                {
                    Documento = "90123456",
                    Nombre = "Andrés Guerrero Lima",
                    IngresosMensuales = 6500,
                    DeudaTotal = 25000
                };

                var cliente10 = new Cliente
                {
                    Documento = "01234567",
                    Nombre = "Victoria Peña Moreno",
                    IngresosMensuales = 1800,
                    DeudaTotal = 7800
                };

                db.Clientes.AddRange(cliente1, cliente2, cliente3, cliente4, cliente5, 
                                   cliente6, cliente7, cliente8, cliente9, cliente10);
                await db.SaveChangesAsync();

                // DEUDAS CON DIFERENTES NIVELES DE MORA
                var deudas = new List<Deuda>
                {
                    // MORA TEMPRANA - Cliente Demo (15 días)
                    new Deuda
                    {
                        ClienteId = cliente1.Id,
                        Monto = 2500,
                        Intereses = 125,
                        PenalidadCalculada = 62.50m,
                        TotalAPagar = 2687.50m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-15)
                    },
                    // MORA TEMPRANA - Sandra Ruiz (25 días)
                    new Deuda
                    {
                        ClienteId = cliente2.Id,
                        Monto = 1800,
                        Intereses = 108,
                        PenalidadCalculada = 54.00m,
                        TotalAPagar = 1962.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-25)
                    },
                    // MORA MODERADA - Roberto Silva (42 días)
                    new Deuda
                    {
                        ClienteId = cliente3.Id,
                        Monto = 6500,
                        Intereses = 650,
                        PenalidadCalculada = 325.00m,
                        TotalAPagar = 7475.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-42)
                    },
                    // MORA MODERADA - Patricia Herrera (35 días)
                    new Deuda
                    {
                        ClienteId = cliente4.Id,
                        Monto = 4200,
                        Intereses = 504,
                        PenalidadCalculada = 252.00m,
                        TotalAPagar = 4956.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-35)
                    },
                    // MORA GRAVE - Miguel Torres (72 días)
                    new Deuda
                    {
                        ClienteId = cliente5.Id,
                        Monto = 8700,
                        Intereses = 1305,
                        PenalidadCalculada = 652.50m,
                        TotalAPagar = 10657.50m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-72)
                    },
                    // MORA GRAVE - Carmen Delgado (65 días)
                    new Deuda
                    {
                        ClienteId = cliente6.Id,
                        Monto = 5600,
                        Intereses = 896,
                        PenalidadCalculada = 448.00m,
                        TotalAPagar = 6944.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-65)
                    },
                    // MORA CRÍTICA - Fernando Castillo (105 días)
                    new Deuda
                    {
                        ClienteId = cliente7.Id,
                        Monto = 15000,
                        Intereses = 3000,
                        PenalidadCalculada = 1500.00m,
                        TotalAPagar = 19500.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-105)
                    },
                    // MORA CRÍTICA - Gloria Mendoza (98 días)
                    new Deuda
                    {
                        ClienteId = cliente8.Id,
                        Monto = 12300,
                        Intereses = 2214,
                        PenalidadCalculada = 1107.00m,
                        TotalAPagar = 15621.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-98)
                    },
                    // CASOS EXTREMOS - Andrés Guerrero (150 días)
                    new Deuda
                    {
                        ClienteId = cliente9.Id,
                        Monto = 25000,
                        Intereses = 6250,
                        PenalidadCalculada = 3125.00m,
                        TotalAPagar = 34375.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-150)
                    },
                    // CASOS EXTREMOS - Victoria Peña (180 días)
                    new Deuda
                    {
                        ClienteId = cliente10.Id,
                        Monto = 7800,
                        Intereses = 2106,
                        PenalidadCalculada = 1053.00m,
                        TotalAPagar = 10959.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-180)
                    }
                };

                db.Deudas.AddRange(deudas);

                // PAGOS VARIADOS DE EJEMPLO
                db.Pagos.AddRange(
                    // Pagos de clientes con mora temprana
                    new Pago { ClienteId = cliente1.Id, Fecha = DateTime.UtcNow.AddDays(-10), Monto = 800, Validado = true, Estado = "Cancelado", Observacion = "Pago parcial reciente" },
                    new Pago { ClienteId = cliente2.Id, Fecha = DateTime.UtcNow.AddDays(-20), Monto = 600, Validado = true, Estado = "Cancelado", Observacion = "Abono" },
                    
                    // Pagos de clientes con mora moderada
                    new Pago { ClienteId = cliente3.Id, Fecha = DateTime.UtcNow.AddDays(-50), Monto = 1200, Validado = true, Estado = "Cancelado", Observacion = "Pago anterior al vencimiento" },
                    new Pago { ClienteId = cliente4.Id, Fecha = DateTime.UtcNow.AddDays(-45), Monto = 900, Validado = true, Estado = "Cancelado", Observacion = "Último pago registrado" },
                    
                    // Pagos de clientes con mora grave
                    new Pago { ClienteId = cliente5.Id, Fecha = DateTime.UtcNow.AddDays(-80), Monto = 2000, Validado = true, Estado = "Cancelado", Observacion = "Pago significativo anterior" },
                    
                    // Pagos pendientes (no validados)
                    new Pago { ClienteId = cliente6.Id, Fecha = DateTime.UtcNow.AddDays(-5), Monto = 1500, Validado = false, Estado = "Pendiente", Observacion = "Pago pendiente de validación" },
                    new Pago { ClienteId = cliente7.Id, Fecha = DateTime.UtcNow.AddDays(-2), Monto = 3000, Validado = false, Estado = "Pendiente", Observacion = "Pago grande pendiente" },
                    
                    // Casos sin pagos recientes (mora crítica)
                    new Pago { ClienteId = cliente9.Id, Fecha = DateTime.UtcNow.AddDays(-200), Monto = 5000, Validado = true, Estado = "Cancelado", Observacion = "Último pago hace mucho tiempo" }
                );


                        new AsignacionAsesor
                        {
                            AsesorUserId = asesorUser.Id,
                            AsesorNombre = asesorUser.FullName,
                            ClienteId = cliente3.Id,
                            FechaAsignacion = DateTime.UtcNow
                        },
                        new AsignacionAsesor
                        {
                            AsesorUserId = asesorUser.Id,
                            AsesorNombre = asesorUser.FullName,
                            ClienteId = cliente4.Id,
                            FechaAsignacion = DateTime.UtcNow
                        },
                        new AsignacionAsesor
                        {
                            AsesorUserId = asesorUser.Id,
                            AsesorNombre = asesorUser.FullName,
                            ClienteId = cliente5.Id,
                            FechaAsignacion = DateTime.UtcNow
                        }
                    );
                }

                await db.SaveChangesAsync();
                
                Console.WriteLine("✅ Datos expandidos creados exitosamente:");
                Console.WriteLine($"   - 10 Clientes con diferentes niveles de mora");
                Console.WriteLine($"   - 10 Deudas (Temprana: 2, Moderada: 2, Grave: 2, Crítica: 2, Extrema: 2)");
                Console.WriteLine($"   - 8 Pagos variados (validados y pendientes)");
                Console.WriteLine($"   - 3 Evaluaciones");
                Console.WriteLine($"   - 2 Transacciones");
                Console.WriteLine($"   - 5 Asignaciones de asesor");
                Console.WriteLine($"   - Rango de mora: 15 a 180 días");
                Console.WriteLine($"   - Rango de montos: S/ 1,962 a S/ 34,375");
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