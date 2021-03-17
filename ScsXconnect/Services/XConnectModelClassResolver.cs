using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sitecore.XConnect.Schema;

namespace Sidekick.XConnect.Services
{
	public class XConnectModelClassResolver : IModelClassResolver
	{
		private Dictionary<string, Tuple<XdbModel, Type>> _facetModels = new Dictionary<string, Tuple<XdbModel, Type>>();
		private readonly object locker = new object();
		public IEnumerable<XdbModel> GetAllModels()
		{
			lock (locker)
			{
				try
				{
					if (_facetModels.Any())
						return _facetModels.Select(x => x.Value.Item1);
					_facetModels = AppDomain.CurrentDomain
						.GetAssemblies()
						.Where(x => !Constants.BinaryBlacklist.Contains(x.GetName().Name))
						.SelectMany(GetFacetTypes)
						.Select(t => new Tuple<XdbModel, Type>((XdbModel) t.GetValue(null), t.DeclaringType))
						.ToDictionary(k => k.Item1.Name, v => v);
					return _facetModels.Select(x => x.Value.Item1);
				}
				catch (ArgumentException e)
				{
					throw new ArgumentException("Unable to build the XConnect model dictionary, likely because multiple  models share a key", e);
				}
			}
		}

		public XdbModel GetModelByName(string name)
		{
			return _facetModels[name].Item1;
		}

		public Type GetModelType(string name)
		{
			return _facetModels[name].Item2;
		}

		private IEnumerable<PropertyInfo> GetFacetTypes(Assembly a)
		{
			IEnumerable<PropertyInfo> types = null;
			try
			{
				types = a.GetTypes().SelectMany(x => x.GetProperties(BindingFlags.Static | BindingFlags.Public).Where(r =>
				{
					try
					{
						return r.PropertyType == typeof(XdbModel);
					}catch(Exception) { }

					return false;
				}));
			}
			catch (ReflectionTypeLoadException e)
			{
				types = e.Types.Where(x => x != null).SelectMany(x => x.GetProperties(BindingFlags.Static|BindingFlags.Public).Where(r =>
				{
					try
					{
						return r.PropertyType == typeof(XdbModel);
					}
					catch (Exception) { }

					return false;
				}));
			}

			return types;
		}
	}
}
