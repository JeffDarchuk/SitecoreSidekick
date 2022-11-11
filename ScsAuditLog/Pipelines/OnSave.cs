using System;
using System.Text;
using Sidekick.AuditLog.Model;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using System.Web;

namespace Sidekick.AuditLog.Pipelines
{
	public class OnSave : AuditEventType
	{
		public override void Process(object sender, EventArgs e)
		{
			try
			{
				Item item = Event.ExtractParameter<Item>(e, 0);
				ItemChanges changes = Event.ExtractParameter<ItemChanges>(e, 1);
				StringBuilder sb = new StringBuilder("<table><th>Field</th><th>Change</th>");
				bool flag = false;
				foreach (FieldChange fieldChange in changes.FieldChanges)
				{
					Field field = item.Fields[fieldChange.FieldID];
					if (!field.Name.StartsWith("__") && fieldChange.OriginalValue != field.Value)
					{
						var originalValue = fieldChange.OriginalValue ?? string.Empty;
						var newValue = field.Value ?? string.Empty;
						flag = true;
						sb.Append("<tr><td>");
						sb.Append(Cleanse(field.Name));
						sb.Append("</td><td>");
						HtmlDiff.HtmlDiff diff = new HtmlDiff.HtmlDiff(HttpUtility.HtmlEncode(originalValue), HttpUtility.HtmlEncode(newValue));
						sb.Append(diff.Build());
						sb.Append("</td></tr>");
					}
				}
				if (!flag)
					return;
				sb.Append("</table>");
				LogEvent(item, sb.ToString());
			}
			catch (Exception ex)
			{
				Log.Error("Problem auditing item save", ex, this);
			}
		}
	}
}
