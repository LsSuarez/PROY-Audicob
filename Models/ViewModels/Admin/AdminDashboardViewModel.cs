using System;
using System.Collections.Generic;
using System.Linq;

namespace Audicob.Models.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        // Métricas generales
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int UsuariosNuevosEstaSemana { get; set; }
        public int UsuariosSuspendidos { get; set; } // Usuarios suspendidos o desactivados

        // Roles y distribución
        public Dictionary<string, int> UsuariosPorRol { get; set; } = new();
        public List<string> RolesDisponibles { get; set; } = new();
        public List<string> NombresRoles { get; set; } = new(); // Para gráficas
        public List<int> CantidadesPorRol { get; set; } = new(); // Para gráficas

        // Últimos usuarios registrados
        public List<UsuarioResumen> UsuariosRecientes { get; set; } = new();

        // Información de última actividad (opcional)
        public DateTime FechaUltimaActividad { get; set; } = DateTime.Now;

        // Agregar métricas para el uso en gráficas o estadísticas de la plataforma
        public List<UsuarioMetricas> UsuarioMetricas { get; set; } = new();  // Por ejemplo, usuarios por actividad semanal

        // Método para calcular usuarios activos
        public void CalcularUsuariosActivos(List<ApplicationUser> users)
        {
            // Filtra los usuarios activos, por ejemplo, usuarios que no están suspendidos
            UsuariosActivos = users.Count(u => u.Activo && u.LastLoginDate > DateTime.Now.AddMonths(-1));
        }

        // Método para calcular usuarios nuevos esta semana
        public void CalcularUsuariosNuevosEstaSemana(List<ApplicationUser> users)
        {
            // Filtra usuarios registrados esta semana
            UsuariosNuevosEstaSemana = users.Count(u => u.FechaRegistro >= DateTime.Now.AddDays(-7));
        }

        // Método para obtener usuarios suspendidos
        public void CalcularUsuariosSuspendidos(List<ApplicationUser> users)
        {
            // Filtra los usuarios suspendidos (esto depende de tu lógica de suspensión)
            UsuariosSuspendidos = users.Count(u => !u.Activo); // Usamos Activo en lugar de IsActive
        }
    }

    public class UsuarioResumen
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public DateTime? FechaUltimaActividad { get; set; } // Fecha de la última actividad del usuario
        public bool CorreoVerificado { get; set; } // Si el correo del usuario está verificado
    }

    // Métricas adicionales de usuarios
    public class UsuarioMetricas
    {
        public string Usuario { get; set; } = string.Empty;
        public int ActividadSemana { get; set; } // Métrica de actividad semanal
        public int ActividadMensual { get; set; } // Métrica de actividad mensual
        public int ActividadAnual { get; set; } // Métrica de actividad anual
    }
}
