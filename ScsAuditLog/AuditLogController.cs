using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml;
using Lucene.Net.Search;
using Rainbow.Model;
using Sidekick.AuditLog.Model;
using Sidekick.AuditLog.Model.Interface;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.SecurityModel;
using Sidekick.Core;
using Sidekick.Core.ContentTree;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;

namespace Sidekick.AuditLog
{
	public class AuditLogController : ScsController
	{
		private static readonly ISitecoreDataAccessService _sitecoreDataAccessSerivce = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();	
		private static readonly ContentTreeNode _root = new ContentTreeNode() { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		private static object _locker = new object();
		public static ContentTreeNode Root
		{
			get
			{
				if (!_root.Nodes.Any())
				{
					lock (_locker)
					{
						if (!_root.Nodes.Any())
						{
							foreach (IItemData child in _sitecoreDataAccessSerivce.GetChildren(_sitecoreDataAccessSerivce.GetRootItemData(_root.DatabaseName)))
							{
								_root.Nodes.Add(new ContentTreeNode(child, false));
							}
						}
					}
				}
				return _root;
			}
		}
		private readonly List<string> _luceneSpecialChars = new List<string>() { "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "\\" };

		[LoggedIn]
		[ActionName("alcontenttree.scsvc")]
		public ActionResult ContentTree(ContentTreeModel data)
		{
			return ScsJson(GetContentTree(data));
		}

		[LoggedIn]
		[ActionName("alqueryactivity.scsvc")]
		public ActionResult UserActivity(ActivityDataModel data)
		{
			return ScsJson(GetUserActivity(data));
		}

		[LoggedIn]
		[ActionName("aleventtypes.scsvc")]
		public ActionResult Types()
		{
			return ScsJson(GetTypes());
		}

		[LoggedIn]
		[ActionName("alautocomplete.scsvc")]
		public ActionResult AutoComplete(AutocompleteModel data)
		{
			return ScsJson(GetAutocomplete(data));
		}

		[LoggedIn]
		[ActionName("alactivitydata.scsvc")]
		public ActionResult ActivityData(ActivityDataModel data)
		{
			return ScsJson(GetActivityData(data));
		}

		[LoggedIn]
		[ActionName("alusers.scsvc")]
		public ActionResult ActiveUsers()
		{
			return ScsJson(GetUsers());
		}

		[LoggedIn]
		[ActionName("alrebuild.scsvc")]
		public ActionResult Rebuild()
		{
			return ScsJson(RebuildLog());
		}

		[LoggedIn]
		[ActionName("alrebuildstatus.scsvc")]
		public ActionResult RebuildStatus()
		{
			return ScsJson(RebuildLogStatus());
		}
		[LoggedIn]
		[ActionName("algetdatabases.scsvc")]
		public ActionResult GetDatabases()
		{
			return ScsJson(_sitecoreDataAccessSerivce.GetAllDatabases().Where(x => x.Name != "core" && x.Name != "filesystem").ToDictionary(x => x.Name, x => x.Name == "master"));
		}

		private object RebuildLogStatus()
		{
			return AuditLogger.Log.Rebuilt;
		}

		private object RebuildLog()
		{
			AuditLogger.Log.Rebuild();
			return true;
		}

		private object GetUsers()
		{
			return AuditLogger.Current.GetUsers();
		}

		private object GetActivityData(ActivityDataModel data)
		{
			DateTime start = DateTime.Parse(data.Start);
			DateTime end = DateTime.Parse(data.End);
			StringBuilder sb = new StringBuilder();
			sb.Append(BuildArrayQuery(data.Filters, data.Field, data.Databases));
			if (sb.Length > 0)
				sb.Append(" AND ");
			sb.Append(BuildArrayQuery(data.EventTypes, "event", data.Databases));
			if (sb.Length > 0)
				sb.Append(" AND ");
			sb.Append($"date:[{start:yyyyMMdd} TO {end:yyyyMMdd}]");
			IndexSearcher searcher = AuditLogger.Current.GetSearcher();
			TopDocs results = AuditLogger.Current.Query(sb.ToString(), searcher);
			dynamic ret = new ExpandoObject();
			ret.total = results.TotalHits;
			ret.perPage = 20;
			ret.results = getResults(results, data.Page, searcher);
			return ret;
		}

		private string BuildArrayQuery(IEnumerable<object> terms, string key, Dictionary<string,bool> databases)
		{
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
				if (term != "*")
					sb.Append($"{key}:{(key=="path" ? '"'+ReplaceReservedChars(term)+'"' : ReplaceReservedChars(term))} OR ");
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
			sb.Append("database:");
			sb.Append(string.Join(" OR database:", databases.Where(x => x.Value).Select(x => x.Key)));
			sb.Append(")");

			return sb.ToString();
		}
		private IEnumerable<IAuditEntry> getResults(TopDocs ids, int page, IndexSearcher searcher)
		{
			int skip = page * 20;
			return ids.ScoreDocs.Reverse().Skip(skip).Take(20).Select(x => new BasicAuditEntry(searcher.Doc(x.Doc), x.Doc));
		}
		private object GetAutocomplete(AutocompleteModel data)
		{
			return AuditLogger.Current.AutoComplete(data.Text, data.Start, data.End, data.EventTypes);
		}


		private object GetTypes()
		{
			return AuditLogger.Current.GetAllEventTypes();
		}

		private object GetContentTree(ContentTreeModel data)
		{
			if (string.IsNullOrWhiteSpace(data.Id)) return Root;

			IItemData item = _sitecoreDataAccessSerivce.GetItemData(data.Id, data.Database);
			return new ContentTreeNode(item);			
		}

		private object GetUserActivity(ActivityDataModel data)
		{
			AuditGraph ret = new AuditGraph();
			int max = 0;
			DateTime start = DateTime.Parse(data.Start);
			DateTime end = DateTime.Parse(data.End);
			TimeSpan range = end.Subtract(start);
			var filter = BuildArrayQuery(data.Filters, data.Field, data.Databases);
			List<string> dates = new List<string>();
			for (int i = 0; i <= range.Days; i++)
				dates.Add(start.AddDays(i).ToString("yyyyMMdd"));
			var searcher = AuditLogger.Current.GetSearcher();
			Dictionary<string, AuditGraphEntry> entries = new Dictionary<string, AuditGraphEntry>();
			foreach (string eventType in data.EventTypes)
			{
				AuditGraphEntry gentry = new AuditGraphEntry() { Color = AuditLogger.Current.GetEventType(eventType).Color };
				foreach (string date in dates)
				{
					var results = AuditLogger.Current.Query($"date:{date} AND event:{eventType} {(filter.Length > 0 ? "AND " : "") + filter}", searcher);
					if (results.TotalHits > max)
						max = results.TotalHits;
					gentry.Coordinates.Add(new AuditGraphCoordinates() { X = date.Insert(6, "-").Insert(4, "-"), Y = results.TotalHits.ToString() });
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
		private string ReplaceReservedChars(string rawInput)
		{
			return _luceneSpecialChars.Aggregate(rawInput, (current, c) => current.Replace(c, "*"));

		}

	}
}
