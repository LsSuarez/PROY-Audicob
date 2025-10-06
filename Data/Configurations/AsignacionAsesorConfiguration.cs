using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class AsignacionAsesorConfiguration : IEntityTypeConfiguration<AsignacionAsesor>
    {
        public void Configure(EntityTypeBuilder<AsignacionAsesor> builder)
        {
            builder.ToTable("AsignacionesAsesores");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.AsesorUserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.AsesorNombre)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(a => a.FechaAsignacion)
                .IsRequired();

            // Relación: Asignación tiene un Cliente
            builder.HasOne(a => a.Cliente)
                .WithOne(c => c.AsignacionAsesor)
                .HasForeignKey<AsignacionAsesor>(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
