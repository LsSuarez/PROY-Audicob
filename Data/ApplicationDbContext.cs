using Audicob.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Audicob.Data.Configurations;

namespace Audicob.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<LineaCredito> LineasCredito { get; set; }
        public DbSet<EvaluacionCliente> Evaluaciones { get; set; }
        public DbSet<AsignacionAsesor> AsignacionesAsesores { get; set; }
        public DbSet<PagoPendiente> PagoPendiente { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Deuda> Deudas { get; set; }
        public DbSet<HistorialCredito> HistorialCreditos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PagoPendiente>().ToTable("PagoPendiente");

            // Configuración explícita de la relación ApplicationUser <-> Cliente
            modelBuilder.Entity<ApplicationUser>()
                
                .HasOne(u => u.Cliente)
                .WithOne(c => c.Usuario)
                .HasForeignKey<Cliente>(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Aplicar configuraciones Fluent API
            modelBuilder.ApplyConfiguration(new ClienteConfiguration());
            modelBuilder.ApplyConfiguration(new PagoConfiguration());
            modelBuilder.ApplyConfiguration(new LineaCreditoConfiguration());
            modelBuilder.ApplyConfiguration(new EvaluacionClienteConfiguration());
            modelBuilder.ApplyConfiguration(new AsignacionAsesorConfiguration());
            modelBuilder.ApplyConfiguration(new DeudaConfiguration());
            modelBuilder.ApplyConfiguration(new TransaccionConfiguration());
        }
    }
}