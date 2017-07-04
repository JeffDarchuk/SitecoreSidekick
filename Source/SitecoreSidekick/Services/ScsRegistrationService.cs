using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitecoreSidekick.Core;
using SitecoreSidekick.Services.Interface;

namespace SitecoreSidekick.Services
{
	public class ScsRegistrationService : IScsRegistrationService
	{
		private Dictionary<Type, IScsRegistration> _registration = new Dictionary<Type, IScsRegistration>();
		private HashSet<string> _processed = new HashSet<string>();
		private readonly StringBuilder _js = new StringBuilder();
		private readonly StringBuilder _css = new StringBuilder();
		public void RegisterSidekick(IScsRegistration sidekick)
		{
			_registration.Add(sidekick.GetType(), sidekick);
			if (!_processed.Contains(sidekick.Name))
			{
				_js.Append(sidekick.CompileEmbeddedResource("js"));
				_css.Append(sidekick.CompileEmbeddedResource("css"));
			}
			_processed.Add(sidekick.Name);
		}

		public void RegisterSidekick(Type t, IScsRegistration sidekick)
		{
			_registration.Add(t, sidekick);
			if (!_processed.Contains(sidekick.Name))
			{
				_js.Append(sidekick.CompileEmbeddedResource("js"));
				_css.Append(sidekick.CompileEmbeddedResource("css"));
			}
			_processed.Add(sidekick.Name);
		}

		public T GetScsRegistration<T>() where T : class, IScsRegistration
		{
			IScsRegistration ret;
			_registration.TryGetValue(typeof(T), out ret);
			return ret as T;
		}

		public IScsRegistration GetScsRegistration(Type t)
		{
			IScsRegistration ret;
			_registration.TryGetValue(t, out ret);
			return ret;
		}

		public IEnumerable<IScsRegistration> GetAllSidekicks()
		{
			return new HashSet<IScsRegistration>(_registration.Values);
		}

		public string Js => _js.ToString();
		public string Css => _css.ToString();
	}
}
