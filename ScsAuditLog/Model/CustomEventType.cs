using Sidekick.AuditLog.Model.Interface;

namespace Sidekick.AuditLog.Model
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
