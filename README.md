# So you want to make a Jellyfin plugin

Awesome! This guide is for you. Jellyfin plugins are written using the dotnet standard framework. What that means is you can write them in any language that implements the CLI or the DLI and can compile to netstandard2.1. The examples on this page are in C# because that is what most of Jellyfin is written in, but F#, Visual Basic, and IronPython should all be compatible once compiled.

## 0. Things you need to get started

- [Dotnet Core SDK 2.2](https://dotnet.microsoft.com/download)

- An editor of your choice. Some free choices are:

   [Visual Studio Code](https://code.visualstudio.com/)

   [Visual Studio Community Edition](https://visualstudio.microsoft.com/downloads/)

   [Mono Develop](https://www.monodevelop.com/)

## 0.5. Quickstarts

We have a number of quickstart options available to speed you along the way.

- [Download the Example Plugin Project](https://github.com/jellyfin/jellyfin-plugin-template/tree/master/Jellyfin.Plugin.Template) from this repository, open it in your IDE and go to [step 3](https://github.com/jellyfin/jellyfin-plugin-template#3-customize-plugin-information)

- Install our dotnet template by [downloading the dotnet-template/content folder from this repo](https://github.com/jellyfin/jellyfin-plugin-template/tree/master/dotnet-template/content) or off of Nuget (Coming soon)

   ```
   dotnet new -i /path/to/templatefolder
   ```

- Run this command then skip to step 4

   ```
      dotnet new Jellyfin-plugin -name MyPlugin
   ```

If you'd rather start from scratch keep going on to step 1. This assumes no specific editor or IDE and requires only the command line with dotnet in the path.

## 1. Initialize your Project

Make a new dotnet standard project with the following command, it will make a directory for itself:

```
dotnet new classlib -f netstandard2.0 -n MyJellyFinPlugin
```

Now add the Jellyfin shared libraries.

```
dotnet add package Jellyfin.Model
dotnet add package Jellyfin.Controller
```

You have an autogenerated Class1.cs file, you won't be needing this, so go ahead and delete it.

## 2. Setup Basics

There are a few mandatory classes you'll need for a plugin so we need to make them.

### Make a new class called PluginConfiguration

You can call it watever you'd like readlly. This class is used to hold settings your plugin might need. We can leave it empty for now. This class should inherit from `MediaBrowser.Model.Plugins.BasePluginConfiguration`

### Make a new class called Plugin

This is the main class for your plugin. It will define your name, version and Id. It should inherit from `MediaBrowser.Common.Plugins.BasePlugin<PluginConfiguration> `

Note: If you called your PluginConfiguration class something different, you need to put that between the <>

### Implement Required Properties

The Plugin class needs a few properties implemented before it can work correctly.

It needs an override on ID, an override on Name and a constructor that follows a specific model. To get started you can use the following snippit:

```c#
public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer){}
public override string Name => throw new System.NotImplementedException();
public override Guid Id => Guid.Parse("");
```

## 3. Customize Plugin Information

You need to populate some of your plugin's information. Go ahead a put in a string of the Name you've overridden name, and generate a GUID
- **Windows Users**: you can use the Powershell command `New-Guid`, `[guid]::NewGuid()` or the Visual Studio GUID generator
- **Linux and OS X Users**: you can use the Powershell Core command `New-Guid` or this command from your shell of choice:

   ```bash
   od -x /dev/urandom | head -1 | awk '{OFS="-"; srand($6); sub(/./,"4",$5); sub(/./,substr("89ab",rand()*4,1),$6); print $2$3,$4,$5,$6,$7$8$9}'
   ```
or

   ```bash
   uuidgen
   ```

- Place that guid inside the `Guid.Parse("")` quotes to define your plugin's ID.

## 4. Adding Functionality

Congratulations, you now have everything you need for a perfectly functional functionless Jellyfin plugin! You can try it out right now if you'd like by compiling it, then placing the dll you generate in the plugins folder under your Jellyfin config directory. If you want to try and hook it up to a debugger make sure you copy the generated PDB file alongside it.

Most people aren't satisfied with just having an entry in a menu for their plugin, most people want to have some functionality, so lets look at how to add it.

### 4a. Implement interfaces to add components

If the functionality you are trying to add is functionality related to something that Jellyfin has an interface for you're in luck. Jellyfin uses some automatic discovery and injection to allow any interfaces you implement in your plugin to be available in Jellyfin.

Here's some interfaces you could implement for common use cases:

- **IAuthenticationProvider** - Allows you to add an authentication provider that can authenticate a user based on a name and a password, but that doesn't expect to deal with local users.
- **IBaseItemComparer** - Allows you to add sorting rules for dealing with media that will show up in sort menus
- **IImageEnhancer** - Allows you to intercept and manipulate images served by Jellyfin
- **IIntroProvider** - Allows you to play a piece of media before another piece of media (i.e. a trailer before a movie, or a network bumper before an episode of a show)
- **IItemResolver** - Allows you to define custom media types
- **ILibraryPostScanTask** - Allows you to define a task that fires after scanning a library
- **ILibraryPreScanTask** - Allows you to define a task that fires before scanning a library
- **IMetadataSaver** - Allows you to define a metadata standard that Jellyfin can use to write metadata
- **IResolverIgnoreRule** - Allows you to define subpaths that are ignored by media resolvers for use with another function (i.e. you wanted to have a theme song for each tv series stored in a subfolder that could be accessed by your plugin for playback in a menu).
- **IScheduledTask** - Allows you to create a scheduled task that will appear in the scheduled task lists on the dashboard.

There are loads of other interfaces that can be used, but you'll need to poke around the API to get some info. If you're an expert on a particular interface, you should help [contribute some documentation](https://docs.jellyfin.org/general/contributing/index.html)!

### 4b. Use plugin aimed interfaces to add custom functionality

If your plugin doesn't fit perfectly neatly into a predefined interface, never fear, there are a set of interfaces that allow your plugin to extend Jellyfin any which way you please. Here's a quick overview on how to use them

- **IPluginConfigurationPage** - Allows you to have a plugin config page on the dashboard. If you used one of the quickstart example projects, a premade page with some useful components to work with has been created for you! If not you can check out this guide here for how to whip one up.

- **IRestfulService** - Allows you to extend the Jellyfin http API and handle API calls that come in on the routes you define.

- **IServerEntryPoint** - Allows you to run code at server startup that will stay in memory. You can make as many of these as you need and it is wildly useful for loading configs or persisting state.

Likewise you might need to get data and services from the Jellyfin core, Jellyfin provides a number of interfaces you can add as parameters to your plugin constructor which are then made available in your project (you can see the 2 mandatory ones that are needed by the plugin system in the constructor as is).

- **IBlurayExaminer** - Allows you to examine blu-ray folders
- **IDtoService** - Allows you to create data transport objects, presumably to send to other plugins or to the core
- **IIsoManager** - Allows the mounting and unmounting of ISO files
- **IJsonSerializer** - Allows you to use the main json serializer
- **ILibraryManager** - Allows you to directly access the media libraries without hopping through the API
- **ILocalizationManager** - Allows you tap into the main localization engine which governs translations, rating systems, units etc...
- **ILogManager** - Allows you to create log entries with a custom name in the application log file
- **INetworkManager** - Allows you to get information about the server's networking status
- **INotificationsRepository** - Allows you to send notifications to users
- **IServerApplicationPaths** - Allows you to get the running server's paths
- **IServerConfigurationManager** - Allows you to write or read server configuration data into the application paths
- **ITaskManager** - Allows you to execute and manipulate scheduled tasks
- **IUserManager** - Allows you to retrieve user info and user library related info
- **IXmlSerializer** - Allows you to use the main xml serializer
- **IZipClient** - Allows you to use the core zip client for compressing and decompressing data

## 5. Submit your Plugin

- Choose a License, Jellyfin recommends [GPLv2](https://www.gnu.org/licenses/old-licenses/gpl-2.0.html). If you would like your plugin to be integrated into Jellyfin and available from the plugin browser you MUST choose a [GPL Compatible License](https://www.gnu.org/licenses/license-list.html#GPLCompatibleLicenses)

- Upload your plugin to github.

- Contact the Jellyfin Team!

## A note about licensing

Licensing is a complex topic. This repository features a GPLv3 license template that can be used to provide a good default license for your plugin. You may alter this if you like, but if you do a permissive license must be chosen.

Due to how plugins in Jellyfin work, when your plugin is compiled into a binary, it will link against the various Jellyfin binary NuGet packages. These packages are licensed under the GPLv3. Thus, due to the nature and restrictions of the GPL, the binary plugin you get will also be licensed under the GPLv3.

If you accept the default GPLv3 license from this template, all will be good. However if you choose a different license, please keep this fact in mind, as it might not always be obvious that an, e.g. MIT-licensed plugin would become GPLv3 when compiled.

Please note that this also means making "proprietary", source-unavailable, or otherwise "hidden" plugins for public consumption is not permitted. To build a Jellyfin plugin for distribution to others, it must be under the GPLv3 or a permissive open-source license that can be linked against the GPLv3.
