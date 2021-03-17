using System;
using Sidekick.AuditLog.Model;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace Sidekick.AuditLog.Pipelines
{
	public class GeneralEvent : AuditEventType
	{
		public override void Process(object sender, EventArgs e)
		{
			try
			{
				Item item = Event.ExtractParameter(e, 0) as Item;
				if (item == null)
					return;
				LogEvent(item, "");
			}
			catch (Exception ex)
			{
				Log.Error("Problem auditing general event "+Label, ex, this);
			}
		}
	}
}
