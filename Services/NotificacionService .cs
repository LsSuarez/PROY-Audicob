using Audicob.Data;
using Audicob.Models;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Services
{
    public class NotificacionService : INotificacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificacionService> _logger;

        public NotificacionService(ApplicationDbContext context,
            ILogger<NotificacionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Notificacion>> ObtenerNotificacionesUsuario(string supervisorId)
        {
            return await _context.Notificaciones
                .Where(n => n.SupervisorId == supervisorId)
                .Include(n => n.AsignacionAsesor)
                .Include(n => n.Cliente)
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();
        }

        public async Task<int> ObtenerNotificacionesNoLeidasCount(string supervisorId)
        {
            return await _context.Notificaciones
                .Where(n => n.SupervisorId == supervisorId && !n.Leida)
                .CountAsync();
        }

        public async Task CrearNotificacion(Notificacion notificacion)
        {
            notificacion.FechaCreacion = DateTime.UtcNow;
            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Notificación creada para supervisor {notificacion.SupervisorId}");
        }

        public async Task MarcarComoLeida(int notificacionId)
        {
            var notificacion = await _context.Notificaciones.FindAsync(notificacionId);
            if (notificacion != null)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.UtcNow;
                _context.Notificaciones.Update(notificacion);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Notificación {notificacionId} marcada como leída");
            }
        }

        public async Task EnviarRecordatorios()
        {
            var hace24Horas = DateTime.UtcNow.AddHours(-24);
            var notificacionesSinLeer = await _context.Notificaciones
                .Where(n => !n.Leida && n.FechaCreacion <= hace24Horas && n.TipoNotificacion == "NuevaAsignacion")
                .ToListAsync();

            foreach (var notificacion in notificacionesSinLeer)
            {
                var recordatorioExistente = await _context.Notificaciones
                    .FirstOrDefaultAsync(n => n.AsignacionAsesorId == notificacion.AsignacionAsesorId
                        && n.TipoNotificacion == "Recordatorio"
                        && n.SupervisorId == notificacion.SupervisorId);

                if (recordatorioExistente == null)
                {
                    var recordatorio = new Notificacion
                    {
                        SupervisorId = notificacion.SupervisorId,
                        Titulo = "⏰ Recordatorio: Cartera Pendiente",
                        Descripcion = $"Tienes una asignación de cartera pendiente de revisar desde hace 24 horas.",
                        AsignacionAsesorId = notificacion.AsignacionAsesorId,
                        ClienteId = notificacion.ClienteId,
                        TipoNotificacion = "Recordatorio",
                        IconoTipo = "⏰",
                        FechaCreacion = DateTime.UtcNow
                    };

                    await CrearNotificacion(recordatorio);
                }
            }
        }
    }
}