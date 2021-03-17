using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Sidekick.AuditLog.Model.Interface;
using Sitecore.Data;

namespace Sidekick.AuditLog.Model
{
	public class BasicAuditEntry : IAuditEntry
	{
		public string Uid { get; set; }
		public string User { get; set; }
		public List<string> Role { get; set; }
		public string Id { get; set; }
		public string Database { get; set; }
		public string Path { get; set; }
		public DateTime TimeStamp { get; set; }
		public string EventId { get; set; }
		public string Note { get; set; }
		public string Label { get; set; }
		public string Color { get; set; }
		public string Icon { get; set; }

		public BasicAuditEntry(Document doc, int luceneId)
		{
			User = doc.Get("user");
			Role = doc.GetValues("role").ToList();
			Id = new ID(doc.Get("id")).ToString();
			Path = doc.Get("path");
			DateTime tmp;
			DateTime.TryParse(doc.Get("timestamp"), out tmp);
			TimeStamp = tmp;
			EventId = doc.Get("event");
			Note = doc.Get("note");
			Label = doc.Get("label");
			Color = doc.Get("color");
			Database = doc.Get("database");
			Uid = luceneId.ToString();
			Icon = doc.Get("icon");
		}
	}
}