using System.Collections.Generic;
using System.Linq;
using ScsContentMigrator.Models;

namespace ScsContentMigrator.Args
{
	public class RemoteContentPullArgs : RemoteContentTreeArgs
	{
		public bool overwrite;
		public bool pullParent;
		public bool mirror;
		public bool preview;

		public bool eventDisabler;
		public bool bulkUpdate;

		public RemoteContentPullArgs()
		{
		}

		public RemoteContentPullArgs(PullItemModel data)
		{
			Ids = data.Ids;
			Database = data.Database;
			Server = data.Server;
			Children = data.Children;
			this.overwrite = data.Overwrite;
			this.pullParent = data.PullParent;
			this.mirror = data.Mirror;
			this.preview = data.Preview;
			this.eventDisabler = data.EventDisabler;
			this.bulkUpdate = data.BulkUpdate;

		}

		public string GetSerializedData(string altId, bool altChildren)
		{
			return
				$"{{\"id\":\"{altId}\",\"database\":\"{Database}\",\"children\":{JsonBool(altChildren)},\"overwrite\":{JsonBool(overwrite)},\"pullParent\":{JsonBool(pullParent)},\"preview\":{JsonBool(preview)},\"bulkUpdate\":{JsonBool(bulkUpdate)},\"eventDisabler\":{JsonBool(eventDisabler)}}}";
		}
		private string JsonBool(bool b)
		{
			return b ? "true" : "false";
		}
	}
}