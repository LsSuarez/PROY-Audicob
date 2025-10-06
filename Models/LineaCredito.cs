namespace Audicob.Models
{
    public class LineaCredito
    {
        public int Id { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
        public string UsuarioAsignador { get; set; } = string.Empty;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}
