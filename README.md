# OriathHubLib

`OriathHubLib` is a C# Shared Project intended to be imported by OriathHub plugins.
Its purpose is to extend and simplify usage of the API exposed by the external `OriathHub.Sdk`, without replacing the SDK and without producing a separate runtime assembly.

The OriathHub SDK is not authored or maintained in this repository.
The upstream SDK can be found here:

https://github.com/danthespal/OriathHubSDK

## Purpose

`OriathHubLib` provides additional source-level helpers, wrappers, and extension code for plugin projects that already depend on `OriathHub.Sdk`.

In practical terms, this repository acts as a companion layer around the SDK:

- the SDK provides the official OriathHub plugin API;
- this repository adds shared C# source files that make that API easier to consume;
- plugin projects import this repository as source, so the code is compiled directly into the plugin assembly;
- no `OriathHubLib.dll` is produced or deployed at runtime.

## Relationship with OriathHub.Sdk

`OriathHub.Sdk` is the compile-time SDK used to build OriathHub plugins.
It exposes the host API reference assemblies, including `Core`, `RemoteObjects`, `Components`, `States`, `RemoteEnums`, `Utils`, and `PluginBase`.
It also exposes public native container layouts from `GameOffsets.Natives.*`, such as `StdVector`, `StdMap`, and `StdWString`.

`OriathHubLib` depends on that SDK API and extends how it is used from plugin code.
It does not own, modify, replace, or redistribute the SDK project itself.

At runtime, the OriathHub host already loads the SDK assemblies.
Plugins should normally ship only their plugin DLL, assets, and additional non-SDK dependencies.

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

## Integration surface

The repository is intentionally small and explicit. A plugin project should only need:

- `OriathHubLib.props` as the MSBuild import entry point;
- `src/OriathHubLib/OriathHubLib.projitems` as the shared source list, imported indirectly by the props file;
- `OriathHubSdk.props` as the SDK version source of truth;
- `nuget.config` and `nuget/` for SDK package restore.

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

When updating the SDK compatibility target:

1. add the new `OriathHub.Sdk.<version>.nupkg` file to `nuget/`;
2. update `OriathHubSdk.props`;
3. update the shared source code if required by SDK API changes;
4. commit the SDK package, version file, and shared source changes together.

This keeps the compatibility relationship explicit:

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

## Development notes

When modifying this repository:

- treat `OriathHubSdk.props` as the SDK version source of truth;
- keep `OriathHubLib.props` as the only file a plugin project needs to import;
- keep this repository focused on SDK API extensions and shared plugin helpers;
- do not add plugin-specific code or settings to this shared project;
- avoid introducing runtime assets unless they are explicitly required by shared source code;
- keep all shared source paths listed in `OriathHubLib.projitems`;
- keep `OriathHubLib.shproj` colocated with `OriathHubLib.projitems` so Visual Studio displays files under the expected tree;
