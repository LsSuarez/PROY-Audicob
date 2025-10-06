namespace Audicob.Models
{
    public class AsignacionAsesor
    {
        public int Id { get; set; }
        public string AsesorUserId { get; set; } = string.Empty;
        public string AsesorNombre { get; set; } = string.Empty;
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}
