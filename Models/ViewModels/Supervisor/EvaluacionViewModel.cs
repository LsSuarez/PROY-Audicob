namespace Audicob.Models.ViewModels.Supervisor
{
    public class EvaluacionViewModel
    {
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal IngresosMensuales { get; set; }
        public decimal DeudaTotal { get; set; }

        public string Estado { get; set; } = "Pendiente"; // Pendiente, Marcado, Rechazado
        public string Responsable { get; set; } = string.Empty;
        public string? Comentario { get; set; }
        public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;
    }
}
