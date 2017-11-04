using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data.Items;

namespace ScsContentMigrator.CMRainbow.Interface
{
	public interface IDefaultLogger : IDefaultDeserializerLogger
	{
		List<dynamic> Lines { get; set; }
		ConcurrentDictionary<string, dynamic> LinesSupport { get; }
		bool HasLinesSupportEvents(string key);
		List<string> LoggerOutput { get; }
		void BeginEvent(Item data, string status, string icon, bool keepOpen);
		void BeginEvent(IItemData data, string status, string icon, bool keepOpen);
		void BeginEvent(string name, string id, string path, string status, string icon, string database, bool keepOpen);
		void CompleteEvent(string id);
		string GetSrc(string imgTag);
	}
}
