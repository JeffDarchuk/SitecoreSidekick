using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScsContentMigrator.Args;

namespace ScsContentMigrator
{
	public class RemoteContentPuller
	{
		private static readonly ConcurrentDictionary<string, OperationStatus> Operation = new ConcurrentDictionary<string, OperationStatus>();

		public string PullContentItem(RemoteContentPullArgs args)
		{
			var id = Guid.NewGuid().ToString();
			Task.Run(() =>
			{
				RegisterEvent(args, id);
			});

			return id;
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
		internal static OperationStatus RegisterEvent(RemoteContentPullArgs args, string newOperationId)
		{
			Operation[newOperationId] = new OperationStatus(args, newOperationId);
			return Operation[newOperationId];
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
