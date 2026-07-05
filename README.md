# OriathHubLib

`OriathHubLib` is a C# Shared Project intended to be imported by OriathHub plugins.
It provides source-level extensions and helpers on top of `OriathHub.Sdk` without producing a separate runtime assembly.

The project is designed for developers and LLM/code-generation agents that need a small, explicit integration surface:

- one shared source import: `src/OriathHubLib/OriathHubLib.projitems`;
- one MSBuild entry point for plugin projects: `OriathHubLib.props`;
- one SDK version source of truth: `OriathHubSdk.props`;
- one embedded offline NuGet feed: `nuget/`;
- one embedded NuGet configuration: `nuget.config`.

## Relationship with OriathHub.Sdk

`OriathHub.Sdk` is the compile-time SDK used to build OriathHub plugins.
It exposes reference assemblies for the host API, including `Core`, `RemoteObjects`, `Components`, `States`, `RemoteEnums`, `Utils`, and `PluginBase`, and also exposes public native container layouts from `GameOffsets.Natives.*`, such as `StdVector`, `StdMap`, and `StdWString`.
At runtime, the OriathHub host already loads the SDK assemblies, so plugins should normally ship only their plugin DLL, assets, and additional non-SDK dependencies.

`OriathHubLib` does not replace the SDK.
It extends SDK usage by injecting additional C# source files into the plugin assembly that imports it.

## Repository layout

```text
OriathHubLib/
  OriathHubLib.props          # Main MSBuild import used by plugin projects
  OriathHubSdk.props          # SDK package version source of truth
  nuget.config                # Restore configuration used by OriathHubLib.props
  nuget/                      # Offline NuGet feed containing OriathHub.Sdk.*.nupkg
  src/OriathHubLib/
    OriathHubLib.shproj       # Visual Studio shared project
    OriathHubLib.projitems    # Shared source item list
    RemoteObjects/            # Shared source files
```

## Versioning model

The SDK version is defined in `OriathHubSdk.props`:

```xml
<Project>
  <PropertyGroup>
    <OriathHubSdkVersion>0.13.0</OriathHubSdkVersion>
  </PropertyGroup>
</Project>
```

`OriathHubLib.props` imports `OriathHubSdk.props` and uses `$(OriathHubSdkVersion)` as the default version for the `OriathHub.Sdk` package reference.

When updating the SDK:

1. add the new `OriathHub.Sdk.<version>.nupkg` file to `nuget/`;
2. update `OriathHubSdk.props`;
3. update shared source code if required by SDK API changes;
4. commit the whole change as one compatibility unit.

This keeps the following coupling explicit:

```text
OriathHubLib commit X
  -> OriathHub.Sdk version Y
  -> shared source code compatible with SDK version Y
```

## Usage in a plugin repository

Add `OriathHubLib` as a Git submodule, usually under `external/`:

```bash
git submodule add https://github.com/Natsui31/OriathHubLib.git external/OriathHubLib
git commit -m "Add OriathHubLib shared project"
```

For fresh clones of the plugin repository, initialize submodules with:

```bash
git submodule update --init --recursive
```

In the plugin `.csproj`, import only `OriathHubLib.props`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <Import Project="external\OriathHubLib\OriathHubLib.props"
          Condition="Exists('external\OriathHubLib\OriathHubLib.props')" />
</Project>
```

Do not import `OriathHubLib.projitems` directly from the plugin project.
`OriathHubLib.props` already imports it.
Importing both files can compile the shared sources twice.

## What the props file does

`OriathHubLib.props` performs four tasks:

1. imports `OriathHubSdk.props`;
2. sets `RestoreConfigFile` to the embedded `nuget.config`;
3. adds the `OriathHub.Sdk` package reference using `$(OriathHubSdkVersion)`;
4. imports `src/OriathHubLib/OriathHubLib.projitems`.

The plugin repository does not need its own `nuget.config` just to restore `OriathHub.Sdk`, as long as it imports `OriathHubLib.props`.

## NuGet restore behavior

`nuget.config` defines two package sources:

```text
OriathHubLibOffline -> ./nuget
nuget.org           -> https://api.nuget.org/v3/index.json
```

Package Source Mapping is used so that:

- `OriathHub.Sdk` is restored from the embedded offline feed;
- other dependencies are restored from `nuget.org`.

If future SDK packages require additional offline-only transitive dependencies, add their package IDs or patterns to the `OriathHubLibOffline` mapping.

## Runtime deployment

Because `OriathHubLib` is a Shared Project, it does not produce `OriathHubLib.dll`.
Its source files are compiled into the plugin DLL.

A plugin should be deployed as expected by the OriathHub host:

```text
<OriathHubDir>/Plugins/<PluginName>/<PluginName>.dll
```

Ship additional non-SDK dependencies only when the plugin directly requires them at runtime.
Do not ship `OriathHub.Sdk` assemblies unless the host loading model changes.

## Development notes for LLM agents

When modifying this repository:

- treat `OriathHubSdk.props` as the SDK version source of truth;
- keep `OriathHubLib.props` as the only file a plugin project needs to import;
- do not add plugin-specific code or settings to this shared project;
- avoid introducing runtime assets unless they are explicitly required by shared source code;
- keep all shared source paths listed in `OriathHubLib.projitems`;
- keep `OriathHubLib.shproj` colocated with `OriathHubLib.projitems` so Visual Studio displays files under the expected tree;
- do not commit `.vs/`, `bin/`, or `obj/` directories.
