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

        public string EstadoAdmin { get; set; } // "Aceptado" o "Rechazado"
        public string MotivoAdmin { get; set; }
        public DateTime? FechaDecisionAdmin { get; set; }

        public string Estado { get; set; } = "Pendiente"; // Pendiente, Aceptado, Rechazado
        public string UsuarioSupervisor { get; set; } = string.Empty;

        // Relación con ApplicationUser
        public string? UserId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        // Relaciones con otras entidades
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public LineaCredito? LineaCredito { get; set; }
        public ICollection<EvaluacionCliente> Evaluaciones { get; set; } = new List<EvaluacionCliente>();
        public ICollection<PagoPendiente> PagosPendientes { get; set; }
        public AsignacionAsesor? AsignacionAsesor { get; set; }
        public Deuda? Deuda { get; set; }

    }
}