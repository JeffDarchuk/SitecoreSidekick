using ScsAuditLog.Model.Interface;

namespace ScsAuditLog.Model
{
	public class AuditSourceRecord
	{
		public AuditSourceRecord(ItemAuditEntry entry, string content)
		{
			Entry = entry;
			Content = content;
		}
		public ItemAuditEntry Entry;
		public string Content;
	}
}
