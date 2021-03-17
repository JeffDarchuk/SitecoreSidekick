using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.ContentMigrator.Models
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
		public bool UseItemBlaster = false;
		public bool IgnoreRevId = false;
		public PullItemModel() { }
		public PullItemModel(ContentMigrationRegistration registration) {
			Children = registration.DefaultChildren;
			Overwrite = registration.DefaultOverwrite;
			PullParent = registration.DefaultPullParent;
			RemoveLocalNotInRemote = registration.DefaultRemoveLocalNotInRemote;
			EventDisabler = registration.DefaultEventDisabler;
			BulkUpdate = registration.DefaultBulkUpdate;
			UseItemBlaster = registration.DefaultUseItemBlaster;
			IgnoreRevId = registration.DefaultIgnoreRevId;
		}

	}
}
