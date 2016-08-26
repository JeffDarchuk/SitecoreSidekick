using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Core;
using ScsAuditLog.Model;
using Sitecore.Pipelines.GetAboutInformation;

namespace ScsAuditLog
{
	public class AuditLogger
	{
		internal static AuditLog Log;
		public static AuditLog Current => Log;
	}
}
