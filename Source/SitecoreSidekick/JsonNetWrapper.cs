using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SitecoreSidekick
{
	public static class JsonNetWrapper
	{
		private static MethodInfo Deserialize;
		private static MethodInfo Serialize;
		static JsonNetWrapper()
		{
			Assembly a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Newtonsoft.Json");
			Type t = a.GetType("Newtonsoft.Json.JsonConvert");
			var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == "DeserializeObject");
			Deserialize = methods.First(x => x.ContainsGenericParameters && x.GetParameters().Length == 1);
			methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.Name == "SerializeObject");
			Serialize = methods.First(x => x.GetParameters().Length == 1);
		}

		public static T DeserializeObject<T>(string str)
		{
			MethodInfo generic = Deserialize.MakeGenericMethod(new[] { typeof(T) });
			return (T)generic.Invoke(null, new object[] { str });
		}

		public static object DeserializeObject(string str, Type t)
		{
			MethodInfo generic = Deserialize.MakeGenericMethod(new[] { t });
			return generic.Invoke(null, new object[]{str});
		}

		public static string SerializeObject(object o)
		{
			return Serialize.Invoke(null, new[] { o }).ToString();
		}

	}
}
