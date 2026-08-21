# Accessible BizHawk

An accessibility fork of [BizHawk](https://github.com/TASEmulators/BizHawk), the multi-system emulator developed by the TASVideos community.

BizHawk is an excellent multi-system emulator designed for tool-assisted speedrunning, featuring accurate emulation, Lua scripting support, memory inspection tools, and much more. This fork extends BizHawk with screen reader compatibility, enabling blind and visually impaired users to access the emulator's powerful features.

## Purpose

This fork adds full NVDA screen reader support for keyboard navigation throughout the application. The goal is to make BizHawk's Lua scripting console and memory tools accessible for developers creating accessibility modifications for retro games.

## Accessibility Changes

### Native Menu System

The WinForms MenuStrip controls have been replaced with native Win32 MainMenu controls. Native Windows menus have built-in accessibility support that integrates properly with screen readers during keyboard navigation.

**Windows with native menus:**
- Main emulator window
- Lua Console
- RAM Watch
- Hex Editor

### Accessible Toolbars

Toolbar controls have been reimplemented using ListView, a native Windows control with complete screen reader support. Each toolbar button is announced by NVDA when navigating with the keyboard.

**Windows with accessible toolbars:**
- Lua Console (11 toolbar actions)
- RAM Watch (14 toolbar actions)

### Control Accessibility Properties

All interactive controls now include appropriate AccessibleName, AccessibleDescription, and AccessibleRole properties to provide context for screen reader users.

## Technical Background

WinForms ToolStrip and MenuStrip controls do not fire Microsoft Active Accessibility (MSAA) focus events during keyboard navigation. Screen readers rely on these events to track and announce the currently focused element. Without them, keyboard navigation is silent while mouse interaction works correctly.

The solution replaces these controls with native Windows equivalents that have accessibility support built into the operating system itself. For complete technical documentation, including analysis of attempted solutions and implementation details, see [NativeMenuAccessibility.txt](NativeMenuAccessibility.txt).

## Known Behavior

When navigating the Lua Console toolbar with the keyboard, there is a brief pause between items. This occurs because NVDA announces each toolbar button as it receives focus. This is normal screen reader behavior and indicates that accessibility is functioning correctly.

## Installation

1. Download the latest release from the [Releases](https://github.com/Lethal-Lawnmower/BizHawk/releases) page
2. Extract the archive to your preferred location
3. Run `EmuHawk.exe`

Accessibility features are enabled by default. No additional configuration is required.

## Building from Source

```
git clone https://github.com/Lethal-Lawnmower/BizHawk.git
cd BizHawk
dotnet build src/BizHawk.Client.EmuHawk/BizHawk.Client.EmuHawk.csproj -c Release
```

## Use Case

This fork is intended for developers who want to use BizHawk's Lua scripting and memory inspection capabilities to create accessibility tools for retro games. The accessible Lua Console and RAM Watch windows enable blind developers to write scripts, monitor game memory, and test accessibility implementations.

## Acknowledgments

- [TASVideos](http://tasvideos.org/) and the BizHawk development team for creating and maintaining an exceptional emulator
- The BizHawk project is available at https://github.com/TASEmulators/BizHawk

## License

This fork maintains the same MIT License as the original BizHawk project.
