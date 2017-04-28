using ScsContentMigrator.Args;

namespace ScsContentMigrator
{
	public class ContentAgent
	{
		private readonly RemoteContentPullArgs _args;

		public ContentAgent(string remoteServer, string rootIds, string database, string bulkUpdate, string children, string overwrite, string eventDisabler, string pullParent, string mirror)
		{
			_args = new RemoteContentPullArgs()
			{
				server = remoteServer,
				ids = rootIds.Split(','),
				database = database,
				bulkUpdate = bulkUpdate.ToLower() == "true",
				children = children.ToLower() == "true",
				overwrite = overwrite.ToLower() == "true",
				eventDisabler = eventDisabler.ToLower() == "true",
				pullParent = pullParent.ToLower() == "true",
				mirror = mirror.ToLower() == "true",
				preview = false
			};
		}

		public void Run()
		{
			ContentMigrationHandler.StartContentSync(_args);
		}
	}
}
