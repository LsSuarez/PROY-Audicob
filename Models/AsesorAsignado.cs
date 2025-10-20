using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models
{
    public class AsesorAsignado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AsesorNombre { get; set; }

        [Required]
        public int CantidadCarteras { get; set; }

        [Required]
        public decimal MontoTotal { get; set; }

        [Required]
        public int CantidadCuentas { get; set; }

        [Required]
        public string Estado { get; set; } // "Parcial", "Ocupado", "Disponible"
    }
}
