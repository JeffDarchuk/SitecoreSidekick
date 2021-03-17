using Sitecore.Publishing.Pipelines.PublishItem;

namespace Sidekick.AuditLog.Pipelines.Publish
{
	public class AuditPublishItem : PublishItemProcessor
	{
		public override void Process(PublishItemContext context)
		{
			AuditLogger.Current.Log(context.VersionToPublish, "6",
				$"<table><th>Field</th><th>Source</th><th>Target</th><tr><td>Location</td><td>{context.PublishOptions.SourceDatabase.Name}</td><td>{context.PublishOptions.TargetDatabase.Name}</td></tr></table>");
		}
	}

}
