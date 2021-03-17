using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.DependencyInjection;
using Sidekick.Core.Services;
using Sidekick.Core.Services.Interface;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.AuditLog
{
	public class Bootstrap
	{
		private static readonly object BootstrapLock = new object();
		/// <summary>
		/// Sets the container to use to an existing container
		/// </summary>
		/// <param name="container">The container to use</param>
		public static void SetContainer(Container container)
		{
			_container = container;
		}

		private static Container _container;
		public static Container Container
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

		private static Container InitializeContainer()
		{
			Container container = Sidekick.Core.Bootstrap.Container;

			// Register components here
			container.Register<ISitecoreDataAccessService, SitecoreDataAccessService>();

			return container;
		}
	}
}
