using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Pipelines.GetVisitorEmailAddress;

namespace ScsAuditLog.Core
{
	public class AuditTrie<T>
	{
		private ConcurrentDictionary<char, AuditTrie<T>> Level = new ConcurrentDictionary<char, AuditTrie<T>>();
		private T Value = default(T);
		private AuditTrie<T> _parent;

		public AuditTrie(AuditTrie<T> parent)
		{
			this._parent = parent;
		}

		public T this[string text]
		{
			get { return Query(text, 0).Value; }
			set { Query(text, 0).Value = value; }
		} 

		public T Query(string text)
		{
			return Query(text, 0).Value;
		}

		private AuditTrie<T> Query(string text, int index)
		{
			if (text.Length == index)
				return this;
			char levelIndex = text[index];
			if (!Level.ContainsKey(levelIndex))
				Level[levelIndex] = new AuditTrie<T>(this);
			return Level[levelIndex].Query(text, index + 1);
		}

		public void Set(string text, T value)
		{
			Query(text, 0).Value = value;
		}
		public Dictionary<string, int> Autocomplete(string text, Func<T, int> getCount, int maxResults)
		{
			return Autocomplete(text, 0, getCount, maxResults);
		}

		private Dictionary<string, int> Autocomplete(string text, int index, Func<T, int> getCount, int maxResults)
		{
			if (text.Length == index)
			{
				return FindKeys(text, getCount).Take(maxResults).ToDictionary(x => x.Value, x => x.Key);
			}
			char levelIndex = text[index];
			if (Level.ContainsKey(levelIndex))
				return Level[levelIndex].Autocomplete(text, index + 1, getCount, maxResults);
			return new Dictionary<string, int>();
		}

		private IEnumerable<KeyValuePair<int, string>> FindKeys(string text, Func<T, int> getCount)
		{
			Queue<Tuple<string, AuditTrie<T>>> q = new Queue<Tuple<string, AuditTrie<T>>>();
			q.Enqueue(new Tuple<string, AuditTrie<T>>(text, this));
			while (q.Any())
			{
				var cur = q.Dequeue();
				int count = getCount(cur.Item2.Value);
				if (count > 0)
					yield return new KeyValuePair<int, string>(count, cur.Item1);
				foreach (char key in cur.Item2.Level.Keys)
				{
					q.Enqueue(new Tuple<string, AuditTrie<T>>(cur.Item1+key, cur.Item2.Level[key]));
				}
			}
		}
	}
}
