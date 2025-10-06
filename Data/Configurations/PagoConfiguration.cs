using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            builder.ToTable("Pagos");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Fecha)
                .IsRequired();

            builder.Property(p => p.Monto)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(p => p.Validado)
                .IsRequired();

            builder.Property(p => p.Estado)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pendiente");

            builder.Property(p => p.Observacion)
                .HasMaxLength(300);

            builder.HasOne(p => p.Cliente)
                .WithMany(c => c.Pagos)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}