using Sidekick.XConnect.Services;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.XConnect
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

			container.Register<IModelClassResolver>(new XConnectModelClassResolver());

			return container;
		}
	}
}
