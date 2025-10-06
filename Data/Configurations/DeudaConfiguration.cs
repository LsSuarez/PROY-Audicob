using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class DeudaConfiguration : IEntityTypeConfiguration<Deuda>
    {
        public void Configure(EntityTypeBuilder<Deuda> builder)
        {
            builder.ToTable("Deudas");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Monto)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(d => d.Intereses)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(d => d.PenalidadCalculada)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(d => d.TotalAPagar)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(d => d.FechaVencimiento)
                .IsRequired();

            // Relación: Deuda pertenece a un Cliente
            builder.HasOne(d => d.Cliente)
                .WithOne(c => c.Deuda)
                .HasForeignKey<Deuda>(d => d.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}