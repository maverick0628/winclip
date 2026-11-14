# Contributing to WinClip

WinClip is free and open source under the [MIT licence](LICENSE). Bug reports, fixes and small features are welcome.

## Before you start

- For anything larger than a bug fix, open an issue first so we can agree on the approach before you write code.
- WinClip is a Windows desktop app (WPF, .NET 8). You need Windows 10 or 11 and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build and run it. The code does not build on macOS or Linux.

## Build and test

```powershell
dotnet restore WinClip.sln
dotnet build WinClip.sln -c Release
dotnet test WinClip.Tests/WinClip.Tests.csproj -c Release
dotnet test src/WinClip.Tests/WinClip.Tests.csproj -c Release
```

CI runs the same commands on every pull request to `master`. Some tests open WPF windows, so run them in an interactive Windows session, not over SSH.

## Pull requests

- Keep each pull request to one concern. A bug fix and an unrelated refactor belong in separate pull requests.
- Add or update tests for behaviour you change. The project has two test projects: `WinClip.Tests` for the app and `src/WinClip.Tests` for actions and core.
- Make sure the build and both test projects pass before you open the pull request.
- Match the style of the surrounding code. `.editorconfig` sets the basics.
- Update `CHANGELOG.md` under `Unreleased` for changes users will notice.
- Describe what changed and why in the pull request. Include screenshots for UI changes.

## Adding an action or plugin

Built-in actions live in `src/WinClip.Actions/` and implement `IAction`. To ship an action outside the main app, build a plugin with the WinClip SDK; see [Docs/GettingStarted.md](Docs/GettingStarted.md) and the [Examples/](Examples/) directory.

## Reporting bugs

Open an issue with your Windows version, the WinClip version (shown in the tray menu), the steps to reproduce the problem and what you expected to happen. Logs are written under `%APPDATA%\WinClip\`. Remove anything private before you attach them.

Report security problems privately; see [SECURITY.md](SECURITY.md).

## Licence

By contributing you agree that your contribution is licensed under the MIT licence that covers the project.
