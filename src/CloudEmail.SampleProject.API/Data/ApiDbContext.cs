using CloudEmail.ApiAuthentication.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace CloudEmail.SampleProject.API.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ReadApiDbContext> options) : base(options)
        {
        }

        public ApiDbContext(DbContextOptions<WriteApiDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationRegistration> ApplicationRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationRegistration>()
                .HasIndex(pp => new { pp.Name })
                .IsUnique();

            modelBuilder.Entity<ApplicationRegistration>()
                .Property(ar => ar.Name)
                .IsRequired();
        }
    }

    [ExcludeFromCodeCoverage]
    public class ReadApiDbContext : ApiDbContext
    {
        public ReadApiDbContext() : this(new DbContextOptions<ReadApiDbContext>())
        {
        }

        public ReadApiDbContext(DbContextOptions<ReadApiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationRegistration>()
                .HasIndex(pp => new { pp.Name })
                .IsUnique();

            modelBuilder.Entity<ApplicationRegistration>()
                .Property(ar => ar.Name)
                .IsRequired();
        }
    }

    [ExcludeFromCodeCoverage]
    public class WriteApiDbContext : ApiDbContext
    {
        public WriteApiDbContext() : this(new DbContextOptions<WriteApiDbContext>())
        {
        }

        public WriteApiDbContext(DbContextOptions<WriteApiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationRegistration>()
                .HasIndex(pp => new { pp.Name })
                .IsUnique();

            modelBuilder.Entity<ApplicationRegistration>()
                .Property(ar => ar.Name)
                .IsRequired();
        }
    }
}
