using GeneticsDataAccess.Map;
using KamGenetics2020.Model;
using MainDataAccess;
using Microsoft.EntityFrameworkCore;

namespace GeneticsDataAccess
{
    public class GeneticsDbContext : SqlDbContext
    {
        public GeneticsDbContext(string connectionString, bool enforceDbRecreation = false) : base(connectionString, enforceDbRecreation)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
               .UseSqlServer(ConnectionString);
            base.OnConfiguring(optionsBuilder);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignore creating data tables for any entities here
            //modelBuilder.Ignore<Organism>();
            //modelBuilder.Ignore<OrganismStat>();
            //modelBuilder.Ignore<LogOrganism>();
            
            base.OnModelCreating(modelBuilder);
            MapEntities(modelBuilder);
        }

        protected override void MapEntities(ModelBuilder modelBuilder)
        {
            WorldMap.Map(modelBuilder);
            WorldStatMap.Map(modelBuilder);
            OrganismMap.Map(modelBuilder);
            GeneMap.Map(modelBuilder);
        }

        public DbSet<World> Worlds { get; set; }
        //public DbSet<WorldStat> WorldStats { get; set; }
        //public DbSet<Organism> Organisms { get; set; }
        //public DbSet<Gene> Genes { get; set; }
    }
}
