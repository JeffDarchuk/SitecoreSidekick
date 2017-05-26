using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using ScsAuditLog.Model;
using SitecoreSidekick.Handlers;
using System.Dynamic;
using System.Reflection;
using System.Web.Mvc;
using System.Xml;
using Lucene.Net.Search;
using ScsAuditLog.Core;
using ScsAuditLog.Model.Interface;
using SitecoreSidekick.ContentTree;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;

namespace ScsAuditLog
{
	public class AuditLogHandler : ScsHandler
	{
		private static readonly ContentTreeNode Root = new ContentTreeNode() { DatabaseName = "master", DisplayName = "Root", Icon = "/~/icon/Applications/32x32/media_stop.png", Open = true, Nodes = new List<ContentTreeNode>() };
		public override string Directive { get; set; } = "aldirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath { get; set; } = "ScsAuditLog.Resources";
		public override string Icon { get; } = "/scs/alportfoliofolder.png";
		public override string Name { get; } = "Audit Log";
		public override string CssStyle { get; } = "width:100%;min-width:900px";
		private readonly List<string> _luceneSpecialChars = new List<string>() { "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "\\" };

		public AuditLogHandler()
		{
		}

		public AuditLogHandler(string keepBackups, string keepRecords, string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
			Database db = Factory.GetDatabase(Root.DatabaseName, false);
			if (db != null)
			{
				foreach (Item child in db.GetRootItem().Children)
				{
					Root.Nodes.Add(new ContentTreeNode(child, false));
				}
			}
			int backup;
			int duration;
			if (!int.TryParse(keepBackups, out backup))
				backup = 0;
			if (!int.TryParse(keepRecords, out duration))
				duration = 0;
			AuditLogger.Log = new AuditLog(backup, duration);
		}

		public override ActionResult ProcessRequest(HttpContextBase context, string filename, dynamic data)
		{
			if (filename == "alcontenttree.scsvc")
				ReturnJson(context, GetContentTree(data));
			else if (filename == "alqueryactivity.scsvc")
				ReturnJson(context, GetUserActivity(data));
			else if (filename == "aleventtypes.scsvc")
				ReturnJson(context, GetTypes());
			else if (filename == "alautocomplete.scsvc")
				ReturnJson(context, GetAutocomplete(data));
			else if (filename == "alactivitydata.scsvc")
				ReturnJson(context, GetActivityData(data));
			else if (filename == "alusers.scsvc")
				ReturnJson(context, GetUsers());
			else if (filename == "alrebuild.scsvc")
				ReturnJson(context, RebuildLog(data));
			else if (filename == "alrebuildstatus.scsvc")
				ReturnJson(context, RebuildLogStatus(data));
			return null;
		}

		private object RebuildLogStatus(HttpContextBase context)
		{
			return AuditLogger.Log.Rebuilt;
		}

		private object RebuildLog(HttpContextBase context)
		{
			AuditLogger.Log.Rebuild();
			return true;
		}

		private object GetUsers()
		{
			return AuditLogger.Current.GetUsers();
		}

		private object GetActivityData(dynamic data)
		{
			DateTime start = DateTime.Parse(data.start);
			DateTime end = DateTime.Parse(data.end);
			StringBuilder sb = new StringBuilder();
			sb.Append(BuildArrayQuery((List<object>) data.filters, data.field));
			if (sb.Length > 0)
				sb.Append(" AND ");
			sb.Append(BuildArrayQuery((List<object>)data.eventTypes, "event"));
			if (sb.Length > 0)
				sb.Append(" AND ");
			sb.Append($"date:[{start:yyyyMMdd} TO {end:yyyyMMdd}]");
			IndexSearcher searcher = AuditLogger.Current.GetSearcher();
			TopDocs results = AuditLogger.Current.Query(sb.ToString(), searcher);
			dynamic ret = new ExpandoObject();
			ret.total = results.TotalHits;
			ret.perPage = 20;
			ret.results = getResults(results, (int)data.page, searcher);
			return ret;
		}

		private string BuildArrayQuery(IEnumerable<object> terms, string key)
		{
			StringBuilder sb = new StringBuilder("(");
			foreach (string term in terms)
			{
				if (term != "*")
					sb.Append($"{key}:{ReplaceReservedChars(term)} OR ");
			}
			if (sb.Length > 1)
			{
				sb.Remove(sb.Length - 4, 4);
				sb.Append(")");
			}
			else
				return "";
			return sb.ToString();
		}
		private IEnumerable<IAuditEntry> getResults(TopDocs ids, int page, IndexSearcher searcher)
		{
			int skip = page*20;
			return ids.ScoreDocs.Reverse().Skip(skip).Take(20).Select(x => new BasicAuditEntry(searcher.Doc(x.Doc), x.Doc));
		} 
		private object GetAutocomplete(dynamic data)
		{
			return AuditLogger.Current.AutoComplete(data.text, data.start, data.end, data.eventTypes);
		}

		public void RegisterCustomEventType(XmlNode node)
		{
			var attr = node.Attributes;
			if (attr != null)
			{
				CustomEventType o = new CustomEventType(attr["id"].Value, attr["color"].Value, attr["label"].Value);
				AuditLogger.Current.RegisterEventType(o);
			}
		}

		public void AddEventProcessor(XmlNode node)
		{
			var attr = node.Attributes;
			if (attr != null)
			{
				string[] parts = attr["type"].Value.Split(',');
				var assembly = Assembly.Load(parts[1].Trim());
				var type = assembly.GetType(parts[0]);
				AuditEventType o = Activator.CreateInstance(type) as AuditEventType;
				if (o != null)
				{
					o.Color = attr["color"].Value;
					o.Id = attr["id"].Value;
					o.Label = attr["label"].Value;
					AuditLogger.Current.RegisterEventType(o);
					EventHandler e = (sender, args) =>
					{
						o.Process(sender, args);
					};
					Event.Subscribe(attr["event"].Value, e);
				}
			}
		}
		private object GetTypes()
		{
			return AuditLogger.Current.GetAllEventTypes();
		}

		private object GetContentTree(dynamic data)
		{
			return string.IsNullOrWhiteSpace(data.id.ToString()) ? Root : new ContentTreeNode(Factory.GetDatabase(data.database.ToString()).GetItem(new ID(data.id)));
		}

		private object GetUserActivity(dynamic data)
		{
			AuditGraph ret = new AuditGraph();
			int max = 0;
			DateTime start = DateTime.Parse(data.start);
			DateTime end = DateTime.Parse(data.end);
			TimeSpan range = end.Subtract(start);
			var filter = BuildArrayQuery(data.filters, data.field);
			List<string> dates = new List<string>();
			for(int i = 0; i <= range.Days; i++)
				dates.Add(start.AddDays(i).ToString("yyyyMMdd"));
			var searcher = AuditLogger.Current.GetSearcher();
			Dictionary<string, AuditGraphEntry> entries = new Dictionary<string, AuditGraphEntry>();
			foreach (string eventType in data.eventTypes)
			{
				AuditGraphEntry gentry = new AuditGraphEntry() { Color = AuditLogger.Current.GetEventType(eventType).Color};
				foreach (string date in dates)
				{
					var results = AuditLogger.Current.Query($"date:{date} AND event:{eventType} {(filter.Length > 0 ? "AND ": "") + filter}", searcher);
					if (results.TotalHits > max)
						max = results.TotalHits;
					gentry.Coordinates.Add(new AuditGraphCoordinates() {X = date.Insert(6, "-").Insert(4, "-"), Y = results.TotalHits.ToString()});
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
