using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsContentMigrator.Models
{
	public class ContentTreeModel
	{
		public List<string> Ids { get; set; } = new List<string>();
		public string Id { get; set; }
		public string Database { get; set; }
		public string Server { get; set; }
		public bool Children { get; set; } = false;
		public string Payload { get; set; }
	}
}
