using System;
using System.Linq;
using System.Web.Mvc;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;

namespace Sidekick.JobManager
{
	class JobManagerController : ScsController
	{
		private readonly IJsonSerializationService _jsonSerializationService;

		public JobManagerController()
		{
			_jsonSerializationService = Sidekick.Core.Bootstrap.Container.Resolve<IJsonSerializationService>();
		}
		[ActionName("jmGetJobs.scsvc")]
		public ActionResult GetJobs()
		{
			return ScsJson(Sitecore.Jobs.JobManager.GetJobs().Select(
				x => new
				{
					name = x.Name,
					category = x.Category,
					displayName = x.DisplayName,
					queueTime = x.QueueTime,
					isDone = x.IsDone,
					user = x.Options.ContextUser.Name,
					status = new
					{
						exceptions = x.Status.Exceptions,
						result = x.Status.Result,
						messages = x.Status.Messages,
						processed = x.Status.Processed,
						total = x.Status.Total,
						state = x.Status.State
					}

				}));
		}
		[ActionName("jmCancelJob.scsvc")]
		public ActionResult CancelJob(string name)
		{
			Sitecore.Jobs.Job job = Sitecore.Jobs.JobManager.GetJob(name);
			if (job != null)
			{
				job.Status.State = Sitecore.Jobs.JobState.Finished;
				job.Status.Expiry = DateTime.Now.AddMinutes(-1.0);
			}

			return Content("done");
		}
	}
}
