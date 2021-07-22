using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;

namespace Sidekick.ContentMigrator.Data
{
	public class Checksum
	{
		internal readonly Dictionary<Guid, SortedSet<short>> _revTracker = new Dictionary<Guid, SortedSet<short>>();
		internal readonly Dictionary<Guid, SortedSet<Guid>> _childTracker = new Dictionary<Guid, SortedSet<Guid>>();
		internal readonly Dictionary<Guid, Guid> _parentTracker = new Dictionary<Guid, Guid>();
		internal readonly HashSet<Guid> _leafTracker = new HashSet<Guid>();
		internal readonly Dictionary<Guid, short> _checksum = new Dictionary<Guid, short>();
		internal DateTime StartTime;
		internal DateTime FinishTime;
		//t.ID, t.Name, t.TemplateID, t.MasterID, t.ParentID, v.Value
		public void LoadRow(string id, string parentId, string value, string language, int? version)
		{
			var idGuid = Guid.Parse(id);
			var parentGuid = Guid.Parse(parentId);
			var valueGuid = Guid.Parse(value);
			if (_parentTracker.ContainsKey(idGuid)) return;
			_parentTracker[idGuid] = parentGuid;
			if (!_childTracker.ContainsKey(parentGuid))
			{
				_childTracker[parentGuid] = new SortedSet<Guid>();
			}
			_childTracker[parentGuid].Add(idGuid);
			if (!_revTracker.ContainsKey(idGuid))
			{
				_revTracker[idGuid] = new SortedSet<short>();
			}
			_revTracker[idGuid].Add(GetHashCode16(value+language+version));
			if (!_childTracker.ContainsKey(idGuid))
				_leafTracker.Add(idGuid);
			_leafTracker.Remove(parentGuid);
		}

		public int GetChecksum(string id)
		{
			
			if (!Guid.TryParse(id, out Guid result)) return -1;
			if (_checksum.ContainsKey(result))
				return _checksum[result];
			return -1;
		}

		public void Generate()
		{
			StartTime = DateTime.Now;
			Queue<Guid> processing = new Queue<Guid>(_leafTracker);
			while (processing.Any())
			{
				Guid id = processing.Dequeue();
				
				if (_childTracker.ContainsKey(id))
				{
					if (!_childTracker[id].All(x => _checksum.ContainsKey(x)))
					{
						processing.Enqueue(id);
						continue;
					}
					_checksum[id] = GetHashCode16(string.Join("", _childTracker[id].Select(x => _checksum[x])) + string.Join("", _revTracker[id]));
				}
				else
				{
					_checksum[id] = GetHashCode16(string.Join("", _revTracker[id]));
				}

				if (_revTracker.ContainsKey(_parentTracker[id]))
				{
					processing.Enqueue(_parentTracker[id]);
				}
			}
			_childTracker.Clear();
			_parentTracker.Clear();
			_leafTracker.Clear();
			FinishTime = DateTime.Now;
			Log.Info($"[Sidekick] checksum generated in {FinishTime.Subtract(StartTime).TotalSeconds}", this);
		}
		public short GetHashCode16(string s)
		{
			var chars = s.ToCharArray();
			var lastCharInd = chars.Length - 1;
			var num1 = 0x15051505;
			var num2 = num1;
			var ind = 0;
			while (ind <= lastCharInd)
			{
				var ch = chars[ind];
				var nextCh = ++ind > lastCharInd ? '\0' : chars[ind];
				num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ (nextCh << 16 | ch);
				if (++ind > lastCharInd) break;
				ch = chars[ind];
				nextCh = ++ind > lastCharInd ? '\0' : chars[ind++];
				num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ (nextCh << 16 | ch);
			}
			return (short)((num1 + num2 * 0x5d588b65) & 0xFFFF);
		}
		private string GenerateRef(SortedSet<short> refIds)
		{
			return refIds.Aggregate(new StringBuilder(), (a, b) => a.Append(b)).ToString();
		}
	}
}
