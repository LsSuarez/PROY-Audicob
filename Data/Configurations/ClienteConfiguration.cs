using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Documento)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.IngresosMensuales)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(c => c.DeudaTotal)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(c => c.FechaActualizacion)
                .IsRequired();

            builder.Property(c => c.UserId)
                .HasMaxLength(450);

            // ELIMINAMOS la configuración de relación con Usuario
            // (ahora está en ApplicationDbContext)

            // Relaciones con otras entidades
            builder.HasMany(c => c.Pagos)
                .WithOne(p => p.Cliente)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Evaluaciones)
                .WithOne(e => e.Cliente)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.LineaCredito)
                .WithOne(l => l.Cliente)
                .HasForeignKey<LineaCredito>(l => l.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.AsignacionAsesor)
                .WithMany(a => a.Clientes)
                .HasForeignKey(c => c.AsignacionAsesorId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}