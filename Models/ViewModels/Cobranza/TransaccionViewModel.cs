namespace Audicob.Models.ViewModels.Cobranza
{
    public class TransaccionViewModel
    {
        public int Id { get; set; }
        public string Fecha { get; set; }
        public string Descripcion { get; set; }
        public decimal Monto { get; set; }
        public string Estado { get; set; }
    }
}
