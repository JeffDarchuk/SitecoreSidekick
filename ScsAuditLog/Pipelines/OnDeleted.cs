using System;
using Sidekick.AuditLog.Model;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace Sidekick.AuditLog.Pipelines
{
	public class OnDeleted : AuditEventType
	{
		public override void Process(object sender, EventArgs e)
		{
			try
			{
				LogEvent(Event.ExtractParameter<Item>(e, 0), "");
			}
			catch (Exception ex)
			{
				Log.Error("problem auditing item deleted", ex, this);
			}
		}
	}
}
