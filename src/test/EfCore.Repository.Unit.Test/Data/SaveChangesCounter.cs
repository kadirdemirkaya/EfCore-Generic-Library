using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Repository.Unit.Test.Data
{
    public sealed class SaveChangesCounter : SaveChangesInterceptor
    {
        public int Calls { get; private set; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Calls++;
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Calls++;
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }
}
