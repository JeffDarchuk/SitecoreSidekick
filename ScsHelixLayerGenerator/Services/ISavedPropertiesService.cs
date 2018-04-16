using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Services
{
	interface ISavedPropertiesService
	{
		string this[string key, string template]
		{
			get;
			set;
		}
	}
}
