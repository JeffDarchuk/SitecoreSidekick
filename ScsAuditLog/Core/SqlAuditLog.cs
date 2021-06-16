using System;
using System.Collections.Generic;
using Sidekick.AuditLog.Model;
using Sidekick.AuditLog.Model.Interface;
using Sitecore.Data.Items;

namespace Sidekick.AuditLog.Core
{
	public class SqlAuditLog : IAuditLog
	{
		public IEnumerable<KeyValuePair<string, int>> AutoComplete(string text, string start, string end, List<object> eventTypes)
		{
			throw new NotImplementedException();
		}

		public object GetActivityData(ActivityDataModel data)
		{
			throw new NotImplementedException();
		}

		public IDictionary<string, IEventType> GetAllEventTypes()
		{
			throw new NotImplementedException();
		}

		public object GetUserActivity(ActivityDataModel data)
		{
			throw new NotImplementedException();
		}

		public HashSet<string> GetUsers()
		{
			throw new NotImplementedException();
		}

		public void Log(IAuditEntry entry, string content = "", bool newRecord = true)
		{
			throw new NotImplementedException();
		}

		public void Log(Item item, IEventType type, string note = "")
		{
			throw new NotImplementedException();
		}

		public void Log(Item item, string typeId, string note = "")
		{
			throw new NotImplementedException();
		}

		public void Log(Item item, string eventId, string color, string label, string note = "")
		{
			throw new NotImplementedException();
		}

		public void Rebuild()
		{
			throw new NotImplementedException();
		}

		public int RebuildLogStatus()
		{
			throw new NotImplementedException();
		}

		public void RegisterEventType(IEventType eventType)
		{
			throw new NotImplementedException();
		}
	}
}
