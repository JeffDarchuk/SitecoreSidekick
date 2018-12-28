using System;
using System.Collections.Generic;
using Rainbow.Model;
using ScsContentMigrator.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator.Models
{
	public class ChildrenItemDataModel
	{
		private readonly IRemoteContentService _remote;
		public ChildrenItemDataModel()
		{
			_remote = Bootstrap.Container.Resolve<IRemoteContentService>();
		}

		public List<Guid> GrandChildren { get; set; }
		public List<KeyValuePair<Guid, string>> Items { get; set; } = new List<KeyValuePair<Guid, string>>();
	}
}
