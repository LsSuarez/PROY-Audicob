using Microsoft.AspNetCore.Identity;
using System;

namespace Audicob.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Datos personales
        public string FullName { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;

        // Auditoría
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true; // Propiedad para determinar si el usuario está activo

        // Personalización
        public string? FotoPerfilUrl { get; set; }

        // Relación con Cliente (si aplica)
        public Cliente? Cliente { get; set; }

        // Propiedad adicional para la última fecha de inicio de sesión
        public DateTime LastLoginDate { get; set; } = DateTime.UtcNow;  // Esta propiedad debe estar en ApplicationUser
    }
}
