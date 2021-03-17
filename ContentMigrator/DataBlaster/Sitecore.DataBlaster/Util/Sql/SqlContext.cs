using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Util.Sql
{
	/// <summary>
	/// Holder class for a connection and a transaction which supports working with embedded sql files.
	/// </summary>
    public class SqlContext
    {
	    private readonly Type _defaultSubject;

	    public SqlConnection Connection { get; private set; }
        public SqlTransaction Transaction { get; set; }

        public SqlContext(SqlConnection connection, Type defaultSubject = null)
        {
	        _defaultSubject = defaultSubject;
	        if (connection == null) throw new ArgumentNullException(nameof(connection));
            Connection = connection;
        }

        #region Reading embedded SQL files

        protected virtual StreamReader GetEmbeddedSqlReader(string relativePath, Type subject = null)
        {
	        if (subject == null) subject = _defaultSubject ?? this.GetType();

            var stream = subject.Assembly.GetManifestResourceStream(subject, relativePath);
            if (stream == null)
                throw new ArgumentException($"Could not locate embedded resource '{subject.Namespace}.{relativePath}' in '{subject.Assembly}'.");

            return new StreamReader(stream);
        }

        protected virtual string GetEmbeddedSql(string relativePath, Type subject = null)
        {
            using (var reader = GetEmbeddedSqlReader(relativePath, subject))
            {
                return reader.ReadToEnd();
            }
        }

        public virtual string GetEmbeddedSql<T>(string relativePath)
        {
            return GetEmbeddedSql(relativePath, typeof(T));
        }

        #endregion

        #region Manipulation SQL files

        public virtual string ReplaceOneLineSqlStringParameter(string sql, string name, string value)
        {
            return Regex.Replace(sql,
                @"(?<=DECLARE\s+" + name + @"\s+.*?=\s?').*?(?=')",
                value);
        }

        public virtual string ReplaceOneLineSqlBitParameter(string sql, string name, bool value)
        {
            return Regex.Replace(sql,
                @"(?<=DECLARE\s+" + name + @"\s+.*?=\s)[01]{1}",
                value ? "1" : "0");
        }
		
		#endregion

		#region Handling SQL files as filterable lines

		public IEnumerable<SqlLine> GetEmbeddedSqlLines(string relativePath, Type subject  =null)
        {
            using (var reader = GetEmbeddedSqlReader(relativePath, subject))
            {
                var begun = false;

                while (reader.Peek() >= 0)
                {
                    var line = reader.ReadLine();
                    if (!begun && line.ToUpper().Contains("-- BEGIN"))
                    {
                        begun = true;
                    }
                    else if (begun)
                    {
                        yield return line;
                    }
                }
            }
        }

        #endregion

        #region Command execution

        [SuppressMessage("Microsoft.Security", "CA2100", Justification = "No user parameters")]
        public virtual SqlCommand NewSqlCommand(string sql, int commandTimeout = int.MaxValue)
        {
            var command = new SqlCommand(sql, Connection)
            {
                CommandTimeout = int.MaxValue
            };
            if (Transaction != null)
                command.Transaction = Transaction;

            return command;
        }

        public virtual void ExecuteSql(string sql, int commandTimeout = int.MaxValue, 
            Action<SqlCommand> commandProcessor = null, bool splitOnGoCommands = false)
        {
            var commands = splitOnGoCommands
                ? Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                : new[] { sql };

            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command)) continue;

                using (var cmd = NewSqlCommand(command, commandTimeout: commandTimeout))
                {
                    commandProcessor?.Invoke(cmd);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public SqlDataReader ExecuteReader(string sql, int commandTimeout = int.MaxValue,
            Action<SqlCommand> commandProcessor = null)
        {
            using (var cmd = NewSqlCommand(sql, commandTimeout: commandTimeout))
            {
                commandProcessor?.Invoke(cmd);
                return cmd.ExecuteReader();
            }
        }

        public SqlDataReader ExecuteReader(IEnumerable<string> sqlLines, int commandTimeout = int.MaxValue,
            Action<SqlCommand> commandProcessor = null)
        {
            return ExecuteReader(string.Join("\n", sqlLines), commandTimeout, commandProcessor);
        }

	    public SqlDataReader ExecuteReader(IEnumerable<SqlLine> sqlLines, int commandTimeout = int.MaxValue,
		    Action<SqlCommand> commandProcessor = null)
	    {
		    return ExecuteReader(string.Join("\n", sqlLines.Select(x => x.ToString())), commandTimeout, commandProcessor);
	    }

		#endregion
	}
}