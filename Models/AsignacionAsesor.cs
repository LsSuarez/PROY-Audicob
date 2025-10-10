namespace Audicob.Models
{
    public class AsignacionAsesor
    {
        public int Id { get; set; }
        public string? AsesorUserId { get; set; }
        public string AsesorNombre { get; set; } = string.Empty;
        public DateTime FechaAsignacion { get; set; }

        public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    }
}
