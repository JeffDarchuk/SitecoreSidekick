using System;

namespace Sidekick.Core.Services.Interface
{
	public interface IJsonSerializationService
	{
		T DeserializeObject<T>(string str);

		object DeserializeObject(string str, Type t);

		string SerializeObject(object o);
	}
}
