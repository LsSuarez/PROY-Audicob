namespace Audicob.Models
{
    public class EvaluacionCliente
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Marcado, Rechazado
        public string? Comentario { get; set; }
        public string Responsable { get; set; } = string.Empty;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}
