namespace Audicob.Models
{
    public class ComprobantePago
    {
        public int Id { get; set; }
        public string NumeroTransaccion { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Metodo { get; set; }  // Ejemplo: "Tarjeta de crédito"
        public string Estado { get; set; }  // Ejemplo: "Completado"
    }
}
