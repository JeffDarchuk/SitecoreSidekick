
using Sidekick.SitecoreResourceManager.Services;
using Sidekick.Core.Services;
using Sidekick.Core.Services.Interface;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.SitecoreResourceManager
{
	public class Bootstrap
	{
		internal static readonly object BootstrapLock = new object();

		private static IContainer _container;
		public static IContainer Container
		{
			get
			{
				lock (BootstrapLock)
				{
					if (_container != null) return _container;
					_container = InitializeContainer();
					return _container;
				}
			}
		}

		private static IContainer InitializeContainer()
		{
			Container container = new Container();
			container.Register<IJsonSerializationService, JsonSerializationService>();
			container.Register<IScsRegistrationService, ScsRegistrationService>();
			container.Register<IAssemblyScannerService, AssemblyScannerService>();
			container.Register<ISavedPropertiesService, SavedPropertiesService>();
			container.Register<ISitecoreDataAccessService, SitecoreDataAccessService>();
			return container;
		}
	}
}
