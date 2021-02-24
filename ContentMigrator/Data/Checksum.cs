using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;

namespace ScsContentMigrator.Data
{
	public class Checksum
	{
		internal readonly Dictionary<Guid, SortedSet<short>> _checksumTracker = new Dictionary<Guid, SortedSet<short>>();
		internal readonly Dictionary<Guid, Guid> _revTracker = new Dictionary<Guid, Guid>();
		internal readonly Dictionary<Guid, List<Guid>> _childTracker = new Dictionary<Guid, List<Guid>>();
		internal readonly Dictionary<Guid, Guid> _parentTracker = new Dictionary<Guid, Guid>();
		internal readonly HashSet<Guid> _leafTracker = new HashSet<Guid>();
		internal readonly Dictionary<Guid, short> _checksum = new Dictionary<Guid, short>();
		//t.ID, t.Name, t.TemplateID, t.MasterID, t.ParentID, v.Value
		public void LoadRow(string id, string parentId, string value)
		{
			var idGuid = Guid.Parse(id);
			var parentGuid = Guid.Parse(parentId);
			var valueGuid = Guid.Parse(value);
			if (_parentTracker.ContainsKey(idGuid)) return;
			_parentTracker[idGuid] = parentGuid;
			if (!_childTracker.ContainsKey(parentGuid))
			{
				_childTracker[parentGuid] = new List<Guid>();
			}
			_childTracker[parentGuid].Add(idGuid);
			if (!_checksumTracker.ContainsKey(idGuid))
			{
				_checksumTracker[idGuid] = new SortedSet<short>();
			}
			_revTracker[idGuid] = valueGuid;
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
			Queue<Guid> processing = new Queue<Guid>(_leafTracker);
			while (processing.Any())
			{
				Guid id = processing.Dequeue();
				if (_checksumTracker[id].Count == 0)
				{
					if (_checksumTracker.ContainsKey(_parentTracker[id]))
					{
						_checksumTracker[_parentTracker[id]].Add(GetHashCode16(_revTracker[id].ToString("N")+id));
					}
				}
				else
				{
					_checksum[id] = GetHashCode16(string.Join("", _checksumTracker[id]));
					if (_checksumTracker.ContainsKey(_parentTracker[id]))
					{
						_checksumTracker[_parentTracker[id]].Add(_checksum[id]);
					}
				}
				if (_checksumTracker.ContainsKey(_parentTracker[id]))
				{
					processing.Enqueue(_parentTracker[id]);
				}
			}
			_checksumTracker.Clear();
			_childTracker.Clear();
			_parentTracker.Clear();
			_leafTracker.Clear();
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
	}
}
