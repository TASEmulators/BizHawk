# Project Overview

This is a retro game emulator project, though emulation is mostly handled by libraries, while this repo is focused on the frontend.

## Folder Structure

- `/ExternalProjects`: Contains the source code for some vendored C# libraries.
- `/src`: Contains the source code for the libraries and the frontend.

Only change C# source files (`*.cs`) and project files (`*.csproj`, `*.props`).

## Libraries and Frameworks

- .NET Framework 4.8 for the frontend.
- .NET Standard 2.0 for the libraries, though some newer features are available thanks to polyfills.

## Coding Standards

- Follow the code style conventions given in the root EditorConfig file.
- Leave a comment beside each block of code you touch with your name/version and an explanation of how the old code was broken.

## UI guidelines

- Use American English spellings in strings.
- Avoid changing the GUI if possible. The GUI encompasses instances of the `Form` and `Control` classes from Windows Forms, which usually have a partial class definition in a file named `*.Designer.cs`.
