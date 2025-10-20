using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audicob.Models.ViewModels.Supervisor
{
    public class AsesorViewModel
    {
        public string Nombre { get; set; }
        public int CantidadCarteras { get; set; }
        public decimal MontoTotal { get; set; }
        public int CantidadCuentas { get; set; }
        public string Estado { get; set; }
    }
}
