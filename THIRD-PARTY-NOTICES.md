# Third-Party Notices

BackPocket Emulator is licensed under the [MIT License](LICENSE). The applications built from this
repository (`emudbg` from `src/BadMango.Emulator.Debug` and the desktop UI from
`src/BadMango.Emulator.UI`) redistribute the third-party components listed below. Each component
remains under its own license; the license identifiers below are taken from each package's NuGet
metadata (`.nuspec`).

## Native libraries

### OpenAL Soft 1.23.1

| | |
|---|---|
| Used by | `BadMango.Emulator.Devices` (speaker audio output) — shipped with `emudbg` |
| License | **LGPL-2.0-or-later** (GNU Library General Public License, version 2 or, at your option, any later version) |
| Full license text | <https://www.gnu.org/licenses/old-licenses/lgpl-2.0.html> (upstream copy: <https://github.com/kcat/openal-soft/blob/master/COPYING>) |
| Upstream project | <https://openal-soft.org/> |
| Source code | <https://github.com/kcat/openal-soft> (release tag `1.23.1`) |
| Delivered via | NuGet package [`Silk.NET.OpenAL.Soft.Native` 1.23.1](https://www.nuget.org/packages/Silk.NET.OpenAL.Soft.Native/1.23.1), packaged by the .NET Foundation and Contributors ([Silk.NET](https://github.com/dotnet/Silk.NET)); the package itself is declared `LGPL-2.0-or-later` |
| Files | `soft_oal.dll` (Windows), `libopenal.so` (Linux), `libopenal.dylib` (macOS), in the output folder or under `runtimes/<rid>/native/` |

How OpenAL Soft is used:

- The binaries are redistributed **unmodified**, exactly as published in the NuGet package above.
  This repository contains none of OpenAL Soft's source code and does not statically link it.
- The library is loaded **dynamically at runtime** as a separate shared library. `OpenTK.Audio.OpenAL`
  binds to it through its runtime library resolver; `OpenAlNativeLibrary` only points OpenTK at the
  bundled file when it exists and loads, and otherwise OpenTK falls back to the system OpenAL library.
- **You may replace it with your own build** (for example, a modified OpenAL Soft) by replacing the
  file listed above in the application folder, or by removing it so the system OpenAL library is used.
  No recompilation or relinking of BackPocket Emulator is required.
- The complete corresponding source code for the distributed version is available from the upstream
  repository at the `1.23.1` tag. If you received a binary distribution of BackPocket Emulator and
  cannot obtain that source, open an issue at
  <https://github.com/Bad-Mango-Solutions/back-pocket-emulator/issues> to request a copy.

## Managed (.NET) libraries

| Component | Version | License | Project |
|---|---|---|---|
| OpenTK.Audio.OpenAL | 4.9.4 | MIT | <https://github.com/opentk/opentk> |
| OpenTK.Core | 4.9.4 | MIT | <https://github.com/opentk/opentk> |
| OpenTK.Mathematics | 4.9.4 | MIT | <https://github.com/opentk/opentk> |
| Serilog | 4.3.1 | Apache-2.0 | <https://serilog.net/> |
| Serilog.Extensions.Hosting | 10.0.0 | Apache-2.0 | <https://github.com/serilog/serilog-extensions-hosting> |
| Serilog.Settings.Configuration | 10.0.0 | Apache-2.0 | <https://github.com/serilog/serilog-settings-configuration> |
| Serilog.Sinks.Console | 6.1.1 | Apache-2.0 | <https://github.com/serilog/serilog-sinks-console> |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 | <https://github.com/serilog/serilog-sinks-file> |
| Autofac | 9.1.0 | MIT | <https://autofac.org/> |
| Autofac.Extensions.DependencyInjection | 11.0.0 | MIT | <https://autofac.org/> |
| Avalonia (incl. Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter, Avalonia.Skia) | 12.0.2 | MIT | <https://avaloniaui.net/> |
| Avalonia.Controls.DataGrid | 12.0.0 | MIT | <https://avaloniaui.net/> |
| Avalonia.AvaloniaEdit | 12.0.0 | MIT | <https://github.com/AvaloniaUI/AvaloniaEdit> |
| AvaloniaEdit.TextMate | 12.0.0 | MIT | <https://github.com/AvaloniaUI/AvaloniaEdit> |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | <https://github.com/CommunityToolkit/dotnet> |
| Microsoft.Extensions.Hosting, Configuration, and Logging.Abstractions packages | 10.0.7 | MIT | <https://dot.net/> |

This table lists the packages referenced directly by the shipped projects. Their transitive
dependencies (for example SkiaSharp and HarfBuzzSharp, pulled in by Avalonia) are distributed under
their own licenses, as declared in their NuGet packages.
