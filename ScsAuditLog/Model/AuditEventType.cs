using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScsAuditLog.Model.Interface;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.HtmlControls;

namespace ScsAuditLog.Model
{
	public abstract class AuditEventType : IEventType
	{
		public string Id { get; set; }
		public string Color { get; set; }
		public string Label { get; set; }
		public abstract void Process(object sender, EventArgs e);

		public void LogEvent(Item item, string note)
		{
			AuditLogger.Current.Log(item, Id, Color, Label, note);
		}
		public void LogEvent(Item item, string id, string note)
		{
			
		}
		public void LogEvent(ItemAuditEntry entry)
		{
			AuditLogger.Current.Log(entry);
		}
		public static string Cleanse(string input)
		{
			return Regex.Replace(input, "\\r\\n?|\\n", "").Replace("|", "&#124;");
		}
	}
}
