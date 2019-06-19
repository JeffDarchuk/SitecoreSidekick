using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Rainbow.Storage;
using ScsContentMigrator.Services.Interface;

namespace ScsContentMigrator.Services
{
	public class DatastoreSaver : IDatastoreSaver
	{
		private static MethodInfo _method = null;
		private static object locker = new object();

		public void Save(IDataStore store, IItemData data)
		{
			if (_method == null)
			{
				lock (locker)
				{
					if (_method == null)
					{
						_method = store.GetType().GetMethod("Save");
					}
				}
			}

			if (_method.GetParameters().Length == 1)
			{
				_method.Invoke(store, new[] {data});
			}
			else
			{
				_method.Invoke(store, new[] {data, null});
			}
		}
	}
}
