using System.ComponentModel.DataAnnotations;

namespace Audicob.Models
{
    public class FiltroGuardado
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string ConfiguracionJson { get; set; } = string.Empty;
        
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        
        public bool EsPredeterminado { get; set; } = false;

        // Relación con ApplicationUser
        public ApplicationUser? Usuario { get; set; }
    }
}