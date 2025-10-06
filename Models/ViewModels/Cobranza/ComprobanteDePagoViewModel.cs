namespace Audicob.Models.ViewModels.Cobranza
{
    public class ComprobanteDePagoViewModel
    {
        public string NumeroTransaccion { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Metodo { get; set; }
        public string Estado { get; set; }
    }
}
