using System;
using System.Collections.Generic;
using System.Linq;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql
{
    public static class SqlLineExtensions
    {
        public static IEnumerable<SqlLine> RemoveParameterLineIf(this IEnumerable<SqlLine> sqlLines, Func<bool> predicate, string parameterName)
        {
            return sqlLines
                .Where(line => !predicate() || line.ToString().IndexOf(parameterName, StringComparison.OrdinalIgnoreCase) < 0);
        }

        public static IEnumerable<SqlLine> ExpandParameterLineIf(this IEnumerable<SqlLine> sqlLines, Func<bool> predicate,
            string parameterName, string parameterValue)
        {
            foreach (var line in sqlLines)
            {
                if (!predicate())
                {
                    yield return line;
                    continue;
                }

                var pos = line.ToString().IndexOf(parameterName, StringComparison.OrdinalIgnoreCase);
                if (pos < 0)
                {
                    yield return line;
                    continue;
                }

                yield return line.ToString().Substring(0, pos) + parameterValue + line.ToString().Substring(pos + parameterName.Length);
            }
        }
    }
}