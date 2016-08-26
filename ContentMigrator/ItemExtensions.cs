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

		public static IEnumerable<string> GetYamlTree(this Item item)
		{
			Stack<Item> i = new Stack<Item>();
			i.Push(item);
			while (i.Any())
			{
				Item currentItem = i.Pop();
				yield return currentItem.GetYaml();
				foreach (Item child in currentItem.Children)
					i.Push(child);
			}
		}

	}
}
