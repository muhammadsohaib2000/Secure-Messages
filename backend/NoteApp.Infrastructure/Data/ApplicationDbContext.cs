using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoteApp.Domain.Entities;

namespace NoteApp.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ILogger<ApplicationDbContext> _logger;

        public DbSet<Note> Notes => Set<Note>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger<ApplicationDbContext> logger)
            : base(options)
        {
            _logger = logger;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            _logger.LogInformation("Configuring entity model for Notes table");

            modelBuilder.Entity<Note>(entity =>
            {
                entity.ToTable("Notes");
                entity.HasKey(e => e.Id);
                
                // Specific configuration for PostgreSQL
                entity.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasColumnType("text");
                
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("now()");
                
                entity.Property(e => e.ExpiresAt)
                    .HasColumnType("timestamp with time zone");
                
                entity.Property(e => e.IsViewed)
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
            });

            _logger.LogInformation("Entity model configuration completed");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Saving changes to database");
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred saving changes to database");
                throw;
            }
        }
    }
} 