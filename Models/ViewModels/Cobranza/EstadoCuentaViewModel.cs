namespace Audicob.Models.ViewModels.Cobranza
{
    public class EstadoCuentaViewModel
    {
        public decimal TotalDeuda { get; set; }
        public decimal Capital { get; set; }
        public decimal Intereses { get; set; }
        public List<Transaccion> HistorialTransacciones { get; set; }  // Lista de transacciones realizadas
    }
}
