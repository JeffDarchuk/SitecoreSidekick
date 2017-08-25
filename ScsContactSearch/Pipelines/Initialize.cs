using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ScsContactSearch.Services;
using Sitecore.Pipelines;
using SitecoreSidekick.Shared.IoC;

namespace ScsContactSearch.Pipelines
{
	public class Initialize
	{
		private IContactAccessService _contact;
		public Initialize()
		{			
			_contact = Bootstrap.Container.Resolve<IContactAccessService>();
		}
		public void Process(PipelineArgs args)
		{
			_contact.EnsureIndexExists();
		}
	}
}
