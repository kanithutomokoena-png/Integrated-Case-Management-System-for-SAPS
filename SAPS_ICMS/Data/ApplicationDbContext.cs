using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAPS_ICMS.Models;

namespace SAPS_ICMS.Data
{
    
    public class ApplicationDbContext : IdentityDbContext<Officer>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
        public DbSet<Case> Cases { get; set; }
        public DbSet<Evidence> Evidences { get; set; }
        public DbSet<Witness> Witnesses { get; set; }
        public DbSet<CaseClosureRequest> CaseClosureRequests { get; set; }
        public DbSet<CaseEditRequest> CaseEditRequests { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            base.OnModelCreating(modelbuilder);

            modelbuilder.Entity<Case>()
                .HasMany(c => c.Witnesses)
                .WithOne(c => c.Case)
                .HasForeignKey(c => c.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
    
}
