# Generating a new Sidekick App

It's now easier than ever with powershell and the Sidekick template

1. Make sure you have the Sidekick foundation nuget package installed (included in the Sidekick core apps package)
1. Pull down the template zip from [here](https://github.com/JeffDarchuk/SitecoreSidekick/raw/master/doc/SidekickTemplate.zip).
1. Extract it into your Sitecore solution.
1. Navigate to the project in the file system and run the file **RunMeToStart.cmd**
1. ![Console Window](SidekickConsole.png)
1. Enter the project namespace
1. Enter the project name camel case and without any spaces or non-word characters
1. Enter a human readable name that users will see when they open sidekick
1. Enter a 2 character code that's unique to your app from the other apps in sidekick
1. The powershell scripts will then rename and inject your properties all over your sidekick app project
1. Open your Sitecore solution and add the project as an existing project
1. Make sure that you're set up to publish your config and binaries to the appropriate location on build

You will now have a new demo app.  here i've named mine "Event Manager"

![New App Project](NewAppProject.png)

You have all the basic Angular peices as well as the registration and controller.  Everything you need for your Sidekick app.


![New App](NewApp.png)

![New App Running](NewAppRunning.png)

In this simple app you can click a button to have the angularjs factory call to the backend controller to get some content from the backend and append it to the front end.
Take this idea and expand uppon it to create whatever you want.  Feel like sharing your app?  Turn it into a nuget package with the binaries and configs and people can install it via nuget.

## Bootstrap
By default this Sidekick app comes with a bootstrapper which acts as a primitive IOC container.  The reason this exists is to have a way to make service based code in a module environment where you can't be certain any IOC containers exist.  If you're making a new module, feel free to use it, however if you're adding an app to a website that has an IOC container you should definately use that.

The bootstrap treats everything like a singleton
```csharp
		private static Container InitializeContainer()
		{
			Container container = SitecoreSidekick.Bootstrap.Container;

			// Register components here
			// This assumes a parameterless constructor
			container.Register<IContentMigrationManagerService, ContentMigrationManagerService>();
			// This you can controll how the object is constructed
			container.RegisterFactory<IRemoteContentService>(args =>
			{
				var registration = container.Resolve<IScsRegistrationService>();
				return new RemoteContentService(registration);
			});
			return container;
		}
```

Then pull it out like this.

```csharp
_migrationManager = Bootstrap.Container.Resolve<IContentMigrationManagerService>();
```
