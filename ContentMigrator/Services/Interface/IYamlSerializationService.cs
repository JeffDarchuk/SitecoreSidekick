using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Sitecore.Data.Items;

namespace Sidekick.ContentMigrator.Services.Interface
{
	public interface IYamlSerializationService
	{
		IItemData DeserializeYaml(string yaml, string itemId);
		string SerializeYaml(IItemData item);
		void WriteSerializedItem(IItemData item, Stream stream);
	}
}
