using KamGenetics2020.Model;
using Microsoft.EntityFrameworkCore;

namespace GeneticsDataAccess.Map
{
    public class GeneMap
    {
        public static void Map(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Gene>(entity =>
                entity.Property(e => e.Modified).HasDefaultValueSql("CURRENT_TIMESTAMP"));
        }
    }
}
