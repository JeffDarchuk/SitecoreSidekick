using System.Collections.Generic;
using System.Linq;

namespace ScsContentMigrator.Args
{
	public class RemoteContentPullArgs : RemoteContentTreeArgs
	{
		public IEnumerable<string> ids;
		public bool overwrite;
		public bool pullParent;
		public bool mirror;
		public bool preview;

		public bool eventDisabler;
		public bool bulkUpdate;

		public RemoteContentPullArgs()
		{
		}

		public RemoteContentPullArgs(dynamic data)
		{
			ids = ((List<object>)data.ids).Select(x => x.ToString());
			database = data.database;
			server = data.server;
			children = data.children;
			overwrite = data.overwrite;
			pullParent = data.pullParent;
			mirror = data.mirror;
			preview = data.preview;
			eventDisabler = data.eventDisabler;
			bulkUpdate = data.bulkUpdate;

		}

		public string GetSerializedData(string altId, bool altChildren)
		{
			return
				$"{{\"id\":\"{altId}\",\"database\":\"{database}\",\"children\":{JsonBool(altChildren)},\"overwrite\":{JsonBool(overwrite)},\"pullParent\":{JsonBool(pullParent)},\"preview\":{JsonBool(preview)},\"bulkUpdate\":{JsonBool(bulkUpdate)},\"eventDisabler\":{JsonBool(eventDisabler)}}}";
		}
		private string JsonBool(bool b)
		{
			return b ? "true" : "false";
		}
	}
}