namespace ScsContentMigrator.Args
{
	public class RemoteContentPullArgs : RemoteContentTreeArgs
	{
		public bool overwrite;
		public bool pullParent;
		public bool mirror;

		public RemoteContentPullArgs()
		{
		}

		public RemoteContentPullArgs(dynamic data)
		{
			id = data.id;
			database = data.database;
			server = data.server;
			children = data.children;
			overwrite = data.overwrite;
			pullParent = data.pullParent;
			mirror = data.mirror;

		}
		public override string GetSerializedData()
		{
			return GetSerializedData(id, children);
		}

		public string GetSerializedData(string altId, bool altChildren)
		{
			return
				$"{{\"id\":\"{altId}\",\"database\":\"{database}\",\"children\":{JsonBool(altChildren)},\"overwrite\":{JsonBool(overwrite)},\"pullParent\":{JsonBool(pullParent)}}}";
		}
		private string JsonBool(bool b)
		{
			return b ? "true" : "false";
		}
	}
}