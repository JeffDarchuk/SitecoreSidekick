using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Sidekick.ContentMigrator.Data;
using Xunit;

namespace Sidekick.ContentMigrator.UnitTests.Data
{
	public class ChecksumTests : TestBase
	{
		[Fact]
		public void LoadRow_AddsToParentTracker()
		{
			string expectedId = Guid.NewGuid().ToString("N");
			string expectedParentId = Guid.NewGuid().ToString("N");
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue, "en", 1);

			checksum._parentTracker.Should().ContainKey(Guid.Parse(expectedId));
			checksum._parentTracker[Guid.Parse(expectedId)].Should().Be(Guid.Parse(expectedParentId));
		}

		[Fact]
		public void LoadRow_NonexistentChildTracker_CreatesAndAddsParentIdToChildTracker()
		{
			string expectedId = Guid.NewGuid().ToString("N");
			string expectedParentId = Guid.NewGuid().ToString("N");
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue, "en", 1);

			checksum._childTracker.Should().ContainKey(Guid.Parse(expectedParentId));
			checksum._childTracker[Guid.Parse(expectedParentId)].Should().Contain(expectedId);
		}

		[Fact]
		public void LoadRow_ExistingChildTracker_AddsParentIdToChildTracker()
		{
			string expectedId = Guid.NewGuid().ToString("N");
			string expectedParentId = Guid.NewGuid().ToString("N");
			string existingId = Guid.NewGuid().ToString("N");
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._childTracker.Add(Guid.Parse(expectedParentId), new SortedSet<Guid> {Guid.Parse(existingId)});

			checksum.LoadRow(expectedId, expectedParentId, expectedValue, "en", 1);

			checksum._childTracker.Should().ContainKey(Guid.Parse(expectedParentId));
			checksum._childTracker[Guid.Parse(expectedParentId)].Should().Contain(Guid.Parse(expectedId));
			checksum._childTracker[Guid.Parse(expectedParentId)].Should().Contain(Guid.Parse(existingId));
		}

		[Fact]
		public void LoadRow_ChildTrackerDoesNotContainId_AddsToLeafTracker()
		{
			string expectedId = Guid.NewGuid().ToString("N");
			string expectedParentId = Guid.NewGuid().ToString("N");
			string expectedValue = "Value";
			Checksum checksum = new Checksum();

			checksum.LoadRow(expectedId, expectedParentId, expectedValue, "en", 1);

			checksum._leafTracker.Should().Contain(expectedId);
		}

		[Fact]
		public void LoadRow_ChildTrackerContainsId_DoesNotAddToLeafTracker()
		{
			string expectedId = Guid.NewGuid().ToString("N");
			string expectedParentId = Guid.NewGuid().ToString("N");
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._childTracker.Add(Guid.Parse(expectedId), new SortedSet<Guid>());
			
			checksum.LoadRow(expectedId, expectedParentId, expectedValue, "en", 1);

			checksum._leafTracker.Should().NotContain(expectedId);
		}

		[Fact]
		public void LoadRow_LeafTrackerRemovesParentId()
		{
			string expectedId = Guid.NewGuid().ToString("N");
			string expectedParentId = Guid.NewGuid().ToString("N");
			string expectedValue = "Value";
			Checksum checksum = new Checksum();
			checksum._leafTracker.Add(Guid.Parse(expectedParentId));

			checksum.LoadRow(expectedId, expectedParentId, expectedValue, "en", 1);

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
			Guid key = Guid.NewGuid();
			short existingChecksum = 42;
			Checksum checksum = new Checksum();
			checksum._checksum.Add(key, existingChecksum);

			int actual = checksum.GetChecksum(key.ToString());

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
			Guid[] guids = plcs.Item1;
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

			checksum._childTracker.Should().BeEmpty();
			checksum._parentTracker.Should().BeEmpty();
			checksum._leafTracker.Should().BeEmpty();
		}

		#region Helper functions
		private Tuple<Guid[], Checksum> PreloadChecksum()
		{
			var root = Guid.NewGuid();

			var parentA = Guid.NewGuid();
			var childA1 = Guid.NewGuid();
			var childA2 = Guid.NewGuid();

			var parentB = Guid.NewGuid();
			var subParentBa = Guid.NewGuid();
			var childBa1 = Guid.NewGuid();
			var childB1 = Guid.NewGuid();

			Checksum checksum = new Checksum();
			checksum._childTracker.Add(root, new SortedSet<Guid> { parentA, parentB });
			checksum._childTracker.Add(parentA, new SortedSet<Guid> { childA1, childA2 });
			checksum._childTracker.Add(parentB, new SortedSet<Guid> { subParentBa, childB1 });
			checksum._childTracker.Add(subParentBa, new SortedSet<Guid> { childBa1 });
			checksum._parentTracker.Add(childA1, parentA);
			checksum._parentTracker.Add(childA2, parentA);
			checksum._parentTracker.Add(subParentBa, parentB);
			checksum._parentTracker.Add(childB1, parentB);
			checksum._parentTracker.Add(childBa1, subParentBa);
			checksum._parentTracker.Add(parentA, root);
			checksum._parentTracker.Add(parentB, root);
			foreach (var guid in new[] { childA1, childA2, childB1, childBa1 })
				checksum._leafTracker.Add(guid);


			return new Tuple<Guid[], Checksum>(new[] {parentA, childA1, childA2, parentB, subParentBa, childBa1, childB1}, checksum);
		}
		#endregion
	}
}
