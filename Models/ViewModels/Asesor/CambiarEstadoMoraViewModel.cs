using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Asesor
{
    /// <summary>
    /// ViewModel para el cambio de estado de morosidad
    /// Cumple con los criterios de aceptación de HU-30
    /// </summary>
    public class CambiarEstadoMoraViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente")]
        public int ClienteId { get; set; }

        [Display(Name = "Cliente")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Display(Name = "Documento")]
        public string ClienteDocumento { get; set; } = string.Empty;

        [Display(Name = "Estado Actual")]
        public string EstadoActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar el nuevo estado")]
        [Display(Name = "Nuevo Estado")]
        public string NuevoEstado { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe especificar el motivo del cambio")]
        [Display(Name = "Motivo del Cambio")]
        [StringLength(200, ErrorMessage = "El motivo no puede exceder 200 caracteres")]
        public string MotivoCambio { get; set; } = string.Empty;

        [Display(Name = "Observaciones")]
        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        public string? Observaciones { get; set; }

        [Display(Name = "Enviar Notificación al Cliente")]
        public bool EnviarNotificacion { get; set; } = true;

        // Lista de estados disponibles
        public static List<string> EstadosDisponibles => new()
        {
            "Al día",
            "Temprana", 
            "Moderada",
            "Grave",
            "Crítica"
        };

        // Lista de motivos predefinidos
        public static List<string> MotivosPredefinidos => new()
        {
            "Pago recibido",
            "Acuerdo de pago firmado",
            "Incumplimiento de pago",
            "Vencimiento de plazo",
            "Evaluación de riesgo",
            "Solicitud del cliente",
            "Revisión periódica",
            "Corrección de datos",
            "Otro motivo"
        };

        /// <summary>
        /// Valida que el cambio de estado sea válido
        /// </summary>
        /// <returns>Lista de errores de validación</returns>
        public List<string> ValidarCambioEstado()
        {
            var errores = new List<string>();

            if (string.IsNullOrEmpty(EstadoActual))
            {
                errores.Add("No se puede determinar el estado actual del cliente");
            }

            if (string.IsNullOrEmpty(NuevoEstado))
            {
                errores.Add("Debe seleccionar un nuevo estado");
            }

            if (!string.IsNullOrEmpty(EstadoActual) && !string.IsNullOrEmpty(NuevoEstado) && EstadoActual == NuevoEstado)
            {
                errores.Add("El nuevo estado debe ser diferente al estado actual");
            }

            if (!string.IsNullOrEmpty(NuevoEstado) && !EstadosDisponibles.Contains(NuevoEstado))
            {
                errores.Add("El estado seleccionado no es válido");
            }

            if (string.IsNullOrWhiteSpace(MotivoCambio))
            {
                errores.Add("Debe especificar el motivo del cambio");
            }

            return errores;
        }

        /// <summary>
        /// Obtiene la descripción del cambio para mostrar al usuario
        /// </summary>
        /// <returns>Descripción legible del cambio</returns>
        public string ObtenerDescripcionCambio()
        {
            return $"Cambiar estado de '{EstadoActual}' a '{NuevoEstado}' para el cliente {ClienteNombre} ({ClienteDocumento})";
        }

        /// <summary>
        /// Determina la clase CSS para el badge del estado
        /// </summary>
        /// <param name="estado">Estado de morosidad</param>
        /// <returns>Clase CSS de Bootstrap</returns>
        public static string ObtenerClaseEstado(string estado)
        {
            return estado switch
            {
                "Al día" => "badge bg-success",
                "Temprana" => "badge bg-warning",
                "Moderada" => "badge bg-info",
                "Grave" => "badge bg-danger",
                "Crítica" => "badge bg-dark",
                _ => "badge bg-secondary"
            };
        }
    }

    /// <summary>
    /// ViewModel para mostrar el historial de cambios de estado
    /// </summary>
    public class HistorialEstadoMoraViewModel
    {
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteDocumento { get; set; } = string.Empty;
        public string EstadoActual { get; set; } = string.Empty;
        public List<HistorialCambioViewModel> Cambios { get; set; } = new();
        public int TotalCambios { get; set; }
        public DateTime? FechaUltimoCambio { get; set; }
    }

    /// <summary>
    /// ViewModel para representar un cambio individual en el historial
    /// </summary>
    public class HistorialCambioViewModel
    {
        public int Id { get; set; }
        public string EstadoAnterior { get; set; } = string.Empty;
        public string NuevoEstado { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string MotivoCambio { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        
        public string TiempoDesdeCambio
        {
            get
            {
                var diferencia = DateTime.Now - FechaCambio;
                if (diferencia.TotalDays >= 1)
                    return $"Hace {(int)diferencia.TotalDays} días";
                if (diferencia.TotalHours >= 1)
                    return $"Hace {(int)diferencia.TotalHours} horas";
                return "Hace menos de una hora";
            }
        }
    }
}