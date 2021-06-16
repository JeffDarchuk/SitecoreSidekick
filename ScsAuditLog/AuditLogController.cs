using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Rainbow.Model;
using Sidekick.AuditLog.Model;
using Sidekick.Core;
using Sidekick.Core.ContentTree;
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
			return ScsJson(AuditLogger.Current.GetUserActivity(data));
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
			return ScsJson(AuditLogger.Current.GetActivityData(data));
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
			return AuditLogger.Log.RebuildLogStatus();
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

		

	}
}
