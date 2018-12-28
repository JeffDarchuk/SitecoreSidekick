using System;
using System.Collections;
using System.Collections.Generic;

namespace ScsContentMigrator.DataBlaster.Sitecore.DataBlaster.Util
{
	/// <summary>
	/// Simple generic implementation of a chain of processors.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Chain<T> : ICollection<T>
	{
		private readonly IList<T> _processors = new List<T>();

		public TIo Execute<TIo>(TIo result, Func<T, TIo, TIo> processorFunction, bool breakOnDefault = false)
		{
			foreach (var processor in _processors)
			{
				result = processorFunction(processor, result);

				if (breakOnDefault && Equals(result, default(TIo)))
					return result;
			}
			return result;
		}

		public TResult Execute<TResult>(Func<T, TResult> processorFunction, bool breakOnDefault = false)
		{
			return Execute(default(TResult), (p, r) => processorFunction(p), breakOnDefault: breakOnDefault);
		}

		public void Execute(Action<T> processorFunction)
		{
			foreach (var processor in _processors)
			{
				processorFunction(processor);
			}
		}

		#region Modifiers

		public Chain<T> Add<TProcessor>()
			where TProcessor : T, new()
		{
			_processors.Add(new TProcessor());
			return this;
		}

		public void InsertBefore<TReference>(T newProcessor, bool matchExactType = true)
			where TReference : T
		{
			var index = IndexOf(typeof(TReference), matchExactType);
			if (index == null) throw new ArgumentException($"Unable to find a proccessor of type '{typeof(TReference).Name}'.");

			_processors.Insert(index.Value, newProcessor);
		}

		public void InsertBefore<TReference, TNew>(bool matchExactType = true)
			where TReference : T
			where TNew : T, new()
		{
			InsertBefore<TReference>(new TNew(), matchExactType: matchExactType);
		}

		public void InsertAfter<TReference>(T newProcessor, bool matchExactType = true)
			where TReference : T
		{
			var index = IndexOf(typeof(TReference), matchExactType);
			if (index == null) throw new ArgumentException($"Unable to find a proccessor of type '{typeof(TReference).Name}'.");

			if (index.Value == Count - 1)
				_processors.Add(newProcessor);
			else
				_processors.Insert(index.Value + 1, newProcessor);
		}

		public void InsertAfter<TReference, TNew>(bool matchExactType = true)
			where TReference : T
			where TNew : T, new()
		{
			InsertAfter<TReference>(new TNew(), matchExactType: matchExactType);
		}

		public bool Replace<TSource>(T replacement, bool matchExactType = true)
			where TSource : T
		{
			var index = IndexOf(typeof(TSource), matchExactType);
			if (index == null) return false;

			_processors[index.Value] = replacement;
			return true;
		}

		public bool Replace<TSource, TTarget>(bool matchExactType = true)
			where TSource : T
			where TTarget : T, new()
		{
			return Replace<TSource>(new TTarget(), matchExactType: matchExactType);
		}

		public void Remove<TProcessor>(bool matchExactType = true)
			where TProcessor : T
		{
			int? index;
			while((index = IndexOf(typeof(TProcessor), matchExactType)) != null)
			{
				_processors.RemoveAt(index.Value);
			}
		}

		private int? IndexOf(Type processorType, bool matchExactType)
		{
			for (var i = 0; i < _processors.Count; i++)
			{
				var processor = _processors[i];
				if (matchExactType && processor.GetType() == processorType || processorType.IsInstanceOfType(processor))
				{
					return i;
				}
			}
			return null;
		}

		#endregion

		#region ICollection members

		public int Count => _processors.Count;

		public bool IsReadOnly => _processors.IsReadOnly;
		
		public void Add(T item)
		{
			_processors.Add(item);
		}

		public void Clear()
		{
			_processors.Clear();
		}

		public bool Contains(T item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			return _processors.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_processors.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			return _processors.Remove(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _processors.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_processors).GetEnumerator();
		}

		#endregion
	}
}