using System;
using Audicob.Helpers;

namespace Audicob.Models.DTOs
{
    public class NotificacionDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLectura { get; set; }
        public string TipoNotificacion { get; set; } = string.Empty;
        public string? IconoTipo { get; set; }
        public bool Importante { get; set; }
        public int? ClienteId { get; set; }
        public string? ClienteNombre { get; set; }
        public int? AsignacionAsesorId { get; set; }

        public static NotificacionDto FromNotificacion(Notificacion notificacion)
        {
            return new NotificacionDto
            {
                Id = notificacion.Id,
                Titulo = notificacion.Titulo,
                Descripcion = notificacion.Descripcion,
                Leida = notificacion.Leida,
                // Convertir a hora de Perú para el cliente
                FechaCreacion = DateTimeHelper.ConvertToPeruTime(notificacion.FechaCreacion),
                FechaLectura = notificacion.FechaLectura.HasValue
                    ? DateTimeHelper.ConvertToPeruTime(notificacion.FechaLectura.Value)
                    : null,
                TipoNotificacion = notificacion.TipoNotificacion,
                IconoTipo = notificacion.IconoTipo,
                Importante = notificacion.Importante,
                ClienteId = notificacion.ClienteId,
                ClienteNombre = notificacion.Cliente?.Nombre,
                AsignacionAsesorId = notificacion.AsignacionAsesorId
            };
        }
    }
}