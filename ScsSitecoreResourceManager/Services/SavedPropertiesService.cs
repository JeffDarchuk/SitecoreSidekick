using Sidekick.Core.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.SitecoreResourceManager.Services
{
	public class SavedPropertiesService : ISavedPropertiesService
	{
		private IScsRegistrationService _registration;
		private IJsonSerializationService _json;
		private string _properties;
		private object _locker = new object();
		public SavedPropertiesService()
		{
			_json = Bootstrap.Container.Resolve<IJsonSerializationService>();
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_properties = _registration.GetScsRegistration<SitecoreResourceManagerRegistration>().GetDataDirectory();
		}
		public string GetPropertiesFilePath(string template)
		{
			string ret = _properties + $"\\{template}.json";
			lock (_locker)
			{
				if (!File.Exists(ret))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(ret));
					File.WriteAllText(ret, _json.SerializeObject(new Dictionary<string, string>()));
				}
			}
			return ret;
		}
		public string this[string key, string template]
		{
			get
			{
				Dictionary<string, string> tmp;
				lock (_locker)
				{
					tmp = _json.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(GetPropertiesFilePath(template)));
				}
				if (tmp.ContainsKey(key))
				{
					return tmp[key];
				}
				return null;
			}
			set
			{
				Dictionary<string, string> tmp;
				lock (_locker)
				{
					tmp = _json.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(GetPropertiesFilePath(template)));
					tmp[key] = value;
					File.WriteAllText(GetPropertiesFilePath(template), _json.SerializeObject(tmp));
				}
			}
		}
	}
}
