using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using SitecoreSidekick.Services;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace ScsContentMigrator
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

			// Register components here			
			container.Register<IScsRegistrationService, ScsRegistrationService>();
			container.Register<IRemoteContentService, RemoteContentService>();
			container.Register<ISitecoreAccessService, SitecoreAccessService>();
			container.Register<IContentMigrationManagerService, ContentMigrationManagerService>();			
			return container;
		}
	}
}
