using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.ContentMigrator.Models
{
	public class RevisionModel
	{
		public string Id;
		public string Database;
		public Dictionary<Guid, string> Rev;
	}
}
