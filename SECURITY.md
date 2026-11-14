# Security Policy

## Reporting a vulnerability

Report security problems privately to support@duncansmith.tech. Do not open a public issue or pull request for a vulnerability.

Include:

- the WinClip version (shown in the tray menu) and your Windows version
- a description of the problem and its impact
- steps to reproduce it, or a proof of concept

You will get an acknowledgement within a few days. Please give us reasonable time to ship a fix before you disclose the problem publicly.

## Supported versions

Only the latest release receives security fixes. WinClip updates itself, so most users are on the latest release.

## Scope

In scope: the WinClip application, its installers, the auto-update path and the plugin sandbox.

Out of scope: problems in third-party AI providers you configure, and problems that need an attacker who already controls your Windows account.

## Release signing

Release installers are signed with Azure Trusted Signing under the identity DST Digital, LLC. The updater refuses installers that are not signed by that identity. See [Services/UpdateSignerPin.cs](Services/UpdateSignerPin.cs).
