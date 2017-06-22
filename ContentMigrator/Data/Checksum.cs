using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data.Managers;

namespace ScsContentMigrator.Data
{
	public class Checksum
	{
		public Dictionary<string, int> Storage { get; } = new Dictionary<string, int>();
		public Dictionary<string, SortedSet<string>> ChildrenIndex { get; } = new Dictionary<string, SortedSet<string>>();

		public int GetChecksum(string id)
		{
			int ret;
			if (Storage.TryGetValue(new Sitecore.Data.ID(id).Guid.ToString("D"), out ret))
			{
				return ret;
			}
			return default(int);
		}

		public void Register(string itemId, string value, string parentId)
		{
			if (!Storage.ContainsKey(itemId))
			{
				Storage[itemId] = 0;
			}
			int newData = default(int);
			if (Storage.ContainsKey(itemId))
			{
				if (int.MaxValue - Storage[itemId] - value.GetHashCode() < 0 )
					Storage[itemId] = Storage[itemId] - int.MaxValue 
				Storage[itemId].Add(value.GetHashCode());
			}
			else if (ChildrenIndex.ContainsKey(itemId))
			{
				StringBuilder sb = new StringBuilder(value);
				foreach (string sort in ChildrenIndex[itemId])
				{
					
					sb.Append(GetChecksum(sort));
				}
				Storage[itemId].Add(sb.ToString().GetHashCode());
				Storage[itemId].Add(value.GetHashCode());
			}
			else
			{
				newData = value.GetHashCode();
				Storage[itemId].Add(newData);
			}
			if (newData != default(int))
			{
				if (!ChildrenIndex.ContainsKey(parentId))
				{
					ChildrenIndex[parentId] = new SortedSet<string>();
				}
				ChildrenIndex[parentId].Add(itemId);
			}
		}
	}
}
