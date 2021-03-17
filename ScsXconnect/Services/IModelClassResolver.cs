using System;
using System.Collections.Generic;
using Sitecore.XConnect.Schema;

namespace Sidekick.XConnect.Services
{
	interface IModelClassResolver
	{
		IEnumerable<XdbModel> GetAllModels();
		XdbModel GetModelByName(string name);
		Type GetModelType(string name);
	}
}
