using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Sidekick.AuditLog.Model;
using Sidekick.Core.Handlers;
using System.Dynamic;
using System.Reflection;
using System.Web.Mvc;
using System.Xml;
using Lucene.Net.Search;
using Sidekick.AuditLog.Core;
using Sidekick.AuditLog.Model.Interface;
using Sidekick.Core.ContentTree;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sidekick.Core;

namespace Sidekick.AuditLog
{
	public class AuditLogRegistration : ScsRegistration
	{
		public override string Directive => "aldirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "Sidekick.AuditLog.Resources";
		public override Type Controller => typeof(AuditLogController);
		public override string Icon { get; } = "/scs/al/resources/alportfoliofolder.png";
		public override string Name { get; } = "Audit Log";
		public override string Identifier => "al";
		public override string CssStyle { get; } = "width:100%;min-width:900px";

		public AuditLogRegistration(string keepBackups, string keepRecords, string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
			int backup;
			int duration;
			if (!int.TryParse(keepBackups, out backup))
				backup = 0;
			if (!int.TryParse(keepRecords, out duration))
				duration = 0;
			AuditLogger.Log = new Core.LuceneAuditLog(backup, duration);
		}
		public void RegisterCustomEventType(XmlNode node)
		{
			var attr = node.Attributes;
			if (attr != null)
			{
				CustomEventType o = new CustomEventType(attr["id"].Value, attr["color"].Value, attr["label"].Value);
				AuditLogger.Current.RegisterEventType(o);
			}
		}

		public void AddEventProcessor(XmlNode node)
		{
			var attr = node.Attributes;
			if (attr != null)
			{
				string[] parts = attr["type"].Value.Split(',');
				var assembly = Assembly.Load(parts[1].Trim());
				var type = assembly.GetType(parts[0]);
				AuditEventType o = Activator.CreateInstance(type) as AuditEventType;
				if (o != null)
				{
					o.Color = attr["color"].Value;
					o.Id = attr["id"].Value;
					o.Label = attr["label"].Value;
					AuditLogger.Current.RegisterEventType(o);
					EventHandler e = (sender, args) =>
					{
						o.Process(sender, args);
					};
					Event.Subscribe(attr["event"].Value, e);
				}
			}
		}
	}
}
