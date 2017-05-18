using System;
using ScsAuditLog.Model;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Data.Events;

namespace ScsAuditLog.Pipelines
{
	public class OnCreated : AuditEventType
	{
		public override void Process(object sender, EventArgs e)
		{
			try
			{
				ItemCreatedEventArgs createdEventArgs = Event.ExtractParameter(e, 0) as ItemCreatedEventArgs;
				if (createdEventArgs == null)
					return;
				LogEvent(createdEventArgs.Item, "");
			}
			catch (Exception ex)
			{
				Log.Error("Problem auditing item created event", ex, this);
			}
		}
	}
}
