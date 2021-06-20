using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Dynamic;
using System.Linq;
using System.Text;
using Sidekick.AuditLog.Model;
using Sidekick.AuditLog.Model.Interface;
using Sidekick.Core.Services.Interface;
using Sitecore.Data.Items;

namespace Sidekick.AuditLog.Core
{
	public class SqlAuditLog : IAuditLog
	{
		private readonly int _logDays;
		private readonly int _recordDays;
		private readonly bool _logAnonymousEvents;
		private static readonly ISitecoreDataAccessService _sitecoreDataAccessSerivce = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		private static readonly IJsonSerializationService _jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();

		private readonly Dictionary<string, IEventType> _types = new Dictionary<string, IEventType>();
		private readonly List<string> _sqlSpecialChars = new List<string>() { "'", "--", "(", ")", "[", "]" };

		public SqlAuditLog(int daysToKeepLog, int daysToKeepRecords, bool logAnonymousEvents)
		{
			_logDays = daysToKeepLog;
			_recordDays = daysToKeepRecords;
			_logAnonymousEvents = logAnonymousEvents;
		}

		public IEnumerable<KeyValuePair<string, int>> AutoComplete(string text, string start, string end, List<object> eventTypes)
		{
			return null;
		}

		public object GetActivityData(ActivityDataModel data)
		{
			using (var db = new SqlAuditLogDataContext())
			{
				DateTime start = DateTime.Parse(data.Start);
				DateTime end = DateTime.Parse(data.End).AddDays(1);
				StringBuilder sb = new StringBuilder();
				sb.Append(BuildArrayQuery(data.Filters, data.Field, data.Databases));
				if (sb.Length > 0)
					sb.Append(" AND ");
				sb.Append(BuildArrayQuery(data.EventTypes, "event", data.Databases));
				var filterExpression = sb.ToString();

				dynamic ret = new ExpandoObject();

				var pageSize = 20;

				var countQuery = "SELECT count(*) FROM [dbo].[AuditEntry] " +
					"WHERE {0} <= [Timestamp] AND [Timestamp] <= {1} AND " +
					filterExpression;

				var selectQuery = "SELECT * FROM [dbo].[AuditEntry] " +
					"WHERE {0} <= [Timestamp] AND [Timestamp] <= {1} AND " +
					filterExpression +
					" ORDER BY [Timestamp] desc" +
					" OFFSET ({2}) ROWS FETCH NEXT ({3}) ROWS ONLY";
#if DEBUG
				Sitecore.Diagnostics.Log.Info($"SCS: {selectQuery}", this);
#endif

				var sqlAuditLogEntriesCount = db.ExecuteQuery<int>(
					countQuery,
					start,
					end)
					.First();

				var sqlAuditLogUser = db.ExecuteQuery<SqlAuditLogUser>("SELECT * FROM dbo.Users").ToList();

				ret.total = sqlAuditLogEntriesCount;
				ret.perPage = pageSize;

				int skip = data.Page * pageSize;

				var sqlAuditLogEntries = db.ExecuteQuery<SqlAuditLogEntry>(
					selectQuery,
					start,
					end,
					skip,
					pageSize);

				ret.results = sqlAuditLogEntries
					.Select(x => new BasicAuditEntry
					{
						Uid = x.Id.ToString(),
						Id = x.ItemId?.ToString() ?? "",
						Path = x.Path,
						Database = x.Database,
						Color = x.Color,
						EventId = x.EventId,
						Icon = x.Icon,
						Label = x.Label,
						Note = x.Note,
						TimeStamp = x.TimeStamp,
						User = sqlAuditLogUser.FirstOrDefault(u => u.Id == x.UserId)?.Username ?? x.UserId.ToString()
					})
					.ToList();
				return ret;
			}
		}

		public object GetUserActivity(ActivityDataModel data)
		{
			using (var db = new SqlAuditLogDataContext())
			{

				AuditGraph ret = new AuditGraph();
				Dictionary<string, AuditGraphEntry> entries = new Dictionary<string, AuditGraphEntry>();
				int max = 0;
				DateTime start = DateTime.Parse(data.Start);
				DateTime end = DateTime.Parse(data.End).AddDays(1);
				TimeSpan range = end.Subtract(start);
				var filter = BuildArrayQuery(data.Filters, data.Field, data.Databases);
				List<DateTime> dates = new List<DateTime>();
				for (int i = 0; i <= range.Days; i++)
					dates.Add(start.AddDays(i));
				foreach (string eventType in data.EventTypes)
				{
					AuditGraphEntry gentry = new AuditGraphEntry() { Color = GetEventType(eventType).Color };
					foreach (var date in dates)
					{
						var query = "SELECT count(*) FROM [dbo].[AuditEntry] " +
							"WHERE {0} <= [Timestamp] AND [Timestamp] <= {1} AND [EventId] = {2} AND " +
							filter;

						var sqlAuditLogEntries = db.ExecuteQuery<int>(
							query,
							date,
							date.AddDays(1),
							eventType)
							.ToList();
						var count = sqlAuditLogEntries.First();
						if (count > max)
							max = count;
						gentry.Coordinates.Add(new AuditGraphCoordinates() { X = date.ToString("yyyy-MM-dd"), Y = count.ToString() });
					}
					entries[eventType] = gentry;
				}

				ret.YMax = max.ToString();
				ret.YMin = "0";
				ret.XMin = start.ToString("yyyy-MM-dd");
				ret.XMax = end.ToString("yyyy-MM-dd");
				ret.GraphEntries = entries;
				ret.LogEntries = null;// timeSet;
				return ret;
			}
		}

		public HashSet<string> GetUsers()
		{
			using (var db = new SqlAuditLogDataContext())
			{
				var sqlAuditLogUser = db.ExecuteQuery<SqlAuditLogUser>("SELECT * FROM dbo.Users");
				var result = sqlAuditLogUser
					.Select(x => x.Username)
					.ToList();
				return new HashSet<string>(result);
			}
		}

		public void RegisterEventType(IEventType eventType)
		{
			_types.Add(eventType.Id, eventType);
		}

		public IEventType GetEventType(string id)
		{
			if (_types.ContainsKey(id))
				return _types[id];
			return null;
		}

		public IDictionary<string, IEventType> GetAllEventTypes()
		{
			return _types;
		}

		public void Log(Item item, string typeId, string note = "")
		{
			Log(item, GetEventType(typeId), note);
		}

		public void Log(Item item, IEventType type, string note = "")
		{
			Log(item, type.Id, type.Color, type.Label, note);
		}

		public void Log(Item item, string eventId, string color, string label, string note = "")
		{
			try
			{
				ItemAuditEntry entry = new ItemAuditEntry(eventId, label, color, item);
				if (entry.User.ToLower().Contains("anonymous") && !_logAnonymousEvents) return;
				entry.Note = note;
				StringBuilder sb = new StringBuilder();
				if (item != null)
				{
					var fieldList = item.Fields.Where(x => !x.Name.StartsWith("__")).Select(x => x.Value);
					foreach (string str in fieldList)
					{
						sb.Append(str);
						sb.Append("|");
					}
				}
				Log(entry, sb.ToString());
			}
			catch (Exception e)
			{
				Sitecore.Diagnostics.Log.Error("SCS: issue writing item log audit log", e, this);
			}
		}

		public void Log(IAuditEntry entry, string content = "", bool newRecord = true)
		{
			Guid userId;

			using (var db = new SqlAuditLogDataContext())
			{
				var username = entry.User.ToLower();
				var sqlAuditLogUser = db.ExecuteQuery<SqlAuditLogUser>("SELECT * FROM dbo.Users WHERE Username = {0}", username).ToList();
				if (!sqlAuditLogUser.Any())
				{
					userId = Guid.NewGuid();
					db.ExecuteCommand("INSERT INTO dbo.Users (Id, Username) VALUES ({0}, {1})", userId, username);
				}
				else
				{
					userId = sqlAuditLogUser.First().Id;
				}
			}

			var role = _jsonSerializationService.SerializeObject(entry.Role);

			using (var db = new SqlAuditLogDataContext())
			{
				db.ExecuteCommand(
				"INSERT INTO [dbo].[AuditEntry] " +
				"([Id], [UserId], [Role], [ItemId], [Database], [Path], [TimeStamp], [EventId], [Note], [Label], [Color], [Icon], [Content]) " +
				"VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12})",
				Guid.NewGuid(),
				userId,
				role,
				entry.Id?.ToLower() ?? "",
				entry.Database ?? "",
				entry.Path ?? "",
				DateTime.Now,
				entry.EventId ?? "",
				entry.Note ?? "",
				entry.Label ?? "",
				entry.Color ?? "",
				entry.Icon ?? "",
				content ?? ""
				);
			}

		}

		public void Rebuild()
		{

		}

		public int RebuildLogStatus()
		{
			return -1;
		}

		private string BuildArrayQuery(IEnumerable<object> terms, string key, Dictionary<string, bool> databases)
		{
			if (key == "event") key = "EventId";
			if (key == "id") key = "ItemId";
			if (key == "user") key = "UserId";
			if (key == "descendants")
			{
				var tmp = new List<object>();
				foreach (var id in terms)
				{
					tmp.Add(_sitecoreDataAccessSerivce.GetItemData(id.ToString()).Path.Trim('/'));
				}
				terms = tmp;
				key = "path";
			}
			StringBuilder sb = new StringBuilder("(");
			foreach (string term in terms)
			{
				var replacedTerm = ReplaceReservedChars(term);
				if (term != "*" && (key == "content" || key == "path"))
					sb.Append($"[{key}] like '%{replacedTerm}%' OR ");
				else if (term != "*" && key == "ItemId")
					sb.Append($"[{key}] = '{replacedTerm.Replace('*', '-')}' OR ");
				else if (term != "*" && key == "UserId")
					sb.Append($"[{key}] = '{GetUserId(replacedTerm)}' OR ");
				else if (term != "*")
					sb.Append($"[{key}] = '{replacedTerm}' OR ");
			}
			if (sb.Length > 1)
			{
				sb.Remove(sb.Length - 4, 4);
				sb.Append(")");
			}

			if (!databases.Any(x => x.Value)) return sb.ToString();
			if (sb.Length > 1)
			{
				sb.Append(" AND (");
			}
			sb.Append("[database] = ");
			sb.Append(string.Join(" OR [database] = ", databases.Where(x => x.Value).Select(x => $"'{x.Key}'")));
			sb.Append(")");

			return sb.ToString();
		}

		private Guid? GetUserId(string username)
		{
			using (var db = new SqlAuditLogDataContext())
			{
				var sqlAuditLogUser = db.ExecuteQuery<SqlAuditLogUser>($"SELECT * FROM dbo.Users WHERE [Username] = '{username}'");
				return sqlAuditLogUser.FirstOrDefault()?.Id;
			}
		}

		private string ReplaceReservedChars(string rawInput)
		{
			return _sqlSpecialChars.Aggregate(rawInput, (current, c) => current.Replace(c, string.Empty));
		}
	}

	public class SqlAuditLogDataContext : DataContext
	{
		private static readonly string DatabaseConnectionString = ConfigurationManager.ConnectionStrings["sidekick.auditlog"]?.ConnectionString;

		public SqlAuditLogDataContext() : base(DatabaseConnectionString)
		{
		}
	}

}
