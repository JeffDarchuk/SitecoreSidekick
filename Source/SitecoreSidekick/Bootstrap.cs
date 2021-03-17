using System.Linq;
using Sidekick.Core.Services;
using Sidekick.Core.Services.Interface;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.Core
{
	public class Bootstrap
	{
		internal static readonly object BootstrapLock = new object();
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
			Container container = new Container();

			// Register components here
			container.Register<IAuthenticationService, AuthenticationService>();
			container.Register<IAuthorizationService, AuthorizationService>();
			container.Register<IJsonSerializationService, JsonSerializationService>();
			container.Register<IScsRegistrationService, ScsRegistrationService>();
			container.Register<IMainfestResourceStreamService, MainfestResourceStreamService>();
			container.RegisterFactory<IHttpClientService>(args => args.Any() ? new HttpClientService(args[0].ToString()) : new HttpClientService());
			container.Register<ISitecoreDataAccessService, SitecoreDataAccessService>();
			container.Register<IIconService, IconService>();

			return container;
		}
	}
}
