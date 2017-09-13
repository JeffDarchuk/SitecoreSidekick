using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Sitecore.Data.Items;

namespace ScsContentMigrator.Services.Interface
{
	public interface IYamlSerializationService
	{
		IItemData DeserializeYaml(string yaml, string itemId);
		string SerializeYaml(Item item);
	}
}
