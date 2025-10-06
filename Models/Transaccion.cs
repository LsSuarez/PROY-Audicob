namespace Audicob.Models
{
    public class Transaccion
    {
        public int Id { get; set; }

        // Número de la transacción (ejemplo: "T12345")
        public string NumeroTransaccion { get; set; } = string.Empty;

        // Fecha de la transacción
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // Monto de la transacción
        public decimal Monto { get; set; }

        // Estado de la transacción (completado, pendiente, etc.)
        public string Estado { get; set; } = "Pendiente";  // Ejemplo: "Completado", "Pendiente", "Fallido"

        // Descripción de la transacción (detalles como el motivo de la transacción)
        public string Descripcion { get; set; } = string.Empty;

        // Relación con Cliente (cada transacción está asociada a un cliente)
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }

        // Propiedad opcional para el método de pago
        public string MetodoPago { get; set; } = string.Empty;  // Ejemplo: "Tarjeta de Crédito", "Transferencia"
    }
}
