using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Args;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Query;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Rainbow;

namespace ScsContentMigrator
{
	public class RemoteContentPuller
	{
		DefaultDeserializer deserializer = new DefaultDeserializer(new DefaultLogger(), new DefaultFieldFilter());
		private static ConcurrentDictionary<string, OperationStatus> _operation = new ConcurrentDictionary<string, OperationStatus>();

		public dynamic PullContentItem(RemoteContentPullArgs args)
		{
			OperationStatus status = RegisterEvent(args);
			return status.OperationId;
		}

		public static bool StopOperation(string operationId)
		{
			if (_operation.ContainsKey(operationId))
			{
				_operation[operationId].CancelOperation();
				return true;
			}
			return false;
		}
		internal static OperationStatus RegisterEvent(RemoteContentPullArgs args)
		{
			var ret = Guid.NewGuid().ToString();
			_operation[ret] = new OperationStatus(args, ret);
			return _operation[ret];
		}

		public static IEnumerable<dynamic> GetRunningOperations()
		{
			return _operation.Values.Select(x => new {x.Completed, x.RootNode, x.OperationId});
		}

		public static IEnumerable<dynamic> OperationStatus(string operationId, int lineNumber)
		{
			if (_operation == null || string.IsNullOrWhiteSpace(operationId))
				yield break;
			if (_operation.ContainsKey(operationId))
				for (int i = lineNumber; i < _operation[operationId].Lines.Count; i++)
					yield return _operation[operationId].Lines[i];
		}
	}
}
