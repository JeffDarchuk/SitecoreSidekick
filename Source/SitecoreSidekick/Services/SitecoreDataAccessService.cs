using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using SitecoreSidekick.Models;
using SitecoreSidekick.Services.Interface;

namespace SitecoreSidekick.Services
{
	public class SitecoreDataAccessService : ISitecoreDataAccessService
	{
		private readonly Database _db = Factory.GetDatabase("master", false);

		public ScsSitecoreItem GetItem(string id)
		{
			Item item = _db.GetItem(id);

			return new ScsSitecoreItem(item);
		}
	}
}
