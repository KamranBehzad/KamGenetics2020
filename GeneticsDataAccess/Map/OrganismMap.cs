using KamGenetics2020.Model;
using Microsoft.EntityFrameworkCore;

namespace GeneticsDataAccess.Map
{
    public class OrganismMap
    {
        public static void Map(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organism>(entity =>
                entity.Property(e => e.Modified).HasDefaultValueSql("CURRENT_TIMESTAMP"));

            //modelBuilder.Entity<Organism>()
            //    .HasOne(r => r.Parent)
            //    .WithMany()
            //    .HasForeignKey(r => r.ParentId);

            //modelBuilder.Entity<Organism>()
            //    .HasOne(r => r.Group)
            //    .WithMany()
            //    .HasForeignKey(r => r.GroupId);

      }
    }
}
