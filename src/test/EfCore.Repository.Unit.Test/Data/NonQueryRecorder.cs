using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace EfCore.Repository.Unit.Test.Data
{
    public sealed class NonQueryRecorder : DbCommandInterceptor
    {
        public List<string> CommandTexts { get; } = new();

        public List<object[]> ParameterValues { get; } = new();

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Record(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Record(command);
            return new ValueTask<InterceptionResult<int>>(result);
        }

        private void Record(DbCommand command)
        {
            CommandTexts.Add(command.CommandText);
            ParameterValues.Add(command.Parameters.Cast<DbParameter>().Select(p => p.Value).ToArray());
        }
    }
}
