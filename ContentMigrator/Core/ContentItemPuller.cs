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
		private int _maxQueue;

		public ContentItemPuller(int maxQueue)
		{
			_maxQueue = maxQueue;
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
				Task.Run(async () =>
				{
					await GatherItems(getChildren, server, cancellationToken, ignoreRevId);
				});
			}
		}

		internal async Task GatherItems(bool getChildren, string server, CancellationToken cancellationToken, bool ignoreRevId)
		{
			ChildrenItemDataModel buffer = null;
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
					if (buffer == null)
					{
						buffer = _remoteContent.GetRemoteItemDataWithChildren(id, server, ignoreRevId ? null : _sitecore.GetItemAndChildrenRevision(id));
					}
					if (GatheredRemoteItems.Count >= _maxQueue)
					{
						await Task.Delay(1000);
					}
					else
					{
						foreach (var item in (getChildren ? buffer.Items : buffer.Items.Where(x => x.Key == id)))
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
						if (getChildren && buffer.GrandChildren != null)
						{
							foreach (var child in buffer.GrandChildren)
							{
								ProcessingIds.Add(child, cancellationToken);
							}
						}
						buffer = null;
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
