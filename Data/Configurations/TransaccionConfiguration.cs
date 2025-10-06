using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class TransaccionConfiguration : IEntityTypeConfiguration<Transaccion>
    {
        public void Configure(EntityTypeBuilder<Transaccion> builder)
        {
            builder.ToTable("Transacciones");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.NumeroTransaccion)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Fecha)
                .IsRequired();

            builder.Property(t => t.Monto)
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder.Property(t => t.Estado)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Descripcion)
                .HasMaxLength(500);

            builder.Property(t => t.MetodoPago)
                .HasMaxLength(100);

            // Relación: Transacción pertenece a un Cliente
            builder.HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}