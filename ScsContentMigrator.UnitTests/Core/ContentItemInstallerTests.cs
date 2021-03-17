using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Rainbow.Diff;
using Rainbow.Model;
using Rainbow.Storage;
using Rainbow.Storage.Sc;
using Sidekick.ContentMigrator.CMRainbow;
using Sidekick.ContentMigrator.CMRainbow.Interface;
using Sidekick.ContentMigrator.Core;
using Sidekick.ContentMigrator.Models;
using Sidekick.ContentMigrator.Services.Interface;
using Sitecore.Data.Items;
using Sidekick.Core.Services.Interface;
using Xunit;
using Sidekick.ContentMigrator;

namespace Sidekick.ContentMigrator.UnitTests.Core
{
	public class ContentItemInstallerTests : TestBase
	{
		[Fact]
		public void GetItemLogEntries_LineToStartFrom_SkipsFirstItems()
		{
			int lineToStartFrom = 3;
			List<dynamic> lines = new List<dynamic> {"One", "Two", "Three", "Four", "Five"};
			GetSubstitute<IDefaultLogger>().Lines = lines;
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			
			var actual = contentItemInstaller.GetItemLogEntries(lineToStartFrom);

			actual.Should().BeEquivalentTo(lines.Skip(lineToStartFrom));
		}

		[Fact]
		public void GetAuditLogEntries_LineToStartFrom_SkipsFirstItems()
		{
			int lineToStartFrom = 3;
			List<string> lines = new List<string> { "One", "Two", "Three", "Four", "Five" };
			GetSubstitute<IDefaultLogger>().LoggerOutput.Returns(lines);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();

			var actual = contentItemInstaller.GetAuditLogEntries(lineToStartFrom);

			actual.Should().BeEquivalentTo(lines.Skip(lineToStartFrom));
		}

		[Fact]
		public void CleanUnwantedLocalItems_EachItem_RecyclesItem()
		{
			var items = new[] {Guid.NewGuid(), Guid.NewGuid()};
			GetSubstitute<ISitecoreDataAccessService>().GetSubtreeOfGuids(Arg.Any<IEnumerable<Guid>>()).Returns(new HashSet<Guid>(items));
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.SetupTrackerForUnwantedLocalItems(items);

			contentItemInstaller.CleanUnwantedLocalItems();

			GetSubstitute<ISitecoreDataAccessService>().Received(items.Length).RecycleItem(Arg.Any<Guid>());
		}

		[Fact]
		public void CleanUnwantedLocalItems_EachItem_BeginsLoggingRecycleEvent()
		{
			var items = new[] { Guid.NewGuid(), Guid.NewGuid() };
			GetSubstitute<ISitecoreDataAccessService>().GetSubtreeOfGuids(Arg.Any<IEnumerable<Guid>>()).Returns(new HashSet<Guid>(items));
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.SetupTrackerForUnwantedLocalItems(items);

			contentItemInstaller.CleanUnwantedLocalItems();

			GetSubstitute<IDefaultLogger>().Received(items.Length).BeginEvent(Arg.Any<IItemData>(), LogStatus.Recycle, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void CleanUnwantedLocalItems_EachItem_Error_BeginsLoggingErrorEvent()
		{
			var items = new[] { Guid.NewGuid(), Guid.NewGuid() };
			GetSubstitute<ISitecoreDataAccessService>().GetSubtreeOfGuids(Arg.Any<IEnumerable<Guid>>()).Returns(new HashSet<Guid>(items));
			GetSubstitute<ISitecoreDataAccessService>().When(sas=>sas.RecycleItem(Arg.Any<Guid>())).Throw(new Exception("Something bad happened"));
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.SetupTrackerForUnwantedLocalItems(items);

			contentItemInstaller.CleanUnwantedLocalItems();

			GetSubstitute<IDefaultLogger>().Received(items.Length).BeginEvent(Arg.Any<ErrorItemData>(), LogStatus.Error, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_ItemExistsInAllowedItems_RemovesFromAllowedItems()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel {Preview = true}, Substitute.For<IItemData>(), remoteData);

			contentItemInstaller.AllowedItems.Should().BeEmpty();
		}

		[Fact]
		public void ProcessItem_ItemDoesNotExistInAllowedItems_DoesNotThrowException()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			try
			{
				contentItemInstaller.ProcessItem(new PullItemModel {Preview = true}, Substitute.For<IItemData>(), remoteData);
			}
			catch (Exception ex)
			{
				Assert.True(false, $"A {ex.GetType()} exception was thrown: {ex.Message}");
			}
		}

		[Fact]
		public void ProcessItem_Preview_LocalItemDoesNotExist_LogsCreated()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Preview = true }, null, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), LogStatus.Created, Arg.Any<string>(), Arg.Any<bool>());
		}
		
		public static IEnumerable<object[]> ProcessItemPreviewTestCases
		{
			get
			{
				yield return new object[] { CreateComparisonResult(cr => cr.AreEqual.Returns(true)), LogStatus.Skipped };
				yield return new object[] { CreateComparisonResult(null, true), LogStatus.Renamed };
				yield return new object[] { CreateComparisonResult(null, false, true), LogStatus.Moved };				
				yield return new object[] { CreateComparisonResult(null, false, false, true), LogStatus.TemplateChange };				
			}
		}

		[Theory]
		[MemberData(nameof(ProcessItemPreviewTestCases), MemberType = typeof(ContentItemInstallerTests))]
		public void ProcessItem_Preview_CompareResults_Logs(ItemComparisonResult compareResult, string expectedLogEvent)
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(compareResult);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Preview = true }, remoteData, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), expectedLogEvent, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_Preview_CompareResults_Overwrite_LogsChanged()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Preview = true, Overwrite = true}, remoteData, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), LogStatus.Changed, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_NotOverwrite_LocalDataExists_LogsEventSkipped()
		{			
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = false }, remoteData, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), LogStatus.Skipped, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_NotOverwrite_LocalDataExists_DoesNotSaveToDataStore()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = false }, remoteData, remoteData);

			GetSubstitute<IDataStore>().Received(0).Save(Arg.Any<IItemData>(), GetSubstitute<IFieldValueManipulator>());
		}

		[Fact]
		public void ProcessItem_Overwrite_LocalDataExists_SameAsRemoteData_LogsSkipped()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			ItemComparisonResult icr = CreateComparisonResult(cr => cr.AreEqual.Returns(true));
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(icr);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = true }, remoteData, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), LogStatus.Skipped, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_Overwrite_LocalDataExists_SameAsRemoteData_DoesNotSaveToDataStore()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			ItemComparisonResult icr = CreateComparisonResult(cr => cr.AreEqual.Returns(true));
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(icr);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = true }, remoteData, remoteData);

			GetSubstitute<IDataStore>().Received(0).Save(Arg.Any<IItemData>(), GetSubstitute<IFieldValueManipulator>());
		}

		[Fact]
		public void ProcessItem_LocalDataExists_SameAsRemoteData_LogsSkipped()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			ItemComparisonResult icr = CreateComparisonResult(cr => cr.AreEqual.Returns(true));
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(icr);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel {  }, remoteData, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), LogStatus.Skipped, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_LocalDataExists_SameAsRemoteData_DoesNotSaveToDataStore()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			ItemComparisonResult icr = CreateComparisonResult(cr => cr.AreEqual.Returns(true));
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(icr);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel {  }, remoteData, remoteData);

			GetSubstitute<IDataStore>().Received(0).Save(Arg.Any<IItemData>(), GetSubstitute<IFieldValueManipulator>());
		}

		[Fact]
		public void ProcessItem_LocalDataDoesNotExist_RemoteParentInError_DoesNotSaveToDataStore()
		{
			Guid itemGuid = Guid.NewGuid();
			Guid parentItemGuid = Guid.NewGuid();			
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			remoteData.ParentId.Returns(parentItemGuid);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);
			contentItemInstaller.Errors.Add(parentItemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { }, null, remoteData);

			GetSubstitute<IDataStore>().Received(0).Save(Arg.Any<IItemData>(), GetSubstitute<IFieldValueManipulator>());
		}

		[Fact]
		public void ProcessItem_Overwrite_LocalDataExists_LogsChanged()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			GetSubstitute<IDefaultLogger>().HasLinesSupportEvents(Arg.Any<string>()).Returns(true);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = true }, remoteData, remoteData);

			GetSubstitute<IDefaultLogger>().Received(1).BeginEvent(Arg.Any<IItemData>(), LogStatus.Changed, Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_Overwrite_LocalDataExists_SavesToDataStore()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			GetSubstitute<IDefaultLogger>().HasLinesSupportEvents(Arg.Any<string>()).Returns(true);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = true }, remoteData, remoteData);

			GetSubstitute<IDataStore>().Received(1).Save(Arg.Any<IItemData>(), GetSubstitute<IFieldValueManipulator>());
		}

		[Fact]
		public void ProcessItem_Overwrite_LocalDataDoesNotExist_DoesNotBeginLogEvent()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			GetSubstitute<IDefaultLogger>().HasLinesSupportEvents(Arg.Any<string>()).Returns(true);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = true }, null, remoteData);

			GetSubstitute<IDefaultLogger>().Received(0).BeginEvent(Arg.Any<IItemData>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
		}

		[Fact]
		public void ProcessItem_Overwrite_LocalDataDoesNotExist_SavesToDataStore()
		{
			Guid itemGuid = Guid.NewGuid();
			IItemData remoteData = Substitute.For<IItemData>();
			remoteData.Id.Returns(itemGuid);
			GetSubstitute<IItemComparer>().Compare(Arg.Any<IItemData>(), Arg.Any<IItemData>()).Returns(CreateComparisonResult());
			GetSubstitute<IDefaultLogger>().HasLinesSupportEvents(Arg.Any<string>()).Returns(true);
			var contentItemInstaller = CreateInstance<ContentItemInstaller>();
			contentItemInstaller.AllowedItems.Add(itemGuid);

			contentItemInstaller.ProcessItem(new PullItemModel { Overwrite = true }, null, remoteData);

			GetSubstitute<IDataStore>().Received(1).Save(Arg.Any<IItemData>(), GetSubstitute<IFieldValueManipulator>());
		}


		#region Helper Functions
		private static ItemComparisonResult CreateComparisonResult(Action<ItemComparisonResult> substituteAction = null, bool isRenamed = false, bool isMoved = false, bool isTemplateChanged = false)
		{
			IItemData data = Substitute.For<IItemData>();
			var comparisonResult = Substitute.For<ItemComparisonResult>(data, data, isRenamed, isMoved, isTemplateChanged, false, null, null, null);
			substituteAction?.Invoke(comparisonResult);
			return comparisonResult;
		}
		#endregion

	}
}
