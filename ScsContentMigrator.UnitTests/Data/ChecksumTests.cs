using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ScsContentMigrator.Data;
using Xunit;

namespace ScsContentMigrator.UnitTests.Data
{
	public class ChecksumTests : TestBase
	{
		[Fact]
		public void LoadRow_AddsToParentTracker()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._parentTracker.Should().ContainKey(expectedId);
			checksum._parentTracker[expectedId].Should().Be(expectedParentId);
		}

		[Fact]
		public void LoadRow_NonexistentChildTracker_CreatesAndAddsParentIdToChildTracker()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._childTracker.Should().ContainKey(expectedParentId);
			checksum._childTracker[expectedParentId].Should().Contain(expectedId);
		}

		[Fact]
		public void LoadRow_ExistingChildTracker_AddsParentIdToChildTracker()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string existingId = "ExistingId";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._childTracker.Add(expectedParentId, new List<string> {existingId});

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._childTracker.Should().ContainKey(expectedParentId);
			checksum._childTracker[expectedParentId].Should().Contain(expectedId);
			checksum._childTracker[expectedParentId].Should().Contain(existingId);
		}

		[Fact]
		public void LoadRow_NonexistentChecksumTracker_CreatesAndAddsValue()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._checksumTracker.Should().ContainKey(expectedId);
			checksum._checksumTracker[expectedId].Should().Contain(expectedValue);
		}

		[Fact]
		public void LoadRow_ExistingChecksumTracker_AddsValue()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string existingValue = "ExistingValue";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._checksumTracker.Add(expectedId, new SortedSet<string> {existingValue});

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._checksumTracker.Should().ContainKey(expectedId);
			checksum._checksumTracker[expectedId].Should().Contain(expectedValue);
			checksum._checksumTracker[expectedId].Should().Contain(existingValue);
		}

		[Fact]
		public void LoadRow_ChildTrackerDoesNotContainId_AddsToLeafTracker()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._leafTracker.Should().Contain(expectedId);
		}

		[Fact]
		public void LoadRow_ChildTrackerContainsId_DoesNotAddToLeafTracker()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._childTracker.Add(expectedId, new List<string>());
			
			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._leafTracker.Should().NotContain(expectedId);
		}

		[Fact]
		public void LoadRow_LeafTrackerRemovesParentId()
		{
			string expectedId = "Id";
			string expectedParentId = "Parent";
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._leafTracker.Add(expectedParentId);

			checksum.LoadRow(expectedId, expectedParentId, expectedValue);

			checksum._leafTracker.Should().NotContain(expectedParentId);
		}

		[Fact]
		public void GetChecksum_InvalidKey_ReturnsNegative1()
		{
			string key = "Not a guid";
			Checksum checksum = new Checksum();

			int actual = checksum.GetChecksum(key);

			actual.Should().Be(-1);
		}

		[Fact]
		public void GetChecksum_Exists_ReturnsChecksum()
		{
			string key = Guid.NewGuid().ToString();
			int existingChecksum = 42;
			Checksum checksum = new Checksum();
			checksum._checksum.Add(key, existingChecksum);

			int actual = checksum.GetChecksum(key);

			actual.Should().Be(existingChecksum);
		}

		[Fact]
		public void GetChecksum_DoesNotExist_ReturnsNegative1()
		{
			string key = Guid.NewGuid().ToString();			
			Checksum checksum = new Checksum();			

			int actual = checksum.GetChecksum(key);

			actual.Should().Be(-1);
		}

		[Fact]
		public void Generate_CreatesChecksums()
		{
			var plcs = PreloadChecksum();
			string[] guids = plcs.Item1;
			Checksum checksum = plcs.Item2;
			
			checksum.Generate();

			foreach (var guid in guids)
			{
				checksum._checksum.Should().ContainKey(guid);
				checksum._checksum.Where(c => c.Key != guid).Select(c => c.Value).Should().NotContain(checksum._checksum[guid]);
			}
		}

		[Fact]
		public void Generate_ClearsLists()
		{
			var plcs = PreloadChecksum();			
			Checksum checksum = plcs.Item2;

			checksum.Generate();

			checksum._checksumTracker.Should().BeEmpty();
			checksum._childTracker.Should().BeEmpty();
			checksum._parentTracker.Should().BeEmpty();
			checksum._leafTracker.Should().BeEmpty();
		}

		#region Helper functions
		private Tuple<string[], Checksum> PreloadChecksum()
		{
			string root = Guid.NewGuid().ToString();

			string parentA = Guid.NewGuid().ToString();
			string childA1 = Guid.NewGuid().ToString();
			string childA2 = Guid.NewGuid().ToString();

			string parentB = Guid.NewGuid().ToString();
			string subParentBa = Guid.NewGuid().ToString();
			string childBa1 = Guid.NewGuid().ToString();
			string childB1 = Guid.NewGuid().ToString();

			Checksum checksum = new Checksum();
			checksum._childTracker.Add(root, new List<string> { parentA, parentB });
			checksum._childTracker.Add(parentA, new List<string> { childA1, childA2 });
			checksum._childTracker.Add(parentB, new List<string> { subParentBa, childB1 });
			checksum._childTracker.Add(subParentBa, new List<string> { childBa1 });
			checksum._parentTracker.Add(childA1, parentA);
			checksum._parentTracker.Add(childA2, parentA);
			checksum._parentTracker.Add(subParentBa, parentB);
			checksum._parentTracker.Add(childB1, parentB);
			checksum._parentTracker.Add(childBa1, subParentBa);
			checksum._parentTracker.Add(parentA, root);
			checksum._parentTracker.Add(parentB, root);
			foreach (var guid in new[] { childA1, childA2, childB1, childBa1 })
				checksum._leafTracker.Add(guid);
			foreach (var guid in new[] { parentA, childA1, childA2, parentB, subParentBa, childBa1, childB1 })
				checksum._checksumTracker.Add(guid, new SortedSet<string> { guid });

			return new Tuple<string[], Checksum>(new[] {parentA, childA1, childA2, parentB, subParentBa, childBa1, childB1}, checksum);
		}
		#endregion
	}
}
