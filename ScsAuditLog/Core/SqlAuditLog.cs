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
		private readonly Dictionary<string, IEventType> _types = new Dictionary<string, IEventType>();
		private static readonly ISitecoreDataAccessService _sitecoreDataAccessSerivce = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		private readonly List<string> _sqlSpecialChars = new List<string>() { "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "\\" };


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

				dynamic ret = new ExpandoObject();

				var pageSize = 20;

				//var totalCount = db.ExecuteQuery<int>(
				//	"SELECT count(*) FROM [dbo].[AuditEntry] " +
				//	"WHERE {0} <= [Timestamp] AND [Timestamp] <= {1}",
				//	start,
				//	end)
				//	.ToList();
				var query = "SELECT * FROM [dbo].[AuditEntry] " +
					"WHERE {0} <= [Timestamp] AND [Timestamp] <= {1} AND " +
					sb.ToString() +
					" ORDER BY [Timestamp] desc";
				Sitecore.Diagnostics.Log.Info($"SCS: {query}", this);

				var sqlAuditLogEntries = db.ExecuteQuery<SqlAuditLogEntry>(
					query,
					start, 
					end)
					.ToList();

				var sqlAuditLogUser = db.ExecuteQuery<SqlAuditLogUser>("SELECT * FROM dbo.Users").ToList();

				ret.total = sqlAuditLogEntries.Count();
				ret.perPage = pageSize;

				int skip = data.Page * pageSize;

				ret.results = sqlAuditLogEntries
					.Skip(skip)
					.Take(pageSize)
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
				List<string> dates = new List<string>();
				for (int i = 0; i <= range.Days; i++)
					dates.Add(start.AddDays(i).ToString("yyyyMMdd"));
				foreach (string eventType in data.EventTypes)
				{
					AuditGraphEntry gentry = new AuditGraphEntry() { Color = GetEventType(eventType).Color };
					foreach (string date in dates)
					{
						var query = "SELECT count(*) FROM [dbo].[AuditEntry] " +
							"WHERE {0} <= [Timestamp] AND [Timestamp] <= {1} AND [EventId] = {2} AND " +
							filter;
						//Sitecore.Diagnostics.Log.Info($"SCS: {query}", this);

						var sqlAuditLogEntries = db.ExecuteQuery<int>(
							query,
							start,
							end,
							eventType)
							.ToList();
						var count = sqlAuditLogEntries.First();
						if (count > max)
							max = count;
						gentry.Coordinates.Add(new AuditGraphCoordinates() { X = date.Insert(6, "-").Insert(4, "-"), Y = count.ToString() });
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

			using (var db = new SqlAuditLogDataContext())
			{
				db.ExecuteCommand(
				"INSERT INTO [dbo].[AuditEntry] " +
				"([Id], [UserId], [Role], [ItemId], [Database], [Path], [TimeStamp], [EventId], [Note], [Label], [Color], [Icon], [Content]) " +
				"VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12})",
				Guid.NewGuid(),
				userId,
				"",//entry.Role,
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
			return 1;
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
				if (term != "*" && (key == "content" || key == "path"))
					sb.Append($"[{key}] like '%{term}%' OR ");
				else if (term != "*" && key == "ItemId")
					sb.Append($"[{key}] = '{term.Replace('*', '-')}' OR ");
				else if (term != "*" && key == "UserId")
					sb.Append($"[{key}] = '{GetUserId(term)}' OR ");
				else if (term != "*")
					sb.Append($"[{key}] = '{term}' OR ");
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
			return _sqlSpecialChars.Aggregate(rawInput, (current, c) => current.Replace(c, @"\"));
		}
	}

	public class SqlAuditLogDataContext : DataContext
	{
		private static readonly string DatabaseConnectionString = ConfigurationManager.ConnectionStrings["scsauditlog"]?.ConnectionString;

		public SqlAuditLogDataContext() : base(DatabaseConnectionString)
		{
		}
	}

	public class SqlAuditLogUser
	{
		public Guid Id { get; set; }
		public string Username { get; set; }
	}

	public class SqlAuditLogEntry
	{
		public Guid Id { get; set; }
		public Guid? UserId { get; set; }
		public string Role { get; set; }
		public Guid? ItemId { get; set; }
		public string Database { get; set; }
		public string Path { get; set; }
		public DateTime TimeStamp { get; set; }
		public string EventId { get; set; }
		public string Note { get; set; }
		public string Label { get; set; }
		public string Color { get; set; }
		public string Icon { get; set; }
	}

}
