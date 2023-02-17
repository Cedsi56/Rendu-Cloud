using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using jadorelecloudgaming.Models;

namespace jadorelecloudgaming.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<jadorelecloudgaming.Models.VirtualMachine> VirtualMachines { get; set; }
    }
}