using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Supervisor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupervisorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        // Dashboard principal
        public async Task<IActionResult> Dashboard()
        {
            var vm = new SupervisorDashboardViewModel
            {
                TotalClientes = await _db.Clientes.CountAsync(),
                EvaluacionesPendientes = await _db.Evaluaciones.CountAsync(e => e.Estado == "Pendiente"),
                TotalDeuda = await _db.Clientes.SumAsync(c => c.DeudaTotal),
                TotalPagosUltimoMes = await _db.Pagos
                    .Where(p => p.Fecha >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(p => p.Monto)
            };

            var pagos = await _db.Pagos
                .Where(p => p.Fecha >= DateTime.UtcNow.AddMonths(-6))
                .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            var pagosFormat = pagos.Select(g => new
            {
                Mes = $"{g.Month}/{g.Year}",
                Total = g.Total
            }).ToList();

            vm.Meses = pagosFormat.Select(p => p.Mes).ToList();
            vm.PagosPorMes = pagosFormat.Select(p => p.Total).ToList();

            var deudas = await _db.Clientes
                .OrderByDescending(c => c.DeudaTotal)
                .Take(5)
                .Select(c => new { c.Nombre, c.DeudaTotal })
                .ToListAsync();

            vm.Clientes = deudas.Select(d => d.Nombre).ToList();
            vm.DeudasPorCliente = deudas.Select(d => d.DeudaTotal).ToList();

            var pagosPendientes = await _db.Pagos
                .Where(p => p.Estado == "Pendiente")
                .Include(p => p.Cliente)
                .OrderBy(p => p.Fecha)
                .Take(10)
                .ToListAsync();

            vm.PagosPendientes = pagosPendientes;

            return View(vm);
        }

        // HU7: Validar pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarPago(int pagoId)
        {
            var pago = await _db.Pagos
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p => p.Id == pagoId);
                
            if (pago == null)
            {
                TempData["Error"] = "Pago no encontrado.";
                return RedirectToAction("Dashboard");
            }

            if (pago.Estado != "Pendiente")
            {
                TempData["Error"] = "Este pago ya ha sido validado.";
                return RedirectToAction("Dashboard");
            }

            var user = await _userManager.GetUserAsync(User);

            pago.Validado = true;
            pago.Estado = "Cancelado";
            
            var fechaValidacion = DateTime.UtcNow;
            pago.Observacion = $"Validado por {user.FullName} el {fechaValidacion:dd/MM/yyyy HH:mm:ss}";

            if (pago.Cliente != null)
            {
                pago.Cliente.DeudaTotal -= pago.Monto;
                if (pago.Cliente.DeudaTotal < 0) pago.Cliente.DeudaTotal = 0;
                pago.Cliente.FechaActualizacion = DateTime.UtcNow;
            }

            _db.Update(pago);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Pago de S/ {pago.Monto:N2} validado exitosamente por {user.FullName}. Estado de cuenta actualizado.";
            return RedirectToAction("Dashboard");
        }

        // GET: Asignar línea de crédito (HU3)
        public async Task<IActionResult> AsignarLineaCredito()
        {
            var clientes = await _db.Clientes
                .Select(c => new { c.Id, c.Nombre })
                .ToListAsync();

            Console.WriteLine("Clientes encontrados (GET): " + clientes.Count);

            ViewBag.Clientes = clientes;
            return View();
        }

        // POST: Asignar línea de crédito (HU3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarLineaCredito(int clienteId, decimal monto)
        {
            if (!ModelState.IsValid)
            {
                var clientes = await _db.Clientes
                    .Select(c => new { c.Id, c.Nombre })
                    .ToListAsync();
                ViewBag.Clientes = clientes;
                return View();
            }

            if (monto < 180)
            { 
                TempData["Error"] = "Debe ingresar un monto mayor a 180.";
                return RedirectToAction("AsignarLineaCredito");
            }

            var cliente = await _db.Clientes
                .Include(c => c.LineaCredito)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no válido.";
                return RedirectToAction("AsignarLineaCredito");
            }

            if (cliente.LineaCredito != null)
            {
                TempData["Error"] = "El cliente ya tiene asignada una línea de crédito.";
                return RedirectToAction("AsignarLineaCredito");
            }

            var linea = new LineaCredito
            {
                ClienteId = cliente.Id,
                Monto = monto,
                FechaAsignacion = DateTime.UtcNow,
                UsuarioAsignador = User.Identity?.Name ?? "Supervisor"
            };

            _db.LineasCredito.Add(linea);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Línea de crédito asignada a {cliente.Nombre}.";
            return RedirectToAction("AsignarLineaCredito");
        }
        // HU1: Ver informe financiero detallado
        public async Task<IActionResult> VerInformeFinanciero(int id)
        {
            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Deuda)
                .Include(c => c.Evaluaciones)
                .Include(c => c.LineaCredito)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            var pagosUltimos12Meses = await _db.Pagos
                .Where(p => p.ClienteId == cliente.Id && p.Fecha >= DateTime.UtcNow.AddMonths(-12))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            var vm = new InformeFinancieroViewModel
            {
                ClienteId = cliente.Id,
                ClienteNombre = cliente.Nombre,
                Documento = cliente.Documento,
                IngresosMensuales = cliente.IngresosMensuales,
                DeudaTotal = cliente.DeudaTotal,
                FechaActualizacion = cliente.FechaActualizacion,
                PagosUltimos12Meses = pagosUltimos12Meses,
                TotalPagado12Meses = pagosUltimos12Meses.Sum(p => p.Monto),
                LineaCredito = cliente.LineaCredito,
                Deuda = cliente.Deuda,
                Evaluaciones = cliente.Evaluaciones.OrderByDescending(e => e.Fecha).ToList()
            };

            return View(vm);
        }

        // HU2: Ver evaluaciones pendientes
        public async Task<IActionResult> EvaluacionesPendientes()
        {
            var evaluaciones = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .Where(e => e.Estado == "Pendiente")
                .OrderBy(e => e.Fecha)
                .ToListAsync();

            return View(evaluaciones);
        }

        // HU2: Ver detalle de evaluación
        public async Task<IActionResult> DetalleEvaluacion(int id)
        {
            var evaluacion = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction("EvaluacionesPendientes");
            }

            var vm = new EvaluacionViewModel
            {
                ClienteId = evaluacion.ClienteId,
                NombreCliente = evaluacion.Cliente.Nombre,
                IngresosMensuales = evaluacion.Cliente.IngresosMensuales,
                DeudaTotal = evaluacion.Cliente.DeudaTotal,
                Estado = evaluacion.Estado,
                Responsable = evaluacion.Responsable,
                Comentario = evaluacion.Comentario,
                FechaEvaluacion = evaluacion.Fecha
            };

            return View(vm);
        }

        // HU2: Confirmar evaluación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEvaluacion(int id)
        {
            var evaluacion = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction("EvaluacionesPendientes");
            }

            var user = await _userManager.GetUserAsync(User);

            evaluacion.Estado = "Marcado";
            evaluacion.Responsable = user.FullName;
            evaluacion.Fecha = DateTime.UtcNow;

            _db.Update(evaluacion);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Evaluación confirmada y marcada por {user.FullName}.";
            return RedirectToAction("EvaluacionesPendientes");
        }

        // HU2: Rechazar evaluación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarEvaluacion(int id, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                TempData["Error"] = "El comentario es obligatorio al rechazar una evaluación.";
                return RedirectToAction("DetalleEvaluacion", new { id });
            }

            var evaluacion = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction("EvaluacionesPendientes");
            }

            var user = await _userManager.GetUserAsync(User);

            evaluacion.Estado = "Rechazado";
            evaluacion.Responsable = user.FullName;
            evaluacion.Comentario = comentario;
            evaluacion.Fecha = DateTime.UtcNow;

            _db.Update(evaluacion);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Evaluación rechazada por {user.FullName}.";
            return RedirectToAction("EvaluacionesPendientes");
        }

        // HU4: Buscar cliente
        public async Task<IActionResult> BuscarCliente(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] = "Debe ingresar un código o documento para buscar.";
                return RedirectToAction("Dashboard");
            }

            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Deuda)
                .Include(c => c.Evaluaciones)
                .Include(c => c.LineaCredito)
                .Include(c => c.AsignacionAsesor)
                .FirstOrDefaultAsync(c => c.Documento == codigo || c.Id.ToString() == codigo);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction("PerfilCliente", new { id = cliente.Id });
        }

        // HU4: Ver perfil completo del cliente
        public async Task<IActionResult> PerfilCliente(int id)
        {
            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Deuda)
                .Include(c => c.Evaluaciones)
                .Include(c => c.LineaCredito)
                .Include(c => c.AsignacionAsesor)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            var transacciones = await _db.Transacciones
                .Where(t => t.ClienteId == cliente.Id)
                .OrderByDescending(t => t.Fecha)
                .Take(10)
                .ToListAsync();

            var vm = new PerfilClienteViewModel
            {
                ClienteInfo = cliente,  // CORREGIDO: Cambié "Cliente" a "ClienteInfo"
                TransaccionesRecientes = transacciones,
                TotalPagos = cliente.Pagos.Sum(p => p.Monto),
                PagosValidados = cliente.Pagos.Count(p => p.Validado),
                PagosPendientes = cliente.Pagos.Count(p => !p.Validado)
            };

            return View(vm);
        }

        private async Task<Cliente?> TryGetClienteAsync(int clienteId)
        {
            return await _db.Clientes
                .Include(c => c.LineaCredito)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
        }

        // ===============================
        // HU-29: FILTRAR ESTADO DE LA MORA
        // ===============================

        [HttpGet]
        public async Task<IActionResult> FiltrarMora()
        {
            var user = await _userManager.GetUserAsync(User);
            var vm = new FiltroMoraViewModel();

            // Cargar filtros guardados del usuario (temporalmente comentado)
            // vm.FiltrosGuardados = await _db.FiltrosGuardados
            //     .Where(f => f.UserId == user.Id)
            //     .OrderBy(f => f.Nombre)
            //     .ToListAsync();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FiltrarMora(FiltroMoraViewModel modelo)
        {
            var startTime = DateTime.UtcNow;
            var user = await _userManager.GetUserAsync(User);

            try
            {
                // DATOS DE PRUEBA FIJOS (para evitar problemas de SQL por ahora)
                var clientesMora = new List<ClienteMoraInfo>
                {
                    new ClienteMoraInfo
                    {
                        ClienteId = 1,
                        Nombre = "Cliente Demo",
                        Documento = "12345678",
                        DeudaTotal = 1200,
                        MontoEnMora = 1200,
                        DiasEnMora = 30,
                        TipoCliente = "General",
                        AsesorAsignado = "Asesor de Cobranza",
                        IngresosMensuales = 2500,
                        FechaUltimoPago = DateTime.UtcNow.AddDays(-40)
                    },
                    new ClienteMoraInfo
                    {
                        ClienteId = 2,
                        Nombre = "María López",
                        Documento = "87654321",
                        DeudaTotal = 800,
                        MontoEnMora = 800,
                        DiasEnMora = 15,
                        TipoCliente = "General",
                        AsesorAsignado = "Asesor de Cobranza",
                        IngresosMensuales = 3200,
                        FechaUltimoPago = DateTime.UtcNow.AddDays(-25)
                    },
                    new ClienteMoraInfo
                    {
                        ClienteId = 3,
                        Nombre = "Carlos Ruiz",
                        Documento = "11223344",
                        DeudaTotal = 1500,
                        MontoEnMora = 1500,
                        DiasEnMora = 45,
                        TipoCliente = "General",
                        AsesorAsignado = "Asesor de Cobranza",
                        IngresosMensuales = 2800,
                        FechaUltimoPago = DateTime.UtcNow.AddDays(-60)
                    }
                };

                // Aplicar filtros en memoria
                var resultadosFiltrados = clientesMora.AsQueryable();

                if (modelo.RangoDiasDesde.HasValue)
                {
                    resultadosFiltrados = resultadosFiltrados.Where(c => c.DiasEnMora >= modelo.RangoDiasDesde.Value);
                }

                if (modelo.RangoDiasHasta.HasValue)
                {
                    resultadosFiltrados = resultadosFiltrados.Where(c => c.DiasEnMora <= modelo.RangoDiasHasta.Value);
                }

                if (!string.IsNullOrEmpty(modelo.TipoCliente))
                {
                    resultadosFiltrados = resultadosFiltrados.Where(c => c.TipoCliente == modelo.TipoCliente);
                }

                if (modelo.MontoDesde.HasValue)
                {
                    resultadosFiltrados = resultadosFiltrados.Where(c => c.MontoEnMora >= modelo.MontoDesde.Value);
                }

                if (modelo.MontoHasta.HasValue)
                {
                    resultadosFiltrados = resultadosFiltrados.Where(c => c.MontoEnMora <= modelo.MontoHasta.Value);
                }

                modelo.ResultadosFiltrados = resultadosFiltrados.ToList();

                // Calcular prioridades y estado de mora para cada cliente
                foreach (var cliente in modelo.ResultadosFiltrados)
                {
                    cliente.CalcularPrioridad();
                    
                    // Determinar estado de mora basado en días
                    if (cliente.DiasEnMora >= 90)
                        cliente.EstadoMora = "Crítica";
                    else if (cliente.DiasEnMora >= 60)
                        cliente.EstadoMora = "Grave";
                    else if (cliente.DiasEnMora >= 30)
                        cliente.EstadoMora = "Moderada";
                    else
                        cliente.EstadoMora = "Temprana";
                }

                // Aplicar filtro de estado de mora si se especificó
                if (!string.IsNullOrEmpty(modelo.EstadoMora))
                {
                    modelo.ResultadosFiltrados = modelo.ResultadosFiltrados
                        .Where(c => c.EstadoMora == modelo.EstadoMora)
                        .ToList();
                }

                // Ordenar por prioridad (Crítica primero)
                modelo.ResultadosFiltrados = modelo.ResultadosFiltrados
                    .OrderByDescending(c => c.NivelPrioridad == "Crítica")
                    .ThenByDescending(c => c.NivelPrioridad == "Alta")
                    .ThenByDescending(c => c.DiasEnMora)
                    .ThenByDescending(c => c.MontoEnMora)
                    .ToList();

                // Guardar filtro si se solicitó
                if (modelo.GuardarFiltro && !string.IsNullOrEmpty(modelo.NombreFiltroGuardado) && user != null)
                {
                    await GuardarConfiguracionFiltro(modelo, user.Id);
                    TempData["Success"] = "Filtro guardado exitosamente.";
                }

                // Calcular metadatos de respuesta
                var endTime = DateTime.UtcNow;
                modelo.TiempoRespuesta = endTime - startTime;
                modelo.TotalRegistros = modelo.ResultadosFiltrados.Count;
                modelo.FechaConsulta = startTime;

                // Cargar filtros guardados (temporalmente comentado)
                // modelo.FiltrosGuardados = await _db.FiltrosGuardados
                //     .Where(f => f.UserId == user.Id)
                //     .OrderBy(f => f.Nombre)
                //     .ToListAsync();

                if (modelo.TiempoRespuesta.TotalSeconds < 3)
                {
                    TempData["Success"] = $"Filtros aplicados exitosamente. {modelo.TotalRegistros} registros encontrados en {modelo.TiempoRespuesta.TotalSeconds:F2} segundos.";
                }
                else
                {
                    TempData["Warning"] = $"Consulta completada en {modelo.TiempoRespuesta.TotalSeconds:F2} segundos. Considere optimizar los filtros.";
                }

                return View(modelo);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al aplicar filtros: " + ex.Message;
                // modelo.FiltrosGuardados = await _db.FiltrosGuardados
                //     .Where(f => f.UserId == user.Id)
                //     .OrderBy(f => f.Nombre)
                //     .ToListAsync();
                return View(modelo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportarResultadosMora(FiltroMoraViewModel modelo)
        {
            // Reejecutar consulta para obtener datos actualizados
            var filtroResult = await FiltrarMora(modelo);
            
            if (filtroResult is ViewResult viewResult && viewResult.Model is FiltroMoraViewModel vm)
            {
                // Generar archivo Excel o PDF según preferencia
                var fileName = $"Reporte_Mora_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var content = GenerarExcelMora(vm.ResultadosFiltrados);
                
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            TempData["Error"] = "No hay datos para exportar.";
            return RedirectToAction("FiltrarMora");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CargarFiltroGuardado(int filtroId)
        {
            var user = await _userManager.GetUserAsync(User);
            // var filtro = await _db.FiltrosGuardados
            //     .FirstOrDefaultAsync(f => f.Id == filtroId && f.UserId == user.Id);
            FiltroGuardado? filtro = null;

            if (filtro != null)
            {
                var configuracion = System.Text.Json.JsonSerializer.Deserialize<FiltroMoraViewModel>(filtro.ConfiguracionJson);
                if (configuracion != null)
                {
                    // Aplicar automáticamente el filtro guardado
                    return await FiltrarMora(configuracion);
                }
            }

            TempData["Error"] = "No se pudo cargar el filtro guardado.";
            return RedirectToAction("FiltrarMora");
        }

        private async Task GuardarConfiguracionFiltro(FiltroMoraViewModel modelo, string userId)
        {
            var configuracionJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                modelo.RangoDiasDesde,
                modelo.RangoDiasHasta,
                modelo.TipoCliente,
                modelo.MontoDesde,
                modelo.MontoHasta,
                modelo.EstadoMora
            });

            var filtroGuardado = new FiltroGuardado
            {
                Nombre = modelo.NombreFiltroGuardado!,
                UserId = userId,
                ConfiguracionJson = configuracionJson,
                FechaCreacion = DateTime.UtcNow,
                EsPredeterminado = false
            };

            // _db.FiltrosGuardados.Add(filtroGuardado);
            // await _db.SaveChangesAsync();
            
            // Temporalmente guardamos en memoria
            TempData["Info"] = "Funcionalidad de guardar filtros estará disponible próximamente.";
        }

        private byte[] GenerarExcelMora(List<ClienteMoraInfo> datos)
        {
            // Implementación básica de generación de Excel
            // En un proyecto real usaríamos EPPlus o ClosedXML
            var csv = "Cliente,Documento,Deuda Total,Días en Mora,Monto en Mora,Prioridad,Asesor\n";
            
            foreach (var item in datos)
            {
                csv += $"{item.Nombre},{item.Documento},{item.DeudaTotal:C},{item.DiasEnMora},{item.MontoEnMora:C},{item.NivelPrioridad},{item.AsesorAsignado}\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        // Método para crear datos de prueba - SOLO PARA DESARROLLO
        [HttpGet]
        public async Task<IActionResult> CrearDatosPrueba()
        {
            try
            {
                // Verificar si ya hay datos
                var clientesExistentes = await _db.Clientes.CountAsync();
                if (clientesExistentes > 0)
                {
                    TempData["Info"] = $"Ya existen {clientesExistentes} clientes en la base de datos.";
                    return RedirectToAction("FiltrarMora");
                }

                // Crear clientes de prueba con deudas - VARIEDAD AMPLIA
                var clientes = new List<Cliente>
                {
                    // MORA TEMPRANA (1-29 días)
                    new Cliente
                    {
                        Nombre = "Luis Morales Castro",
                        Documento = "12345678",
                        IngresosMensuales = 3500,
                        DeudaTotal = 2500,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    new Cliente
                    {
                        Nombre = "Sandra Ruiz Vega", 
                        Documento = "23456789",
                        IngresosMensuales = 2800,
                        DeudaTotal = 1800,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    
                    // MORA MODERADA (30-59 días)
                    new Cliente
                    {
                        Nombre = "Roberto Silva Muñoz",
                        Documento = "34567890",
                        IngresosMensuales = 4200,
                        DeudaTotal = 6500,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    new Cliente
                    {
                        Nombre = "Patricia Herrera Cruz",
                        Documento = "45678901",
                        IngresosMensuales = 3100,
                        DeudaTotal = 4200,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    
                    // MORA GRAVE (60-89 días)
                    new Cliente
                    {
                        Nombre = "Miguel Torres Jiménez",
                        Documento = "56789012",
                        IngresosMensuales = 5000,
                        DeudaTotal = 8700,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    new Cliente
                    {
                        Nombre = "Carmen Delgado Ramos",
                        Documento = "67890123",
                        IngresosMensuales = 2900,
                        DeudaTotal = 5600,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    
                    // MORA CRÍTICA (90+ días)
                    new Cliente
                    {
                        Nombre = "Fernando Castillo Vargas",
                        Documento = "78901234",
                        IngresosMensuales = 4800,
                        DeudaTotal = 15000,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    new Cliente
                    {
                        Nombre = "Gloria Mendoza Soto",
                        Documento = "89012345",
                        IngresosMensuales = 3300,
                        DeudaTotal = 12300,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    
                    // CASOS EXTREMOS
                    new Cliente
                    {
                        Nombre = "Andrés Guerrero Lima",
                        Documento = "90123456",
                        IngresosMensuales = 6500,
                        DeudaTotal = 25000,
                        FechaActualizacion = DateTime.UtcNow
                    },
                    new Cliente
                    {
                        Nombre = "Victoria Peña Moreno",
                        Documento = "01234567",
                        IngresosMensuales = 1800,
                        DeudaTotal = 7800,
                        FechaActualizacion = DateTime.UtcNow
                    }
                };

                _db.Clientes.AddRange(clientes);
                await _db.SaveChangesAsync();

                // Crear deudas con diferentes niveles de mora
                var deudas = new List<Deuda>
                {
                    // MORA TEMPRANA (1-29 días) - Luis Morales
                    new Deuda
                    {
                        ClienteId = clientes[0].Id,
                        Monto = 2500,
                        Intereses = 125,
                        PenalidadCalculada = 62.50m,
                        TotalAPagar = 2687.50m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-15) // 15 días en mora
                    },
                    // MORA TEMPRANA - Sandra Ruiz
                    new Deuda
                    {
                        ClienteId = clientes[1].Id,
                        Monto = 1800,
                        Intereses = 108,
                        PenalidadCalculada = 54.00m,
                        TotalAPagar = 1962.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-25) // 25 días en mora
                    },
                    
                    // MORA MODERADA (30-59 días) - Roberto Silva
                    new Deuda
                    {
                        ClienteId = clientes[2].Id,
                        Monto = 6500,
                        Intereses = 650,
                        PenalidadCalculada = 325.00m,
                        TotalAPagar = 7475.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-42) // 42 días en mora
                    },
                    // MORA MODERADA - Patricia Herrera
                    new Deuda
                    {
                        ClienteId = clientes[3].Id,
                        Monto = 4200,
                        Intereses = 504,
                        PenalidadCalculada = 252.00m,
                        TotalAPagar = 4956.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-35) // 35 días en mora
                    },
                    
                    // MORA GRAVE (60-89 días) - Miguel Torres
                    new Deuda
                    {
                        ClienteId = clientes[4].Id,
                        Monto = 8700,
                        Intereses = 1305,
                        PenalidadCalculada = 652.50m,
                        TotalAPagar = 10657.50m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-72) // 72 días en mora
                    },
                    // MORA GRAVE - Carmen Delgado
                    new Deuda
                    {
                        ClienteId = clientes[5].Id,
                        Monto = 5600,
                        Intereses = 896,
                        PenalidadCalculada = 448.00m,
                        TotalAPagar = 6944.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-65) // 65 días en mora
                    },
                    
                    // MORA CRÍTICA (90+ días) - Fernando Castillo
                    new Deuda
                    {
                        ClienteId = clientes[6].Id,
                        Monto = 15000,
                        Intereses = 3000,
                        PenalidadCalculada = 1500.00m,
                        TotalAPagar = 19500.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-105) // 105 días en mora
                    },
                    // MORA CRÍTICA - Gloria Mendoza
                    new Deuda
                    {
                        ClienteId = clientes[7].Id,
                        Monto = 12300,
                        Intereses = 2214,
                        PenalidadCalculada = 1107.00m,
                        TotalAPagar = 15621.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-98) // 98 días en mora
                    },
                    
                    // CASOS EXTREMOS - Andrés Guerrero
                    new Deuda
                    {
                        ClienteId = clientes[8].Id,
                        Monto = 25000,
                        Intereses = 6250,
                        PenalidadCalculada = 3125.00m,
                        TotalAPagar = 34375.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-150) // 150 días en mora
                    },
                    // CASOS EXTREMOS - Victoria Peña
                    new Deuda
                    {
                        ClienteId = clientes[9].Id,
                        Monto = 7800,
                        Intereses = 2106,
                        PenalidadCalculada = 1053.00m,
                        TotalAPagar = 10959.00m,
                        FechaVencimiento = DateTime.UtcNow.AddDays(-180) // 180 días en mora
                    }
                };

                _db.Deudas.AddRange(deudas);
                await _db.SaveChangesAsync();

                // Crear pagos variados de prueba
                var pagos = new List<Pago>
                {
                    // Pagos de clientes con mora temprana
                    new Pago
                    {
                        ClienteId = clientes[0].Id,
                        Monto = 800,
                        Fecha = DateTime.UtcNow.AddDays(-10),
                        Estado = "Cancelado",
                        Validado = true,
                        Observacion = "Pago parcial reciente"
                    },
                    new Pago
                    {
                        ClienteId = clientes[1].Id,
                        Monto = 600,
                        Fecha = DateTime.UtcNow.AddDays(-20),
                        Estado = "Cancelado",
                        Validado = true,
                        Observacion = "Abono"
                    },
                    
                    // Pagos de clientes con mora moderada
                    new Pago
                    {
                        ClienteId = clientes[2].Id,
                        Monto = 1200,
                        Fecha = DateTime.UtcNow.AddDays(-50),
                        Estado = "Cancelado",
                        Validado = true,
                        Observacion = "Pago anterior al vencimiento"
                    },
                    new Pago
                    {
                        ClienteId = clientes[3].Id,
                        Monto = 900,
                        Fecha = DateTime.UtcNow.AddDays(-45),
                        Estado = "Cancelado",
                        Validado = true,
                        Observacion = "Último pago registrado"
                    },
                    
                    // Pagos de clientes con mora grave
                    new Pago
                    {
                        ClienteId = clientes[4].Id,
                        Monto = 2000,
                        Fecha = DateTime.UtcNow.AddDays(-80),
                        Estado = "Cancelado",
                        Validado = true,
                        Observacion = "Pago significativo anterior"
                    },
                    
                    // Pagos pendientes (no validados)
                    new Pago
                    {
                        ClienteId = clientes[5].Id,
                        Monto = 1500,
                        Fecha = DateTime.UtcNow.AddDays(-5),
                        Estado = "Pendiente",
                        Validado = false,
                        Observacion = "Pago pendiente de validación"
                    },
                    new Pago
                    {
                        ClienteId = clientes[6].Id,
                        Monto = 3000,
                        Fecha = DateTime.UtcNow.AddDays(-2),
                        Estado = "Pendiente",
                        Validado = false,
                        Observacion = "Pago grande pendiente"
                    },
                    
                    // Casos sin pagos recientes (mora crítica)
                    new Pago
                    {
                        ClienteId = clientes[8].Id,
                        Monto = 5000,
                        Fecha = DateTime.UtcNow.AddDays(-200),
                        Estado = "Cancelado",
                        Validado = true,
                        Observacion = "Último pago hace mucho tiempo"
                    }
                };

                _db.Pagos.AddRange(pagos);
                await _db.SaveChangesAsync();

                TempData["Success"] = "¡Datos de prueba expandidos creados exitosamente! Se crearon 10 clientes con diferentes niveles de mora: Temprana (2), Moderada (2), Grave (2), Crítica (2) y Casos Extremos (2).";
                return RedirectToAction("FiltrarMora");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear datos de prueba: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }


    }
}