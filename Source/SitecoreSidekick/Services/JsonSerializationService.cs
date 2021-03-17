using Sidekick.Core.Services.Interface;
using System;
using System.Linq;
using System.Reflection;

namespace Sidekick.Core.Services
{
	public class JsonSerializationService : IJsonSerializationService
	{
		private readonly MethodInfo _deserialize;
		private readonly MethodInfo _serialize;
		public JsonSerializationService()
		{
			Assembly a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Newtonsoft.Json");
			Type t = a.GetType("Newtonsoft.Json.JsonConvert");
			var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == "DeserializeObject");
			_deserialize = methods.First(x => x.ContainsGenericParameters && x.GetParameters().Length == 1);
			methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == "SerializeObject");
			_serialize = methods.First(x => x.GetParameters().Length == 1);
		}

		public T DeserializeObject<T>(string str)
		{
			MethodInfo generic = _deserialize.MakeGenericMethod(new[] { typeof(T) });
			return (T)generic.Invoke(null, new object[] { str });
		}

		public object DeserializeObject(string str, Type t)
		{
			MethodInfo generic = _deserialize.MakeGenericMethod(new[] { t });
			return generic.Invoke(null, new object[] { str });
		}

		public string SerializeObject(object o)
		{
			return _serialize.Invoke(null, new[] { o }).ToString();
		}
	}
}
