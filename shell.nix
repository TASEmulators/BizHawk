{ system ? builtins.currentSystem
, pkgs ? import <nixpkgs> { inherit system; }
, lib ? pkgs.lib
, mkShell ? pkgs.mkShell
, git-cola ? pkgs.git-cola
, git ? pkgs.gitMinimal
, nano ? pkgs.nano
, powershell ? pkgs.powershell
, debugDotnetHostCrashes ? false # forwarded to Dist/launch-scripts.nix
, debugPInvokes ? false # forwarded to Dist/launch-scripts.nix
, forNixOS ? true
, profileManagedCalls ? false # forwarded to Dist/launch-scripts.nix
, useKate ? false
, useNanoAndCola ? false
, useVSCode ? false
}: let
	avail = import ./. {
		inherit forNixOS system
			debugDotnetHostCrashes debugPInvokes profileManagedCalls;
	};
	f = drv: mkShell {
		packages = [ git powershell ]
			++ lib.optionals useNanoAndCola [ git-cola nano ]
			++ lib.optionals useKate avail.IDEs.kate
			++ lib.optionals useVSCode [] #TODO https://devblogs.microsoft.com/dotnet/csharp-dev-kit-now-generally-available/ https://learn.microsoft.com/en-us/training/modules/implement-visual-studio-code-debugging-tools/
			;
		inputsFrom = [ drv ];
		shellHook = avail.shellHook drv;
	};
	shells = lib.pipe avail [
		(lib.mapAttrs (name: drv: if lib.hasPrefix "bizhawkAssemblies-" name then drv else drv.assemblies or null))
		(lib.filterAttrs (_: drv: drv != null))
		(lib.mapAttrs (_: asms: lib.traceIf (lib.hasSuffix "-bin" asms.name) "the attr specified packages BizHawk from release artifacts; some builddeps may be missing from this shell"
			f asms))
	];
in shells // mkShell {
	packages = [ avail.emuhawk.hawkSourceInfo.dotnet-sdk ];
	inputsFrom = [ shells.emuhawk-latest ]; # this is the intended way to `override` a shell env.
}
