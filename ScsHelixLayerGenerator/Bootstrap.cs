
using ScsSitecoreResourceManager.Services;
using SitecoreSidekick.Services;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsSitecoreResourceManager
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
