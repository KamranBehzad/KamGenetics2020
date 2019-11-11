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
            // Uncomment the following line to add EF Command logging
            //.UseLoggerFactory(ConsoleLoggerFactory)
            .UseSqlServer(ConnectionString);
         base.OnConfiguring(optionsBuilder);
      }


      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         base.OnModelCreating(modelBuilder);
         MapEntities(modelBuilder);
      }

      private static void MapEntities(ModelBuilder modelBuilder)
      {
         WorldMap.Map(modelBuilder);
         WorldStatMap.Map(modelBuilder);
         OrganismMap.Map(modelBuilder);
         GeneMap.Map(modelBuilder);
      }

      public DbSet<World> Worlds { get; set; }
      public DbSet<WorldStat> WorldStats { get; set; }
      public DbSet<Organism> Organisms { get; set; }
      public DbSet<Gene> Genes { get; set; }
   }}
