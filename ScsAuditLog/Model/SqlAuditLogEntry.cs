using System;

namespace Sidekick.AuditLog.Model
{
	public class SqlAuditLogEntry
	{
		public Guid Id { get; set; }
		public Guid? UserId { get; set; }
		public string Role { get; set; }
		public Guid? ItemId { get; set; }
		public string Database { get; set; }
		public string Path { get; set; }
		public DateTime TimeStamp { get; set; }
		public string EventId { get; set; }
		public string Note { get; set; }
		public string Label { get; set; }
		public string Color { get; set; }
		public string Icon { get; set; }
	}
}
