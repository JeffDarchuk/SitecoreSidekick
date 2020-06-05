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
		public bool Children = true;
		public bool Overwrite = true;
		public bool PullParent = true;
		public bool RemoveLocalNotInRemote = false;
		public bool Preview = false;
		public bool EventDisabler = true;
		public bool BulkUpdate = true;
		public bool UseItemBlaster = true;
		public bool IgnoreRevId = false;
	}
}
