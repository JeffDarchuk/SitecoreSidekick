using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsContentMigrator.Models
{
	public class PullItemModel
	{
		public List<string> Ids;
		public string Database;
		public string Server;
		public bool Children;
		public bool Overwrite;
		public bool PullParent;
		public bool RemoveLocalNotInRemote;
		public bool Preview;
		public bool EventDisabler;
		public bool BulkUpdate;
	}
}
