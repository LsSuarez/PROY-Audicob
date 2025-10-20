using System;

namespace Audicob.Models
{
    public class PagoPendiente
    {
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
    }
}