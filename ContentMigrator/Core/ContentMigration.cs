using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ScsContentMigrator.Args;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services.Interface;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator.Core
{
	public class ContentMigration : IContentMigration
	{
		private readonly IContentItemPuller _puller;
		private readonly IContentItemInstaller _installer;
		private readonly IRemoteContentService _remoteContent;
		private readonly ISitecoreAccessService _sitecoreAccess;
		private readonly IScsRegistrationService _registration;
		private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
		private PullItemModel _model;
		public ContentMigrationOperationStatus Status => _installer.Status;
		public ContentMigration()
		{
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
			_sitecoreAccess = Bootstrap.Container.Resolve<ISitecoreAccessService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_puller = new ContentItemPuller();
			_installer = new ContentItemInstaller();
		}

		public ContentMigration(IContentItemPuller puller, IContentItemInstaller installer, IRemoteContentService remoteContent, ISitecoreAccessService sitecoreAccess, IScsRegistrationService registration)
		{
			_puller = puller;
			_installer = installer;
			_remoteContent = remoteContent;
			_sitecoreAccess = sitecoreAccess;
			_registration = registration;
		}

		public int ItemsInQueueToInstall => _puller.ItemsToInstall.Count;
		public void StartContentMigration(PullItemModel model)
		{
			_model = model;
			if (model.PullParent)
			{
				foreach (var id in model.Ids.Select(Guid.Parse).Where(x => _sitecoreAccess.GetItem(x) == null))
				{
					var item = _remoteContent.GetRemoteItemData(id, model.Server);
					var parent = _sitecoreAccess.GetItem(item.ParentId);
					while (parent == null)
					{
						item = _remoteContent.GetRemoteItemData(item.ParentId, model.Server);
						_puller.ItemsToInstall.Add(item);
						parent = _sitecoreAccess.GetItem(item.ParentId);
					}
				}
			}
			if (model.RemoveLocalNotInRemote)
			{
				_installer.SetupTrackerForUnwantedLocalItems(model.Ids.Select(Guid.Parse));
			}
			_puller.StartGatheringItems(model.Ids.Select(Guid.Parse), _registration.GetScsRegistration<ContentMigrationRegistration>().RemoteThreads, model.Children, model.Server, _cancellation);
			_installer.StartInstallingItems(model, _puller.ItemsToInstall, _registration.GetScsRegistration<ContentMigrationRegistration>().WriterThreads, _cancellation);
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
			StartContentMigration(_model);
		}
	}
}
