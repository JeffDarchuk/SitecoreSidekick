using System.Collections.Generic;
using ScsContentMigrator.Args;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Models;

namespace ScsContentMigrator.Services.Interface
{
	public interface IContentMigrationManagerService
	{
		string StartContentMigration(PullItemModel args);
		bool CancelContentMigration(string operationId);
		IContentMigration GetContentMigration(string operationId);
		IEnumerable<IContentMigration> GetAllContentMigrations();
		IEnumerable<dynamic> GetItemLogEntries(string operationId, int lineToStartFrom);
		IEnumerable<string> GetAuditLogEntries(string operationId, int lineToStartFrom);
	}
}
