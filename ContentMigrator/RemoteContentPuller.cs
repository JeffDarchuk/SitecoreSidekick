using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ScsContentMigrator.Args;

namespace ScsContentMigrator
{
	public class RemoteContentPuller
	{
		private static readonly ConcurrentDictionary<string, OperationStatus> Operation = new ConcurrentDictionary<string, OperationStatus>();

		public dynamic PullContentItem(RemoteContentPullArgs args)
		{
			OperationStatus status = RegisterEvent(args);
			return status.OperationId;
		}

		public static bool StopOperation(string operationId)
		{
			if (Operation.ContainsKey(operationId))
			{
				Operation[operationId].CancelOperation();
				return true;
			}
			return false;
		}
		internal static OperationStatus RegisterEvent(RemoteContentPullArgs args)
		{
			var ret = Guid.NewGuid().ToString();
			Operation[ret] = new OperationStatus(args, ret);
			return Operation[ret];
		}

		public static IEnumerable<dynamic> GetRunningOperations()
		{
			return Operation.Values.Select(x => new {x.Completed, x.RootNodes, x.OperationId, x.IsPreview, x.Cancelled, x.StartedTime, x.FinishedTime});
		}

		public static OperationStatus GetOperation(string id)
		{
			if (Operation.ContainsKey(id))
				return Operation[id];
			return null;
		}

		public static IEnumerable<dynamic> OperationStatus(string operationId, int lineNumber)
		{
			if (Operation == null || string.IsNullOrWhiteSpace(operationId))
				yield break;
			if (Operation.ContainsKey(operationId))
				for (int i = lineNumber; i < Operation[operationId].Lines.Count; i++)
					yield return Operation[operationId].Lines[i];
		}
	}
}
