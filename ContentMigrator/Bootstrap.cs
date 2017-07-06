using Rainbow.Storage;
using Rainbow.Storage.Sc;
using Rainbow.Storage.Sc.Deserialization;
using ScsContentMigrator.CMRainbow;
using ScsContentMigrator.Core;
using ScsContentMigrator.Core.Interface;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using SitecoreSidekick.Shared.IoC;
using System.Linq;
using MicroCHAP;

namespace ScsContentMigrator
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
			Container container = SitecoreSidekick.Bootstrap.Container;

			// Register components here
			container.Register<IContentMigrationManagerService, ContentMigrationManagerService>();
			container.Register<IRemoteContentService, RemoteContentService>();
			container.Register<ISitecoreAccessService, SitecoreAccessService>();
			container.RegisterFactory<IDataStore>(args =>
			{
				IDefaultDeserializerLogger logger = (IDefaultDeserializerLogger)args.FirstOrDefault(a => a is IDefaultDeserializerLogger);
				var deserializer = new DefaultDeserializer(logger, new DefaultFieldFilter());
				return new SitecoreDataStore(deserializer);
			});
			container.RegisterFactory<IContentItemPuller>(args => new ContentItemPuller());
			container.RegisterFactory<IContentItemInstaller>(args => new ContentItemInstaller());
			container.RegisterFactory<ISignatureService>(args=> new SignatureService((string)args[0]));

			return container;
		}
	}
}
