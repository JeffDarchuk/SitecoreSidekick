﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Sidekick.ContentMigrator.Core;
using Sidekick.ContentMigrator.Models;
using Sidekick.ContentMigrator.Services.Interface;
using Xunit;

namespace Sidekick.ContentMigrator.UnitTests.Core
{
	public class ContentItemPullerTests : TestBase
	{
		[Fact]
		public void StartGatheringItems_AddsRootIdsToProcessingCollection()
		{
			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();

			List<Guid> expectedGuids = new List<Guid> {Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()};

			contentItemPuller.StartGatheringItems(expectedGuids, "master", 0, false, "", CancellationToken.None, false);

			contentItemPuller.ProcessingIds.Count.Should().Be(expectedGuids.Count);
		}

		[Fact]
		public void GatherItems_TakesFromProcessingList()
		{
			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());
			
			contentItemPuller.GatherItems(false, "master", "", CancellationToken.None, false);

			contentItemPuller.ProcessingIds.Should().BeEmpty();
		}

		[Fact]
		public void GatherItems_FetchesRemoteContent()
		{
			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			contentItemPuller.GatherItems(false, "master", "", CancellationToken.None, false);

			GetSubstitute<IRemoteContentService>().Received(1).GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
		}

		[Fact]
		public void GatherItems_DeserializesRemoteContentItem()
		{
			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			contentItemPuller.GatherItems(false, "master", "", CancellationToken.None, false);

			GetSubstitute<IYamlSerializationService>().Received(1).DeserializeYaml(Arg.Any<string>(), Arg.Any<string>());
		}

		[Fact]
		public void GatherItems_AddsDeserializedValueToGatheredRemoteItems()
		{
			IItemData expectedItemData = Substitute.For<IItemData>();
			expectedItemData.Name.Returns("ExpectedName");
			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			GetSubstitute<IYamlSerializationService>().DeserializeYaml(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedItemData);
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			contentItemPuller.GatherItems(false, "master", "", CancellationToken.None, false);

			contentItemPuller.GatheredRemoteItems.Should().NotBeEmpty();
			contentItemPuller.GatheredRemoteItems.Should().Contain(expectedItemData);
		}

		[Fact]
		public void GatherItems_DoNotGetChildren_DoesNotGetChildren()
		{
			IItemData expectedItemData = Substitute.For<IItemData>();
			expectedItemData.Name.Returns("ExpectedName");
			expectedItemData.GetChildren().Returns(new List<IItemData> {Substitute.For<IItemData>()});

			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			GetSubstitute<IYamlSerializationService>().DeserializeYaml(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedItemData);
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			contentItemPuller.GatherItems(false, "master", "", CancellationToken.None, false);

			contentItemPuller.GatheredRemoteItems.Count.Should().Be(1);
		}

		[Fact]
		public void GatherItems_GetsChildren_ProcessesChildren()
		{
			Guid parentGuid = Guid.NewGuid();
			Guid childGuid = Guid.NewGuid(); 
			IItemData expectedItemData = Substitute.For<IItemData>();
			expectedItemData.Name.Returns("ExpectedName");
			expectedItemData.GetChildren().Returns(new List<IItemData> { Substitute.For<IItemData>() });

			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(parentGuid, Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel {GrandChildren = new List<Guid> {childGuid}});
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(childGuid, Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			GetSubstitute<IYamlSerializationService>().DeserializeYaml(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedItemData);
			contentItemPuller.ProcessingIds.Add(parentGuid);

			contentItemPuller.GatherItems(true, "master", "", CancellationToken.None, false);

			contentItemPuller.GatheredRemoteItems.Count.Should().Be(2);
		}

		[Fact]
		public void GatherItems_MultipleItems_CanceledDuringFirstItem_OnlyGathersOneItem()
		{
			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(ci =>
			{
				// Introduce a delay
				Thread.Sleep(10);
				return new ChildrenItemDataModel();
			});
			GetSubstitute<IYamlSerializationService>().DeserializeYaml(Arg.Any<string>(), Arg.Any<string>()).Returns(Substitute.For<IItemData>());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			CancellationTokenSource cts = new CancellationTokenSource(10);
			contentItemPuller.GatherItems(false, "master", "", cts.Token, false);

			contentItemPuller.GatheredRemoteItems.Count.Should().BeLessThan(3);
		}

		[Fact]
		public void GatherItems_MultipleItems_ProcessesEachItem()
		{
			IItemData expectedItemData = Substitute.For<IItemData>();
			expectedItemData.Name.Returns("ExpectedName");
			expectedItemData.GetChildren().Returns(new List<IItemData> { Substitute.For<IItemData>() });

			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			GetSubstitute<IYamlSerializationService>().DeserializeYaml(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedItemData);
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());
			contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			contentItemPuller.GatherItems(false, "master", "", CancellationToken.None, false);

			contentItemPuller.GatheredRemoteItems.Count.Should().Be(3);
		}

		[Fact]
		public async Task GatherItems_MultipleThreads_AllCompleteWhenFinished()
		{
			int expectedCount = 1000;
			IItemData expectedItemData = Substitute.For<IItemData>();
			expectedItemData.Name.Returns("ExpectedName");
			expectedItemData.GetChildren().Returns(new List<IItemData> { Substitute.For<IItemData>() });

			ContentItemPuller contentItemPuller = CreateInstance<ContentItemPuller>();
			GetSubstitute<IRemoteContentService>().GetRemoteItemDataWithChildren(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(new ChildrenItemDataModel());
			GetSubstitute<IYamlSerializationService>().DeserializeYaml(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedItemData);
			for (int i = 0; i < expectedCount; i++)
				contentItemPuller.ProcessingIds.Add(Guid.NewGuid());

			var cancellationTokenSource = new CancellationTokenSource();

			Task[] taskList =
			{
				Task.Run(() =>
				{
					contentItemPuller.GatherItems(false, "master", "", cancellationTokenSource.Token, false);
				}),
				Task.Run(() =>
				{
					contentItemPuller.GatherItems(false, "master", "", cancellationTokenSource.Token, false);
				}),
				Task.Run(() =>
				{
					contentItemPuller.GatherItems(false, "master", "", cancellationTokenSource.Token, false);
				})
			};

			await Task.WhenAny(taskList);

			contentItemPuller.GatheredRemoteItems.Count.Should().Be(expectedCount);
			taskList.All(t => t.IsCompleted).Should().BeTrue();
		}
	}
}
