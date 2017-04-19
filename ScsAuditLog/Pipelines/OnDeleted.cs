using System;
using ScsAuditLog.Model;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace ScsAuditLog.Pipelines
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
