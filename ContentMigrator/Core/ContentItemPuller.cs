using Rainbow.Model;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SitecoreSidekick.Services.Interface;

namespace ScsContentMigrator.Core
{
	class ContentItemPuller : IContentItemPuller
	{
		internal readonly BlockingCollection<IItemData> GatheredRemoteItems = new BlockingCollection<IItemData>();
		internal readonly BlockingCollection<Guid> ProcessingIds = new BlockingCollection<Guid>();
		private readonly IRemoteContentService _remoteContent;
		private readonly IYamlSerializationService _yamlSerializationService;
		private readonly ILoggingService _log;
		private readonly ISitecoreDataAccessService _sitecore;
		private readonly object _locker = new object();
		private int _processing = 0;

		public ContentItemPuller()
		{
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
			_yamlSerializationService = Bootstrap.Container.Resolve<IYamlSerializationService>();
			_sitecore = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
			_log = Bootstrap.Container.Resolve<ILoggingService>();
		}

		public BlockingCollection<IItemData> ItemsToInstall => GatheredRemoteItems;

		public bool Completed { get; private set; }

		public void StartGatheringItems(IEnumerable<Guid> rootIds, int threads, bool getChildren, string server, CancellationToken cancellationToken, bool ignoreRevId)
		{			
			foreach (Guid id in rootIds)
			{
				ProcessingIds.Add(id);
			}
			for (int i = 0; i < threads; i++)
			{
				Task.Run(() =>
				{
					GatherItems(getChildren, server, cancellationToken, ignoreRevId);
				});
			}
		}

		internal void GatherItems(bool getChildren, string server, CancellationToken cancellationToken, bool ignoreRevId)
		{			
			while (!Completed)
			{
				try
				{
					Guid id;
					if (!ProcessingIds.TryTake(out id, int.MaxValue, cancellationToken))
					{
						break;
					}
					lock (_locker)
						_processing++;
					ChildrenItemDataModel remoteContentItem = _remoteContent.GetRemoteItemDataWithChildren(id, server, ignoreRevId ? null : _sitecore.GetItemAndChildrenRevision(id));
					foreach (var item in (getChildren ? remoteContentItem.Items : remoteContentItem.Items.Where(x => x.Key == id)))
					{
						if (item.Value != null)
						{
							IItemData itemData = _yamlSerializationService.DeserializeYaml(item.Value, item.Key.ToString());
							GatheredRemoteItems.Add(itemData, cancellationToken);
						}
						else
						{
							GatheredRemoteItems.Add(_sitecore.GetItemData(item.Key), cancellationToken);
						}
					}
					if (getChildren && remoteContentItem.GrandChildren != null)
					{
						foreach (var child in remoteContentItem.GrandChildren)
						{
							ProcessingIds.Add(child, cancellationToken);
						}
					}
				}
				catch (OperationCanceledException e)
				{
					_log.Warn("Content migration operation was cancelled", e, this);
					break;
				}
				catch (Exception e)
				{
					_log.Error("error processing ids to gather remote content", e, this);
				}
				lock (_locker)
				{
					_processing--;
					if (_processing > 0 || ProcessingIds.Count != 0)
					{
						continue;
					}
				}
				Completed = true;
				ProcessingIds.CompleteAdding();
				GatheredRemoteItems.CompleteAdding();
			}
		}
	}
}
