using System.Collections.Generic;

namespace Audicob.Models.ViewModels.Supervisor
{
    public class PerfilClienteViewModel
    {
        public Audicob.Models.Cliente ClienteInfo { get; set; } = null!;
        public List<Audicob.Models.Transaccion> TransaccionesRecientes { get; set; } = new();
        public decimal TotalPagos { get; set; }
        public int PagosValidados { get; set; }
        public int PagosPendientes { get; set; }
    }
}