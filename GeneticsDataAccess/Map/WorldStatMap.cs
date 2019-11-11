using KamGenetics2020.Model;
using Microsoft.EntityFrameworkCore;

namespace GeneticsDataAccess.Map
{
   public class WorldStatMap
    {
        public static void Map(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WorldStat>(entity =>
                entity.Property(e => e.Modified).HasDefaultValueSql("CURRENT_TIMESTAMP"));
        }
    }
}
