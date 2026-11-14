# Third-Party Notices

WinClip is released under the [MIT licence](LICENSE). It depends on the packages below, each under its own licence. Transitive dependencies carry their own licences; check the package metadata on nuget.org for the full set.

## Shipped with the application

| Package | Version | Licence |
|---------|---------|---------|
| [WPF-UI](https://github.com/lepoco/wpfui) | 3.0.5 | MIT |
| [ZXing.Net](https://github.com/micjahn/ZXing.Net) | 0.16.11 | Apache-2.0 |
| [ZXing.Net.Bindings.Windows.Compatibility](https://github.com/micjahn/ZXing.Net) | 0.16.14 | Apache-2.0 |
| Microsoft.Extensions.Configuration.Abstractions | 8.0.0 | MIT |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | MIT |
| Microsoft.Extensions.Hosting | 8.0.0 | MIT |
| Microsoft.Extensions.Logging, .Abstractions, .Console, .Debug | 8.0.0 | MIT |
| System.Security.Cryptography.ProtectedData | 8.0.0 | MIT |
| Microsoft.Win32.Registry | 5.0.0 | MIT |

## Used only to build and test

| Package | Version | Licence |
|---------|---------|---------|
| xunit | 2.6.6 | Apache-2.0 |
| xunit.runner.visualstudio | 2.5.4 and 2.5.6 | Apache-2.0 |
| Moq | 4.20.70 | BSD-3-Clause |
| Microsoft.NET.Test.Sdk | 17.8.0 and 17.9.0 | MIT |
| Microsoft.Extensions.Configuration | 8.0.0 | MIT |
| Microsoft.Extensions.Logging | 8.0.0 and 8.0.1 | MIT |

## Build tools

The Inno Setup installer scripts in `installer/` are compiled with [Inno Setup](https://jrsoftware.org/isinfo.php), which is not distributed here.
