using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audicob.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        // Relación con el Supervisor que recibe la notificación
        public string SupervisorId { get; set; } = string.Empty;
        public ApplicationUser? Supervisor { get; set; }

        // Contenido de la notificación
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        // Relación con la asignación de asesor
        public int? AsignacionAsesorId { get; set; }
        public AsignacionAsesor? AsignacionAsesor { get; set; }

        // Información del cliente relacionado
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        // Estado de lectura
        public bool Leida { get; set; } = false;

        // Fechas
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaLectura { get; set; }

        // Tipo de notificación para filtrar
        public string TipoNotificacion { get; set; } = "NuevaAsignacion";

        // Información adicional opcional
        public string? IconoTipo { get; set; } 
        public bool Importante { get; set; } = false; 

        // Método para marcar como leída
        public void MarcarComoLeida()
        {
            if (!Leida)
            {
                Leida = true;
                FechaLectura = DateTime.UtcNow;
            }
        }

        // Método para verificar si ha pasado 24 horas sin leer
        public bool TieneRecordatorioPendiente()
        {
            return !Leida && (DateTime.UtcNow - FechaCreacion).TotalHours >= 24;
        }
    }
}