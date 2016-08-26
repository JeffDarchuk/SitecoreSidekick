using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Model;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace ScsAuditLog.Pipelines
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
				Log.Error("Problem auditing general event "+this.Label, ex, this);
			}
		}
	}
}
