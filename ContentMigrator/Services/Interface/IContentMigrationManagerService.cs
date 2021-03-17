using System.Collections.Generic;
using Sidekick.ContentMigrator.Args;
using Sidekick.ContentMigrator.Core;
using Sidekick.ContentMigrator.Core.Interface;
using Sidekick.ContentMigrator.Models;

namespace Sidekick.ContentMigrator.Services.Interface
{
	public interface IContentMigrationManagerService
	{
		ContentMigration StartContentMigration(PullItemModel args);
		bool CancelContentMigration(string operationId);
		IContentMigration GetContentMigration(string operationId);
		IEnumerable<IContentMigration> GetAllContentMigrations();
		IEnumerable<dynamic> GetItemLogEntries(string operationId, int lineToStartFrom);
		IEnumerable<string> GetAuditLogEntries(string operationId, int lineToStartFrom);
	}
}
