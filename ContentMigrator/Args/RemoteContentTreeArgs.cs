using Microsoft.CSharp.RuntimeBinder;
using ScsContentMigrator.Models;

namespace ScsContentMigrator.Args
{
	public class RemoteContentTreeArgs : ContentTreeModel
	{
		public RemoteContentTreeArgs()
		{
		}
		public RemoteContentTreeArgs(string id, string database, string server, bool children = false)
		{
			Id = id;
			Database = database;
			Server = server;
			Children = children;

        }
	}
}
