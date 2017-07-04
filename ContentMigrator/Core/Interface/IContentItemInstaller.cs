using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rainbow.Model;
using ScsContentMigrator.Args;
using ScsContentMigrator.Models;

namespace ScsContentMigrator.Core.Interface
{
	public interface IContentItemInstaller
	{
		void StartInstallingItems(PullItemModel args, BlockingCollection<IItemData> itemsToInstall, int threads, CancellationTokenSource cancellation);
		void CleanUnwantedLocalItems();
		void SetupTrackerForUnwantedLocalItems(IEnumerable<Guid> rootIds);
		bool Completed { get; }
		IEnumerable<dynamic> GetItemLogEntries(int lineToStartFrom);
		IEnumerable<string> GetAuditLogEntries(int lineToStartFrom);
		ContentMigrationOperationStatus Status { get; }
	}
}
