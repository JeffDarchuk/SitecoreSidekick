﻿using System;
using System.Collections.Generic;
using Sitecore.Data;

namespace Sidekick.AuditLog.Model.Interface
{
	public interface IAuditEntry
	{
		string Uid { get; set; }
		string User { get; set; }
		List<string> Role { get; set; }
		string Id { get; set; }
		string Database { get; set; }
		string Path { get; set; }
		DateTime TimeStamp { get; set; }
		string EventId { get; set; }
		string Note { get; set; }
		string Label { get; set; }
		string Color { get; set; }
		string Icon { get; set; }
	}
}
