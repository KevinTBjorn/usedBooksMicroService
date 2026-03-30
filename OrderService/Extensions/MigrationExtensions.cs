using Microsoft.EntityFrameworkCore;
using OrderService.Data;

namespace OrderService.Extensions
{
    public static class MigrationExtensions
    {
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            db.Database.Migrate();
        }
    }
}
