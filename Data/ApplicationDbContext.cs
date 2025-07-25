using projectaardvarkx2.Entities;
using projectaardvarkx2.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projectaardvarkx2.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using projectaardvarkx2.Services;

namespace projectaardvarkx2.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public bool ShowDeleted { get; set; } = false;

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<WarrantyType> WarrantyTypes { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<AttachmentType> AttachmentTypes { get; set; }
        public DbSet<AttributeValue> Attributes { get; set; }

        private readonly ICurrentUserService _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();

            var now = DateTime.UtcNow;
            var user = GetCurrentUser();

            var stateList = new List<EntityState>
            {
                EntityState.Added,
                EntityState.Modified,
                EntityState.Deleted
            };

            foreach (var entry in entries.Where(x => stateList.Contains(x.State)))
            {
                //Soft Delete first, so Modified will trigger after
                if (entry.Entity is ISoftDelete softDeleteEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Deleted:
                            entry.State = EntityState.Modified;
                            softDeleteEntity.DeletedOn = now;
                            softDeleteEntity.DeletedBy = user;
                            break;
                    }
                }

                if (entry.Entity is IAuditableEntity auditableEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            auditableEntity.LastModifiedOn = now;
                            auditableEntity.LastModifiedBy = user;
                            break;

                        case EntityState.Added:
                            auditableEntity.CreatedOn = now;
                            auditableEntity.CreatedBy = user;
                            auditableEntity.LastModifiedOn = now;
                            auditableEntity.LastModifiedBy = user;
                            break;
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            RenameIdentityTables(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var foreignKey in entityType.GetForeignKeys())
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.SetNull;
                }
            }
        }

        protected void RenameIdentityTables(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "Users");
            });
            builder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable(name: "Roles");
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });
            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });
        }

        private string GetCurrentUser()
        {
            return _currentUserService.UserId ?? "system";
        }
    }
}
