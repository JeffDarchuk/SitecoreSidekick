using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Data.Items;

namespace SitecoreSidekick.Services.Interface
{
	public interface ISitecoreDataAccessService
	{
		Item GetItem(string id);
	}
}
