using System.Collections.Generic;
using ScsContentMigrator.Args;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator
{
	public class ContentAgent
	{
		private readonly PullItemModel _args;
		private readonly IContentMigrationManagerService _migrationManager;
		
		public ContentAgent(string remoteServer, string rootIds, string database, string bulkUpdate, string children, string overwrite, string eventDisabler, string pullParent, string removeLocalNotInRemote) : this(remoteServer, rootIds, database, bulkUpdate, children, overwrite, eventDisabler, pullParent, removeLocalNotInRemote, Bootstrap.Container.Resolve<IContentMigrationManagerService>())
		{
		}
		public ContentAgent(string remoteServer, string rootIds, string database, string bulkUpdate, string children, string overwrite, string eventDisabler, string pullParent, string removeLocalNotInRemote, IContentMigrationManagerService migrationManager)
		{
			_args = new PullItemModel()
			{
				Server = remoteServer,
				Ids = new List<string>(rootIds.Split(',')),
				Database = database,
				BulkUpdate = bulkUpdate.ToLower() == "true",
				Children = children.ToLower() == "true",
				Overwrite = overwrite.ToLower() == "true",
				EventDisabler = eventDisabler.ToLower() == "true",
				PullParent = pullParent.ToLower() == "true",
				RemoveLocalNotInRemote = removeLocalNotInRemote.ToLower() == "true",
				Preview = false
			};
			_migrationManager = migrationManager;
		}

		public void Run()
		{
			_migrationManager.StartContentMigration(_args);
		}
	}
}
