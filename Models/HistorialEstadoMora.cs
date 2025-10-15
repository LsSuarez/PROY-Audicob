using System.ComponentModel.DataAnnotations;

namespace Audicob.Models
{
    /// <summary>
    /// Modelo para registrar el historial de cambios de estado de morosidad
    /// Implementa auditoría completa para cumplir con los criterios de aceptación
    /// </summary>
    public class HistorialEstadoMora
    {
        public int Id { get; set; }

        // Relación con el cliente
        [Required(ErrorMessage = "El cliente es obligatorio")]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = new Cliente();

        // Estados de morosidad
        [Required(ErrorMessage = "El estado anterior es obligatorio")]
        [MaxLength(50)]
        public string EstadoAnterior { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nuevo estado es obligatorio")]
        [MaxLength(50)]
        public string NuevoEstado { get; set; } = string.Empty;

        // Auditoría y trazabilidad
        [Required(ErrorMessage = "El usuario que realizó el cambio es obligatorio")]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser Usuario { get; set; } = new ApplicationUser();

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

        // Observaciones del cambio (opcional)
        [MaxLength(500)]
        public string? Observaciones { get; set; }

        // Motivo del cambio
        [Required(ErrorMessage = "El motivo del cambio es obligatorio")]
        [MaxLength(200)]
        public string MotivoCambio { get; set; } = string.Empty;

        // Información adicional para auditoría
        [MaxLength(100)]
        public string DireccionIP { get; set; } = string.Empty;

        [MaxLength(200)]
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Método helper para obtener una descripción legible del cambio
        /// </summary>
        /// <returns>Descripción del cambio de estado</returns>
        public string ObtenerDescripcionCambio()
        {
            return $"Estado cambiado de '{EstadoAnterior}' a '{NuevoEstado}' por {Usuario?.UserName ?? "Usuario desconocido"} el {FechaCambio:dd/MM/yyyy HH:mm}";
        }

        /// <summary>
        /// Valida si el cambio de estado es válido
        /// </summary>
        /// <returns>True si el cambio es válido</returns>
        public bool EsCambioValido()
        {
            var estadosValidos = new[] { "Al día", "Temprana", "Moderada", "Grave", "Crítica" };
            return estadosValidos.Contains(EstadoAnterior) && estadosValidos.Contains(NuevoEstado) && EstadoAnterior != NuevoEstado;
        }
    }
}