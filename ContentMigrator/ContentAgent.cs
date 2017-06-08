using System.Collections.Generic;
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
				Server = remoteServer,
				Ids = new List<string>(rootIds.Split(',')),
				Database = database,
				bulkUpdate = bulkUpdate.ToLower() == "true",
				Children = children.ToLower() == "true",
				overwrite = overwrite.ToLower() == "true",
				eventDisabler = eventDisabler.ToLower() == "true",
				pullParent = pullParent.ToLower() == "true",
				mirror = mirror.ToLower() == "true",
				preview = false
			};
		}

		public void Run()
		{
			ContentMigrationRegistration.StartContentSync(_args);
		}
	}
}
