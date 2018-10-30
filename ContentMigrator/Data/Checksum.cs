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
		internal readonly Dictionary<string, SortedSet<string>> _checksumTracker = new Dictionary<string, SortedSet<string>>();
		internal readonly Dictionary<string, string> _revTracker = new Dictionary<string, string>();
		internal readonly Dictionary<string, List<string>> _childTracker = new Dictionary<string, List<string>>();
		internal readonly Dictionary<string,string> _parentTracker = new Dictionary<string, string>();
		internal readonly HashSet<string> _leafTracker = new HashSet<string>();
		internal readonly Dictionary<string, int> _checksum = new Dictionary<string, int>();
		//t.ID, t.Name, t.TemplateID, t.MasterID, t.ParentID, v.Value
		public void LoadRow(string id, string parentId, string value)
		{
			if (_parentTracker.ContainsKey(id)) return;
			_parentTracker[id] = parentId;
			if (!_childTracker.ContainsKey(parentId))
			{
				_childTracker[parentId] = new List<string>();
			}
			_childTracker[parentId].Add(id);
			if (!_checksumTracker.ContainsKey(id))
			{
				_checksumTracker[id] = new SortedSet<string>();
			}
			_revTracker[id] = value;
			if (!_childTracker.ContainsKey(id))
				_leafTracker.Add(id);
			_leafTracker.Remove(parentId);
		}

		public int GetChecksum(string id)
		{
			if (!Guid.TryParse(id, out Guid result)) return -1;
			string key = result.ToString();
			if (_checksum.ContainsKey(key))
				return _checksum[key];
			return -1;
		}

		public void Generate()
		{
			Queue<string> processing = new Queue<string>(_leafTracker);
			while (processing.Any())
			{
				string id = processing.Dequeue();
				if (_checksumTracker[id].Count == 0)
				{
					if (_checksumTracker.ContainsKey(_parentTracker[id]))
					{
						_checksumTracker[_parentTracker[id]].Add(GetHashCode32(_revTracker[id]+id).ToString());
					}
				}
				else
				{
					_checksum[id] = GetHashCode32(string.Join("", _checksumTracker[id]));
					if (_checksumTracker.ContainsKey(_parentTracker[id]))
					{
						_checksumTracker[_parentTracker[id]].Add(_checksum[id].ToString());
					}
				}
				if (_checksumTracker.ContainsKey(_parentTracker[id]) && _checksumTracker[_parentTracker[id]].Count >= _childTracker[_parentTracker[id]].Count)
				{
					processing.Enqueue(_parentTracker[id]);
				}
			}
			_checksumTracker.Clear();
			_childTracker.Clear();
			_parentTracker.Clear();
			_leafTracker.Clear();
		}
		public int GetHashCode32(string s)
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
			return num1 + num2 * 0x5d588b65;
		}
	}
}
