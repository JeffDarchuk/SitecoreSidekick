using SitecoreSidekick.Services;
using SitecoreSidekick.Services.Implementation;
using SitecoreSidekick.Shared.IoC;

namespace SitecoreSidekick
{
	public class Bootstrap
	{
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
				if (_container != null) return _container;
				_container = InitializeContainer();
				return _container;
			}
		}

		private static Container InitializeContainer()
		{
			Container container = new Container();

			// Register components here
			container.Register<IAuthenticationService, AuthenticationService>();
			container.Register<IJsonSerializationService, JsonSerializationService>();
			container.Register<IScsRegistrationService, ScsRegistrationService>();

			return container;
		}
	}
}
