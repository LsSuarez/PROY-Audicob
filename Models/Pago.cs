namespace Audicob.Models
{
    public class Pago
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public bool Validado { get; set; }

        // Ahora 'Estado' es una propiedad persistente en la base de datos
        public string Estado { get; set; } = "Pendiente"; // Valor por defecto

        public string? Observacion { get; set; }

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!; // Relación con Cliente
    }
}
