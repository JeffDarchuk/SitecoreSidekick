using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Rainbow.Model;
using Rainbow.Storage.Yaml;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Data;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using Sitecore.Diagnostics;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator.Core
{
	class ContentItemPuller : IContentItemPuller
	{
		private readonly BlockingCollection<IItemData> _gatheredRemoteItems = new BlockingCollection<IItemData>();
		private readonly BlockingCollection<Guid> _processingIds = new BlockingCollection<Guid>();
		private readonly IRemoteContentService _remoteContent;
		private readonly object _locker = new object();

		public ContentItemPuller()
		{
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
		}
		public ContentItemPuller(IRemoteContentService remoteContent)
		{
			_remoteContent = remoteContent;
		}
		public BlockingCollection<IItemData> ItemsToInstall => _gatheredRemoteItems;

		public bool Completed { get; private set; }

		public void StartGatheringItems(IEnumerable<Guid> rootIds, int threads, bool getChildren, string server, CancellationTokenSource cancellation)
		{
			int processing = 0;
			foreach (Guid id in rootIds)
			{
				_processingIds.Add(id);
			}
			for (int i = 0; i < threads; i++)
			{
				Task.Run(() =>
				{
					Thread.CurrentThread.Priority = ThreadPriority.Lowest;
					while (!Completed)
					{
						try
						{
							Guid id;
							if (!_processingIds.TryTake(out id, int.MaxValue, cancellation.Token))
							{
								break;
							}
							lock(_locker)
								processing++;
							ChildrenItemDataModel remoteContentItem = _remoteContent.GetRemoteItemDataWithChildren(id, server);
							IItemData itemData = DeserializeYaml(remoteContentItem.Item, id.ToString());
							_gatheredRemoteItems.Add(itemData);
							if (getChildren)
							{
								foreach (var child in remoteContentItem.Children)
								{
									_processingIds.Add(child);
								}
							}
						}
						catch (OperationCanceledException e)
						{
							Log.Warn("Content migration operation was cancelled", e, this);
							lock (_locker)
							{
								processing--;
								if (processing > 0 || _processingIds.Count != 0)
								{
									continue;
								}
							}
							Completed = true;
							_processingIds.CompleteAdding();
							_gatheredRemoteItems.CompleteAdding();
							break;
						}
						catch (Exception e)
						{
							Log.Error("error processing ids to gather remote content", e, this);
						}
						lock (_locker)
						{
							processing--;
							if (processing > 0 || _processingIds.Count != 0)
							{
								continue;
							}
						}
						Completed = true;
						_processingIds.CompleteAdding();
						_gatheredRemoteItems.CompleteAdding();
					}
				});
			}
		}
		public static IItemData DeserializeYaml(string yaml, string itemId)
		{
			var formatter = new YamlSerializationFormatter(null, null);
			if (yaml != null)
			{
				using (var ms = new MemoryStream())
				{
					IItemData itemData = null;
					try
					{
						var bytes = Encoding.UTF8.GetBytes(yaml);
						ms.Write(bytes, 0, bytes.Length);

						ms.Seek(0, SeekOrigin.Begin);
						itemData = formatter.ReadSerializedItem(ms, itemId);
					}
					catch (Exception e)
					{
						Log.Error("Problem reading yaml from remote server", e, typeof(RemoteContentService));
					}
					if (itemData != null)
					{
						return itemData;
					}
				}
			}
			return null;
		}
	}
}
