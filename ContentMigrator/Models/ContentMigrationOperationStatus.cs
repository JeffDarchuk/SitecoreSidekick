using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sidekick.Core.ContentTree;

namespace Sidekick.ContentMigrator.Models
{
	public class ContentMigrationOperationStatus
	{
		public bool Completed { get; set; }
		public IEnumerable<ContentTreeNode> RootNodes { get; set; }
		public string OperationId { get; set; }
		public bool IsPreview { get; set; }
		public bool Cancelled { get; set; }
		public string Server { get; set; }
		public DateTime StartedTime { get; set; }
		public DateTime FinishedTime { get; set; }
	}
}
