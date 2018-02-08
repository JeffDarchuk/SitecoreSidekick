using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Services
{
	public class AssemblyScannerService : IAssemblyScannerService
	{
		ConcurrentDictionary<Type, IEnumerable<object>> _cache = new ConcurrentDictionary<Type, IEnumerable<object>>();
		private IEnumerable<Type> GetTypes(Assembly a, Type type)
		{
			IEnumerable<Type> types = null;
			try
			{
				types = a.GetTypes().Where(t => type.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
			}
			catch (ReflectionTypeLoadException e)
			{
				types = e.Types.Where(t => t != null && type.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
			}

			if (types == null) yield break;

			foreach (var t in types)
			{
				yield return t;
			}
		}

		public IEnumerable<T> ScanForImplementsInterface<T>()
		{
			return AppDomain.CurrentDomain
				.GetAssemblies()
				.Where(x => !Constants.BinaryBlacklist.Contains(x.GetName().Name))
				.SelectMany(x => GetTypes(x, typeof(T)))
				.Select(t =>(T)Activator.CreateInstance(t));
		}
	}
}
