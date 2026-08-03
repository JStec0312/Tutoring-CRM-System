using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tutoring.Domain.Students;

namespace Tutoring.Infrastructure.Persistence.Configurations
{

    internal sealed class StudentConfiguration
        : IEntityTypeConfiguration<Student>
    {
        public void Configure(
            EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("Students");
            builder.HasKey(student => student.Id);

            builder.Property(student => student.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(student => student.LastName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(student => student.Email)
                .HasMaxLength(320);

            builder.Property(student => student.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(student => student.CreatedAtUtc)
                .IsRequired();
        }
    }
}