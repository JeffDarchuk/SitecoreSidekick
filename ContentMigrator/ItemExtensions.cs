using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Yaml;
using Sitecore.Data.Items;

namespace ScsContentMigrator
{
	public static class ItemExtensions
	{
		public static YamlSerializationFormatter Formatter = new YamlSerializationFormatter(null, null);
		public static string GetYaml(this Item item)
		{
			using (var stream = new MemoryStream())
			{
				ItemData data = new ItemData(item);
				Formatter.WriteSerializedItem(data, stream);
				stream.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}
