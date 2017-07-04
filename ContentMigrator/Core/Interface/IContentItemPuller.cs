using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Rainbow.Model;

namespace ScsContentMigrator.Core.Interface
{
	public interface IContentItemPuller
	{
		bool Completed { get; }
		void StartGatheringItems(IEnumerable<Guid> rootIds, int threads, bool getChildren, string server, CancellationTokenSource cancellation);
		BlockingCollection<IItemData> ItemsToInstall { get; }
	}
}
