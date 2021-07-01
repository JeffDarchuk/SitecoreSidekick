using System.Collections.Generic;
using Sidekick.AuditLog.Model;
using Sidekick.AuditLog.Model.Interface;
using Sitecore.Data.Items;

namespace Sidekick.AuditLog.Core
{
	public interface IAuditLog
	{
		IEnumerable<KeyValuePair<string, int>> AutoComplete(string text, string start, string end, List<object> eventTypes);
		object GetActivityData(ActivityDataModel data);
		object GetUserActivity(ActivityDataModel data);
		IDictionary<string, IEventType> GetAllEventTypes();
		HashSet<string> GetUsers();
		void Log(IAuditEntry entry, string content = "", bool newRecord = true);
		void Log(Item item, IEventType type, string note = "");
		void Log(Item item, string typeId, string note = "");
		void Log(Item item, string eventId, string color, string label, string note = "");
		void Rebuild();
		int RebuildLogStatus();
		void RegisterEventType(IEventType eventType);
	}
}