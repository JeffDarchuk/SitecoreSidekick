using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Yaml;
using Sidekick.ContentMigrator.Services.Interface;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Sidekick.ContentMigrator.Services
{
	public class YamlSerializationService : IYamlSerializationService
	{
		private readonly YamlSerializationFormatter _formatter = new YamlSerializationFormatter(null, null);
		public IItemData DeserializeYaml(string yaml, string itemId)
		{			
			if (yaml != null)
			{
				using (var ms = new MemoryStream())
				{
					IItemData itemData = null;
					try
					{
						var bytes = Encoding.UTF8.GetBytes(yaml);
						ms.Write(bytes, 0, bytes.Length);

						ms.Seek(0, SeekOrigin.Begin);
						itemData = _formatter.ReadSerializedItem(ms, itemId);
					}
					catch (Exception e)
					{
						Log.Error("Problem reading yaml from remote server", e, typeof(RemoteContentService));
					}
					if (itemData != null)
					{
						return itemData;
					}
				}
			}
			return null;
		}
		
		public string SerializeYaml(IItemData item)
		{
			using (var stream = new MemoryStream())
			{				
				_formatter.WriteSerializedItem(item, stream);
				stream.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public void WriteSerializedItem(IItemData item, Stream stream)
		{
			_formatter.WriteSerializedItem(item, stream);
		}
	}
}
