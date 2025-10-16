using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Asesor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Controllers
{
    [Authorize(Roles = "AsesorCobranza")]
    public class AsesorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AsesorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Validación básica
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    TempData["Error"] = "Usuario no autenticado";
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "No se pudo obtener la información del usuario.";
                    return RedirectToAction("Login", "Account");
                }

                // Crear ViewModel con valores por defecto seguros
                var vm = new AsesorDashboardViewModel
                {
                    TotalClientesAsignados = 0,
                    TotalDeudaCartera = 0,
                    TotalPagosRecientes = 0,
                    Clientes = new List<string>(),
                    DeudasPorCliente = new List<decimal>()
                };

                // Intentar cargar datos de forma segura
                try
                {
                    var asignaciones = await _db.AsignacionesAsesores
                        .Include(a => a.Cliente)
                        .Where(a => a.AsesorUserId == user.Id)
                        .ToListAsync();

                    if (asignaciones != null && asignaciones.Any())
                    {
                        vm.TotalClientesAsignados = asignaciones.Count;
                        
                        // Calcular deuda total de forma segura
                        vm.TotalDeudaCartera = 0;
                        vm.Clientes = new List<string>();
                        vm.DeudasPorCliente = new List<decimal>();

                        foreach (var asignacion in asignaciones)
                        {
                            if (asignacion.Cliente != null)
                            {
                                vm.TotalDeudaCartera += asignacion.Cliente.DeudaTotal;
                                vm.Clientes.Add(asignacion.Cliente.Nombre ?? "Sin nombre");
                                vm.DeudasPorCliente.Add(asignacion.Cliente.DeudaTotal);
                            }
                        }

                        // Consultar pagos recientes de forma segura
                        try
                        {
                            var clienteIds = asignaciones.Select(a => a.ClienteId).ToList();
                            var fechaLimite = DateTime.UtcNow.AddMonths(-1);
                            
                            var pagoRecientes = await _db.Pagos
                                .Where(p => clienteIds.Contains(p.ClienteId) && p.Fecha >= fechaLimite)
                                .ToListAsync();
                            
                            vm.TotalPagosRecientes = pagoRecientes.Sum(p => p.Monto);
                        }
                        catch
                        {
                            // Si falla la consulta de pagos, usar 0
                            vm.TotalPagosRecientes = 0;
                        }
                    }
                    else
                    {
                        ViewBag.InfoMessage = "No tienes clientes asignados actualmente.";
                    }
                }
                catch (Exception dataEx)
                {
                    ViewBag.WarningMessage = $"Error al cargar algunos datos: {dataEx.Message}";
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error en Dashboard: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // HU-30: Cambiar el estado de morosidad

        /// <summary>
        /// Muestra la vista para seleccionar un cliente y cambiar su estado de morosidad
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CambiarEstadoMora(int? clienteId)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    TempData["Error"] = "Usuario no autenticado";
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "No se pudo obtener la información del usuario.";
                    return RedirectToAction("Login", "Account");
                }

                // Verificar rol de forma segura
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("AsesorCobranza"))
                {
                    TempData["Error"] = $"No tienes permisos para acceder a esta funcionalidad.";
                    return RedirectToAction("Index", "Home");
                }

                // Obtener clientes asignados al asesor
                var clientesAsignados = new List<Cliente>();
                
                try
                {
                    var asignaciones = await _db.AsignacionesAsesores
                        .Include(a => a.Cliente)
                        .Where(a => a.AsesorUserId == user.Id)
                        .ToListAsync();

                    clientesAsignados = asignaciones
                        .Where(a => a.Cliente != null)
                        .Select(a => a.Cliente!)
                        .ToList();
                }
                catch (Exception dataEx)
                {
                    TempData["Warning"] = $"Error al cargar clientes: {dataEx.Message}";
                }

                var vm = new CambiarEstadoMoraViewModel();

                if (clienteId.HasValue && clientesAsignados.Any())
                {
                    var cliente = clientesAsignados.FirstOrDefault(c => c.Id == clienteId.Value);
                    if (cliente != null)
                    {
                        vm.ClienteId = cliente.Id;
                        vm.ClienteNombre = cliente.Nombre;
                        vm.ClienteDocumento = cliente.Documento;
                        vm.EstadoActual = cliente.EstadoMora;
                    }
                }

                ViewBag.ClientesAsignados = clientesAsignados;
                
                if (!clientesAsignados.Any())
                {
                    ViewBag.InfoMessage = "No tienes clientes asignados para gestionar.";
                }

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error en CambiarEstadoMora: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Procesa el cambio de estado de morosidad
        /// Criterio de Aceptación 1, 2: Cambiar estado y guardarlo en historial
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoMora(CambiarEstadoMoraViewModel modelo)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Challenge();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ModelState.AddModelError("", "No se pudo obtener la información del usuario. Por favor, inicie sesión nuevamente.");
                    return await CargarDatosFormulario(modelo);
                }

                if (!User.IsInRole("AsesorCobranza"))
                {
                    ModelState.AddModelError("", "No tienes permisos para realizar esta acción.");
                    return await CargarDatosFormulario(modelo);
                }
                
                // Validar que el cliente esté asignado al asesor
                var asignacion = await _db.AsignacionesAsesores
                    .Include(a => a.Cliente)
                    .FirstOrDefaultAsync(a => a.AsesorUserId == user.Id && a.ClienteId == modelo.ClienteId);

                if (asignacion == null)
                {
                    ModelState.AddModelError("", "No tienes permisos para modificar este cliente.");
                    return await CargarDatosFormulario(modelo);
                }

                var cliente = asignacion.Cliente;

                // Validar el cambio de estado
                modelo.EstadoActual = cliente.EstadoMora;
                var errores = modelo.ValidarCambioEstado();
                
                if (errores.Any())
                {
                    foreach (var error in errores)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return await CargarDatosFormulario(modelo);
                }

                if (!ModelState.IsValid)
                {
                    return await CargarDatosFormulario(modelo);
                }

                // Crear registro en el historial antes de cambiar el estado
                var historial = new HistorialEstadoMora
                {
                    ClienteId = cliente.Id,
                    EstadoAnterior = cliente.EstadoMora,
                    NuevoEstado = modelo.NuevoEstado,
                    UsuarioId = user.Id,
                    FechaCambio = DateTime.UtcNow,
                    MotivoCambio = modelo.MotivoCambio,
                    Observaciones = modelo.Observaciones,
                    DireccionIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Desconocida",
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                // Cambiar el estado del cliente
                cliente.EstadoMora = modelo.NuevoEstado;
                cliente.FechaActualizacion = DateTime.UtcNow;

                // Guardar cambios en la base de datos
                _db.HistorialEstadosMora.Add(historial);
                _db.Clientes.Update(cliente);
                await _db.SaveChangesAsync();

                // Criterio de Aceptación 3: Enviar notificación si está habilitado
                if (modelo.EnviarNotificacion)
                {
                    await EnviarNotificacionCambioEstado(cliente, historial);
                }

                TempData["Success"] = $"Estado de morosidad cambiado exitosamente de '{historial.EstadoAnterior}' a '{historial.NuevoEstado}' para el cliente {cliente.Nombre}.";
                
                return RedirectToAction("VerHistorialEstado", new { clienteId = cliente.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al cambiar el estado: {ex.Message}");
                return await CargarDatosFormulario(modelo);
            }
        }

        /// <summary>
        /// Muestra el historial de cambios de estado de un cliente
        /// Criterio de Aceptación 5: El cambio debe ser visible en el historial
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerHistorialEstado(int clienteId)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Challenge();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "No se pudo obtener la información del usuario. Por favor, inicie sesión nuevamente.";
                return RedirectToAction("Login", "Account");
            }

            if (!User.IsInRole("AsesorCobranza"))
            {
                TempData["Error"] = "No tienes permisos para acceder a esta funcionalidad.";
                return RedirectToAction("Index", "Home");
            }
            
            // Verificar que el cliente esté asignado al asesor
            var asignacion = await _db.AsignacionesAsesores
                .Include(a => a.Cliente)
                .FirstOrDefaultAsync(a => a.AsesorUserId == user.Id && a.ClienteId == clienteId);

            if (asignacion == null)
            {
                TempData["Error"] = "No tienes permisos para ver el historial de este cliente.";
                return RedirectToAction("Dashboard");
            }

            var cliente = asignacion.Cliente;

            // Obtener historial de cambios de estado
            var historial = await _db.HistorialEstadosMora
                .Include(h => h.Usuario)
                .Where(h => h.ClienteId == clienteId)
                .OrderByDescending(h => h.FechaCambio)
                .ToListAsync();

            var vm = new HistorialEstadoMoraViewModel
            {
                ClienteId = cliente.Id,
                ClienteNombre = cliente.Nombre,
                ClienteDocumento = cliente.Documento,
                EstadoActual = cliente.EstadoMora,
                TotalCambios = historial.Count,
                FechaUltimoCambio = historial.FirstOrDefault()?.FechaCambio,
                Cambios = historial.Select(h => new HistorialCambioViewModel
                {
                    Id = h.Id,
                    EstadoAnterior = h.EstadoAnterior,
                    NuevoEstado = h.NuevoEstado,
                    FechaCambio = h.FechaCambio,
                    UsuarioNombre = h.Usuario?.UserName ?? "Usuario desconocido",
                    MotivoCambio = h.MotivoCambio,
                    Observaciones = h.Observaciones
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Obtiene los datos del cliente por AJAX
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerDatosCliente(int clienteId)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "No se pudo obtener la información del usuario" });
            }

            if (!User.IsInRole("AsesorCobranza"))
            {
                return Json(new { success = false, message = "No tienes permisos para realizar esta acción" });
            }
            
            var cliente = await _db.AsignacionesAsesores
                .Include(a => a.Cliente)
                .Where(a => a.AsesorUserId == user.Id && a.ClienteId == clienteId)
                .Select(a => a.Cliente)
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return Json(new { success = false, message = "Cliente no encontrado o no asignado" });
            }

            return Json(new 
            { 
                success = true, 
                cliente = new 
                {
                    id = cliente.Id,
                    nombre = cliente.Nombre,
                    documento = cliente.Documento,
                    estadoActual = cliente.EstadoMora,
                    deudaTotal = cliente.DeudaTotal,
                    fechaActualizacion = cliente.FechaActualizacion.ToString("dd/MM/yyyy")
                }
            });
        }

        // Métodos auxiliares privados

        /// <summary>
        /// Carga los datos necesarios para el formulario
        /// </summary>
        private async Task<IActionResult> CargarDatosFormulario(CambiarEstadoMoraViewModel modelo)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Challenge();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }
            
            var clientesAsignados = await _db.AsignacionesAsesores
                .Include(a => a.Cliente)
                .Where(a => a.AsesorUserId == user.Id)
                .Select(a => a.Cliente)
                .ToListAsync();

            ViewBag.ClientesAsignados = clientesAsignados;
            return View(modelo);
        }

        /// <summary>
        /// Envía notificación al cliente sobre el cambio de estado
        /// Criterio de Aceptación 3: Notificación automática al cliente
        /// </summary>
        private async Task EnviarNotificacionCambioEstado(Cliente cliente, HistorialEstadoMora historial)
        {
            try
            {
                // Aquí se implementaría la lógica de notificación
                // Por ejemplo: email, SMS, notificación push, etc.
                
                // Simulación de envío de notificación
                // En una implementación real, aquí iría la integración con:
                // - Servicio de email (SendGrid, AWS SES, etc.)
                // - Servicio de SMS (Twilio, etc.)
                // - Sistema de notificaciones push

                var mensaje = $"Estimado {cliente.Nombre}, " +
                             $"su estado de morosidad ha sido actualizado de '{historial.EstadoAnterior}' " +
                             $"a '{historial.NuevoEstado}' el {historial.FechaCambio:dd/MM/yyyy HH:mm}. " +
                             $"Motivo: {historial.MotivoCambio}";

                // Log para auditoría
                Console.WriteLine($"[NOTIFICACIÓN] Cliente: {cliente.Nombre}, Mensaje: {mensaje}");
                
                await Task.CompletedTask; // Simular operación asíncrona
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar la operación principal
                Console.WriteLine($"[ERROR] No se pudo enviar notificación: {ex.Message}");
            }
        }
    }
}
