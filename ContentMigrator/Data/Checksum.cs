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
		private readonly Dictionary<string, SortedSet<string>> _checksumTracker = new Dictionary<string, SortedSet<string>>();
		private readonly Dictionary<string, List<string>> _childTracker = new Dictionary<string, List<string>>();
		private readonly Dictionary<string,string> _parentTracker = new Dictionary<string, string>();
		private readonly HashSet<string> _leafTracker = new HashSet<string>();
		private readonly Dictionary<string, int> _checksum = new Dictionary<string, int>();
		//t.ID, t.Name, t.TemplateID, t.MasterID, t.ParentID, v.Value
		public void LoadRow(string id, string parentId, string value)
		{
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
			_checksumTracker[id].Add(value);
			if (!_childTracker.ContainsKey(id))
				_leafTracker.Add(id);
			_leafTracker.Remove(parentId);
		}

		public int GetChecksum(string id)
		{
			string key = Guid.Parse(id).ToString();
			if (_checksum.ContainsKey(key))
				return _checksum[key];
			return -1;
		}

		public void Generate()
		{
			Queue<string> processing = new Queue<string>(_leafTracker);
			HashSet<string> tracker = new HashSet<string>();
			while (processing.Any())
			{
				string id = processing.Dequeue();
				_checksum[id] = string.Join("", _checksumTracker[id]).GetHashCode();
				if (_checksumTracker.ContainsKey(_parentTracker[id]))
				{
					_checksumTracker[_parentTracker[id]].Add(_checksum[id].ToString());
				}
				if (!tracker.Contains(_parentTracker[id]) && _checksumTracker.ContainsKey(_parentTracker[id]))
				{
					processing.Enqueue(_parentTracker[id]);
					tracker.Add(_parentTracker[id]);
				}
			}
			_checksumTracker.Clear();
			_childTracker.Clear();
			_parentTracker.Clear();
			_leafTracker.Clear();
		}
	}
}
