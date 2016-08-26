using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Model;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Data.Events;

namespace ScsAuditLog.Pipelines
{
	public class OnRename : AuditEventType
	{
		public override void Process(object sender, EventArgs e)
		{
			try
			{
				Item parameter1 = Event.ExtractParameter<Item>(e, 0);
				string parameter2 = Event.ExtractParameter<string>(e, 1);
				if (parameter1.Name == parameter2)
					return;
				StringBuilder stringBuilder = new StringBuilder("<table><th>Field</th><th>Old</th><th>New</th>");
				stringBuilder.Append("<tr><td>");
				stringBuilder.Append("Name");
				stringBuilder.Append("</td><td>");
				stringBuilder.Append(Cleanse(parameter2));
				stringBuilder.Append("</td><td>");
				stringBuilder.Append(Cleanse(parameter1.Name));
				stringBuilder.Append("</td></tr>");
				stringBuilder.Append("</table>");
				LogEvent(parameter1, stringBuilder.ToString());
			}
			catch (Exception ex)
			{
				Log.Error("problem auditing item renamed", ex, this);
			}
		}
	}
}
