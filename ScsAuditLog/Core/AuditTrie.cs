using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sidekick.AuditLog.Core
{
	public class AuditTrie<T>
	{
		private readonly ConcurrentDictionary<char, AuditTrie<T>> _level = new ConcurrentDictionary<char, AuditTrie<T>>();
		private T _value;

		public T this[string text]
		{
			get => Query(text, 0)._value;
			set => Query(text, 0)._value = value;
		} 

		public T Query(string text)
		{
			return Query(text, 0)._value;
		}

		private AuditTrie<T> Query(string text, int index)
		{
			if (text.Length == index)
				return this;
			char levelIndex = text[index];
			if (!_level.ContainsKey(levelIndex))
				_level[levelIndex] = new AuditTrie<T>();
			return _level[levelIndex].Query(text, index + 1);
		}

		public void Set(string text, T value)
		{
			Query(text, 0)._value = value;
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
			if (_level.ContainsKey(levelIndex))
				return _level[levelIndex].Autocomplete(text, index + 1, getCount, maxResults);
			return new Dictionary<string, int>();
		}

		private IEnumerable<KeyValuePair<int, string>> FindKeys(string text, Func<T, int> getCount)
		{
			Queue<Tuple<string, AuditTrie<T>>> q = new Queue<Tuple<string, AuditTrie<T>>>();
			q.Enqueue(new Tuple<string, AuditTrie<T>>(text, this));
			while (q.Any())
			{
				var cur = q.Dequeue();
				int count = getCount(cur.Item2._value);
				if (count > 0)
					yield return new KeyValuePair<int, string>(count, cur.Item1);
				foreach (char key in cur.Item2._level.Keys)
				{
					q.Enqueue(new Tuple<string, AuditTrie<T>>(cur.Item1+key, cur.Item2._level[key]));
				}
			}
		}
	}
}
