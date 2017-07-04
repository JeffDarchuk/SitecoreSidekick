using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using ScsContentMigrator.Args;
using ScsContentMigrator.Core;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services.Interface;

namespace ScsContentMigrator.Services
{
	public class ContentMigrationManagerService : IContentMigrationManagerService
	{

		private readonly Dictionary<string, IContentMigration> _migrations = new Dictionary<string, IContentMigration>();
		public string StartContentMigration(PullItemModel model)
		{
			string id = Guid.NewGuid().ToString();
			ContentMigration newMigration = new ContentMigration();
			newMigration.Status.OperationId = id;
			newMigration.StartContentMigration(model);
			_migrations.Add(id, newMigration);
			return id;
		}

		public bool CancelContentMigration(string operationId)
		{
			IContentMigration ret;
			_migrations.TryGetValue(operationId, out ret);
			ret?.CancelMigration();
			if (ret == null)
				return false;
			return true;
		}

		public IContentMigration GetContentMigration(string operationId)
		{
			IContentMigration ret;
			_migrations.TryGetValue(operationId, out ret);
			return ret;
		}

		public IEnumerable<IContentMigration> GetAllContentMigrations()
		{
			return _migrations.Values.ToArray();
		}

		public IEnumerable<string> GetAuditLogEntries(string operationId, int fromLineNumber)
		{
			IContentMigration ret;
			_migrations.TryGetValue(operationId, out ret);
			return ret?.GetAuditLogEntries(fromLineNumber) ?? new List<string>();
		}

		public IEnumerable<dynamic> GetItemLogEntries(string operationId, int fromLineNumber)
		{
			IContentMigration ret;
			_migrations.TryGetValue(operationId, out ret);
			return ret?.GetItemLogEntries(fromLineNumber) ?? new List<dynamic>();
		}
	}
}
