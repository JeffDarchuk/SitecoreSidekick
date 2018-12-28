using System;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql
{
	public class SqlLine
	{
		private readonly string _line;

		public SqlLine(string line)
		{
			if (line == null) throw new ArgumentNullException(nameof(line));
			_line = line;
		}

		public override string ToString()
		{
			return _line;
		}

		public static implicit operator SqlLine(string line)
		{
			return line == null ? null : new SqlLine(line);
		}

		public static implicit operator string(SqlLine sqlLine)
		{
			return sqlLine.ToString();
		}
	}
}