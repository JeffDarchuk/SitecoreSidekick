using Microsoft.CSharp.RuntimeBinder;

namespace ScsContentMigrator.Args
{
	public class RemoteContentTreeArgs
	{
		public string id;
		public string database;
		public string server;
		public bool children;

		public RemoteContentTreeArgs()
		{
		}
		public RemoteContentTreeArgs(dynamic data)
		{
			id = data.id;
			database = data.database;
			server = data.server;
			try
			{
				children = data.children;
			}
			catch (RuntimeBinderException)
			{
				//no problems, this field is optional
			}
        }
	}
}
