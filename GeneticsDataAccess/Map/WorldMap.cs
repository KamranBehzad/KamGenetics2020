using KamGenetics2020.Model;
using Microsoft.EntityFrameworkCore;

namespace GeneticsDataAccess.Map
{
    public class WorldMap
    {
        public static void Map(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<World>(entity =>
                entity.Property(e => e.Modified).HasDefaultValueSql("CURRENT_TIMESTAMP"));
        }
    }
}
