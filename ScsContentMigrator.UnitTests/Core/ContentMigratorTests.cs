using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Rainbow.Model;
using ScsContentMigrator.Core;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services.Interface;
using Sitecore.Data.Items;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;
using Xunit;

namespace ScsContentMigrator.UnitTests.Core
{
	public class ContentMigratorTests : TestBase
	{
		[Fact]
		public void StartContentMigration_PullParent_AttemptsToGetFromSitecore()
		{
			Guid initialTarget = Guid.NewGuid();
			Guid parent = Guid.NewGuid();
			var item = Substitute.For<IItemData>();
			item.ParentId.Returns(parent);
			ContentMigration contentMigration = CreateInstance<ContentMigration>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemData(Arg.Any<Guid>(), Arg.Any<string>()).Returns(Substitute.For<IItemData>());
			GetSubstitute<IContentItemPuller>().ItemsToInstall.Returns(new BlockingCollection<IItemData>());
			GetSubstitute<ISitecoreDataAccessService>().GetItemData(parent).Returns(Substitute.For<IItemData>());			

			contentMigration.StartContentMigration(new PullItemModel {PullParent = true, Ids = new List<string> { initialTarget.ToString()}});

			GetSubstitute<ISitecoreDataAccessService>().Received(1).GetItemData(initialTarget);
		}

		[Fact]
		public void StartContentMigration_PullParent_PullsFromSitecoreUntilParentNodeIsFound()
		{
			Guid initialTarget = Guid.NewGuid();
			Guid firstParent = Guid.NewGuid();
			Guid secondParent = Guid.NewGuid();
			var item = Substitute.For<IItemData>();
			item.ParentId.Returns(firstParent);
			var parentItem = Substitute.For<IItemData>();
			parentItem.ParentId.Returns(secondParent);
			ContentMigration contentMigration = CreateInstance<ContentMigration>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemData(initialTarget, Arg.Any<string>()).Returns(item);
			GetSubstitute<IRemoteContentService>().GetRemoteItemData(firstParent, Arg.Any<string>()).Returns(parentItem);
			GetSubstitute<IRemoteContentService>().GetRemoteItemData(secondParent, Arg.Any<string>()).Returns(Substitute.For<IItemData>());
			GetSubstitute<IContentItemPuller>().ItemsToInstall.Returns(new BlockingCollection<IItemData>());
			GetSubstitute<ISitecoreDataAccessService>().GetItemData(initialTarget).Returns((IItemData)null);
			GetSubstitute<ISitecoreDataAccessService>().GetItemData(firstParent).Returns((IItemData)null);
			GetSubstitute<ISitecoreDataAccessService>().GetItemData(secondParent).Returns(item);			

			contentMigration.StartContentMigration(new PullItemModel { PullParent = true, Ids = new List<string> { initialTarget.ToString() } });

			GetSubstitute<ISitecoreDataAccessService>().Received(1).GetItemData(initialTarget);
			GetSubstitute<ISitecoreDataAccessService>().Received(1).GetItemData(firstParent);
			GetSubstitute<ISitecoreDataAccessService>().Received(1).GetItemData(secondParent);
		}

		[Fact]
		public void StartContentMigration_NotPullingParent_DoesNotPullFromSitecoreOrRemote()
		{
			ContentMigration contentMigration = CreateInstance<ContentMigration>();			
			GetSubstitute<IContentItemPuller>().ItemsToInstall.Returns(new BlockingCollection<IItemData>());

			contentMigration.StartContentMigration(new PullItemModel {PullParent = false, Ids = new List<string>{Guid.NewGuid().ToString()}});

			GetSubstitute<IRemoteContentService>().Received(0).GetRemoteItemData(Arg.Any<Guid>(), Arg.Any<string>());
			GetSubstitute<ISitecoreDataAccessService>().Received(0).GetItemData(Arg.Any<Guid>());
		}

		[Fact]
		public void StartContentMigration_RemoveLocalNotInRemote_SetsUpTrackerForUnwantedLocalItems()
		{
			ContentMigration contentMigration = CreateInstance<ContentMigration>();			
			GetSubstitute<IContentItemPuller>().ItemsToInstall.Returns(new BlockingCollection<IItemData>());

			contentMigration.StartContentMigration(new PullItemModel { PullParent = false, RemoveLocalNotInRemote = true, Ids = new List<string> { Guid.NewGuid().ToString() } });

			GetSubstitute<IContentItemInstaller>().Received(1).SetupTrackerForUnwantedLocalItems(Arg.Any<IEnumerable<Guid>>());
		}

		[Fact]
		public void StartContentMigration_PullerStartsGatheringItems()
		{
			ContentMigration contentMigration = CreateInstance<ContentMigration>();			
			GetSubstitute<IContentItemPuller>().ItemsToInstall.Returns(new BlockingCollection<IItemData>());

			contentMigration.StartContentMigration(new PullItemModel { PullParent = false, Ids = new List<string> { Guid.NewGuid().ToString() } });

			GetSubstitute<IContentItemPuller>().Received(1).StartGatheringItems(Arg.Any<IEnumerable<Guid>>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
		}

		[Fact]
		public void StartContentMigration_InstallerStartsInstallingItems()
		{			
			ContentMigration contentMigration = CreateInstance<ContentMigration>();
			GetSubstitute<ISitecoreDataAccessService>();			
			GetSubstitute<IContentItemPuller>().ItemsToInstall.Returns(new BlockingCollection<IItemData>());

			contentMigration.StartContentMigration(new PullItemModel { PullParent = false, Ids = new List<string> { Guid.NewGuid().ToString() } });

			GetSubstitute<IContentItemInstaller>().Received(1).StartInstallingItems(Arg.Any<PullItemModel>(), Arg.Any<BlockingCollection<IItemData>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
		}

	}
}
