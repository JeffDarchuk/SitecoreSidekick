using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sidekick.Core;
using Sidekick.Core.Services.Interface;

namespace Sidekick.JobManager
{
	class JobManagerRegistration : ScsRegistration
	{
		public JobManagerRegistration(string roles, string isAdmin, string users) : base(roles, isAdmin, users)
		{
		}

		public override string Identifier => "jm";
		public override string Directive => "jmmasterdirective";
		public override NameValueCollection DirectiveAttributes { get; set; }
		public override string ResourcesPath => "Sidekick.JobManager.Resources";
		public override Type Controller => typeof(JobManagerController);
		public override string Icon => "/scs/jm/resources/jmicon.png";
		public override string Name => "Job Manager";
		public override string CssStyle => "min-width:600px;";
	}
}
