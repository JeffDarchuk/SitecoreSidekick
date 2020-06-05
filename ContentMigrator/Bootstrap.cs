using System;
using System.Collections.Generic;
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
using System.Reflection;
using MicroCHAP;
using Rainbow.Diff;
using ScsContentMigrator.CMRainbow.Interface;
using SitecoreSidekick.Services;
using SitecoreSidekick.Services.Interface;

namespace ScsContentMigrator
{
	public class Bootstrap
	{
		private static readonly object BootstrapLock = new object();

		/// <summary>
		/// Sets the container to use to an existing container
		/// </summary>
		/// <param name="container">The container to use</param>
		static Bootstrap()
		{

		}
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
			container.RegisterFactory<IRemoteContentService>(args =>
			{
				var registration = container.Resolve<IScsRegistrationService>();
				return new RemoteContentService(registration);
			});
			container.Register<ISitecoreDataAccessService, SitecoreDataAccessService>();			
			container.Register<ILoggingService, LoggingService>();
			container.RegisterFactory<IDataStore>(args =>
			{
				IDefaultDeserializerLogger logger = (IDefaultDeserializerLogger)args.FirstOrDefault(x => x is IDefaultDeserializerLogger);
				Assembly a = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "Rainbow.Storage.Sc");
				Type t = a?.GetType("Rainbow.Storage.Sc.Deserialization.DefaultDeserializer");
				foreach (var constructor in t?.GetConstructors().Where(x => x.GetParameters().Length == 2) ?? Enumerable.Empty<ConstructorInfo>())
				{
					return new SitecoreDataStore((DefaultDeserializer)constructor.Invoke(new object[] {logger, new DefaultFieldFilter()}));
				}
				foreach (var constructor in t?.GetConstructors().Where(x => x.GetParameters().Length == 3) ?? Enumerable.Empty<ConstructorInfo>())
				{
					return new SitecoreDataStore((DefaultDeserializer)constructor.Invoke(new object[] { false, logger, new DefaultFieldFilter() }));
				}

				return null;
			});
			container.Register<IDatastoreSaver, DatastoreSaver>();
			container.RegisterFactory<IContentItemPuller>(args => new ContentItemPuller(100));
			container.RegisterFactory<IContentItemInstaller>(args => new ContentItemInstaller());
			container.RegisterFactory<IDefaultLogger>(args => new DefaultLogger() );
			container.RegisterFactory<ISignatureService>(args=> new SignatureService((string)args[0]));
			container.RegisterFactory<IItemComparer>(args => new DefaultItemComparer());
			container.RegisterFactory<IYamlSerializationService>(args => new YamlSerializationService());
			container.Register<IChecksumManager, ChecksumManager>();

			return container;
		}
	}
}
