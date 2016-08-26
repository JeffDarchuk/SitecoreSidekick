using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsAuditLog.Model.Interface;

namespace ScsAuditLog.Model
{
	public class CustomEventType : IEventType
	{
		public CustomEventType(string id, string color, string label)
		{
			Id = id;
			Color = color;
			Label = label;
		}
		public string Id { get; set; }
		public string Color { get; set; }
		public string Label { get; set; }
	}
}
