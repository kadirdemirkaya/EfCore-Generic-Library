using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace EfCore.Repository.Unit.Test.Data
{
    public sealed class CommandCounter : DbCommandInterceptor
    {
        public int Synchronous { get; private set; }

        public int Asynchronous { get; private set; }

        public void Reset()
        {
            Synchronous = 0;
            Asynchronous = 0;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Synchronous++;
            return result;
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Synchronous++;
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Asynchronous++;
            return new ValueTask<InterceptionResult<DbDataReader>>(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Asynchronous++;
            return new ValueTask<InterceptionResult<object>>(result);
        }
    }
}
