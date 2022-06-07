using Sidekick.ContentMigrator.Core.Interface;
using Sidekick.ContentMigrator.Models;
using Sidekick.ContentMigrator.Services.Interface;
using Sidekick.Core.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sidekick.ContentMigrator.Core
{
	public class ContentMigration : IContentMigration
	{
		private IContentItemPuller _puller;
		private IContentItemInstaller _installer;
		private readonly IRemoteContentService _remoteContent;
		private readonly ISitecoreDataAccessService _sitecoreAccess;
		private readonly IScsRegistrationService _registration;
		private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
		private PullItemModel _model;
		public ContentMigrationOperationStatus Status => _installer.Status;
		public ContentMigration()
		{
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
			_sitecoreAccess = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_puller = Bootstrap.Container.Resolve<IContentItemPuller>();
			_installer = Bootstrap.Container.Resolve<IContentItemInstaller>();
		}

		public int ItemsInQueueToInstall => _puller.ItemsToInstall.Count;
		public void StartContentMigration(PullItemModel model)
		{
			_model = model;
			if (model.PullParent)
			{
				foreach (var id in model.Ids.Select(Guid.Parse).Where(x => _sitecoreAccess.GetItemData(x, model.Database) == null))
				{
					var item = _remoteContent.GetRemoteItemData(id, model.Database, model.Server);
					var parent = _sitecoreAccess.GetItemData(item.ParentId, model.Database);
					while (parent == null)
					{
						item = _remoteContent.GetRemoteItemData(item.ParentId, model.Database, model.Server);
						_puller.ItemsToInstall.Add(item);
						parent = _sitecoreAccess.GetItemData(item.ParentId, model.Database);
					}
				}
			}
			Task.Run(() =>
			{
				if (model.RemoveLocalNotInRemote)
				{
					_installer.SetupTrackerForUnwantedLocalItems(model.Ids.Select(Guid.Parse));
				}
				_puller.StartGatheringItems(model.Ids.Select(Guid.Parse), model.Database, _registration.GetScsRegistration<ContentMigrationRegistration>()?.RemoteThreads ?? 1, model.Children, model.Server, _cancellation.Token, model.IgnoreRevId);
				_installer.StartInstallingItems(model, _puller.ItemsToInstall, _registration.GetScsRegistration<ContentMigrationRegistration>()?.WriterThreads ?? 1, _cancellation.Token);
			});
		}

		public void CancelMigration()
		{
			_cancellation.Cancel();
		}

		public IEnumerable<dynamic> GetItemLogEntries(int lineToStartFrom)
		{
			return _installer.GetItemLogEntries(lineToStartFrom);
		}

		public IEnumerable<string> GetAuditLogEntries(int lineToStartFrom)
		{
			return _installer.GetAuditLogEntries(lineToStartFrom);
		}

		public void StartOperationFromPreview()
		{
			if (_model == null)
			{
				throw new ArgumentNullException("Cannot start an operation as a preview if it hasn't been started as a preview.");
			}
			_model.Preview = false;
			_puller = new ContentItemPuller(100);
			_installer = new ContentItemInstaller();
			StartContentMigration(_model);
		}
	}
}
