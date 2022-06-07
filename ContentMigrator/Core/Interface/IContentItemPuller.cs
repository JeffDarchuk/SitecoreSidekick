using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Rainbow.Model;

namespace Sidekick.ContentMigrator.Core.Interface
{
	public interface IContentItemPuller
	{
		bool Completed { get; }
		void StartGatheringItems(IEnumerable<Guid> rootIds, string database, int threads, bool getChildren, string server, CancellationToken cancellationToken, bool ignoreRevId);
		BlockingCollection<IItemData> ItemsToInstall { get; }
	}
}
