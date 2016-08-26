using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Model;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Data.Events;

namespace ScsAuditLog.Pipelines
{
	public class OnMoved : AuditEventType
	{
		public override void Process(object sender, EventArgs e)
		{
			try
			{
				Item parameter1 = Event.ExtractParameter<Item>(e, 0);
				ID parameter2 = Event.ExtractParameter<ID>(e, 1);
				if (!(parameter1.ParentID != parameter2))
					return;
				Item obj = parameter1.Database.GetItem(parameter2);
				StringBuilder stringBuilder = new StringBuilder("<table><th>Field</th><th>Old</th><th>New</th>");
				stringBuilder.Append("<tr><td>");
				stringBuilder.Append("Location");
				stringBuilder.Append("</td><td>");
				stringBuilder.Append(Cleanse(obj.Paths.FullPath));
				stringBuilder.Append("</td><td>");
				stringBuilder.Append(Cleanse(parameter1.Parent.Paths.FullPath));
				stringBuilder.Append("</td></tr>");
				stringBuilder.Append("</table>");
				LogEvent(parameter1, stringBuilder.ToString());
			}
			catch (Exception ex)
			{
				Log.Error("Problem auditing item moved", ex, this);
			}
		}
	}
}
