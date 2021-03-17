using System;
using System.Collections.Generic;
using Sidekick.ContentMigrator.Args;
using Sidekick.ContentMigrator.Models;

namespace Sidekick.ContentMigrator.Core.Interface
{
	public interface IContentMigration
	{
		ContentMigrationOperationStatus Status { get; }
		int ItemsInQueueToInstall { get;}
		void CancelMigration();
		void StartContentMigration(PullItemModel args);
		void StartOperationFromPreview();
		IEnumerable<dynamic> GetItemLogEntries(int lineToStartFrom);
		IEnumerable<string> GetAuditLogEntries(int lineToStartFrom);
	}
}
