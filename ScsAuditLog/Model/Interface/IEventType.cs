using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsAuditLog.Model.Interface
{
	public interface IEventType
	{
		string Id { get; set; }
		string Color { get; set; }
		string Label { get; set; }
	}
}
