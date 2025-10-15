namespace Audicob.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal IngresosMensuales { get; set; }
        public decimal DeudaTotal { get; set; }
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
        
        // Estado de morosidad del cliente
        public string EstadoMora { get; set; } = "Al día"; // Al día, Temprana, Moderada, Grave, Crítica

        // Relación con ApplicationUser
        public string? UserId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        // Relaciones con otras entidades
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public LineaCredito? LineaCredito { get; set; }
        public ICollection<EvaluacionCliente> Evaluaciones { get; set; } = new List<EvaluacionCliente>();
        public AsignacionAsesor? AsignacionAsesor { get; set; }
        public Deuda? Deuda { get; set; }
        
        // Historial de cambios de estado de morosidad
        public ICollection<HistorialEstadoMora> HistorialEstadosMora { get; set; } = new List<HistorialEstadoMora>();
    }
}