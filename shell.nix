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
		shellHook = ''
			export BIZHAWKBUILD_HOME='${builtins.toString ./.}'
			export BIZHAWK_HOME="$BIZHAWKBUILD_HOME/output/"
			ldLibPath='${lib.makeLibraryPath drv.buildInputs}' # for running tests
			if [ -z "$LD_LIBRARY_PATH" ]; then
				export LD_LIBRARY_PATH="$ldLibPath"
			else
				export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:$ldLibPath"
			fi
			alias discohawk-monort-local='${avail.launchScriptsForLocalBuild.discohawk}'
			alias emuhawk-monort-local='${avail.launchScriptsForLocalBuild.emuhawk}'
			case "$-" in *i*)
				pfx="$(realpath --relative-to="$PWD" "$BIZHAWKBUILD_HOME")/"
				if [ "$pfx" = "./" ]; then pfx=""; fi
				printf "%s\n%s\n" \
					"Run ''${pfx}Dist/Build{Debug,Release}.sh to build the solution. You may need to clean up with ''${pfx}Dist/CleanupBuildOutputDirs.sh." \
					"Once built, running {discohawk,emuhawk}-monort-local will pull from ''${pfx}output/* and use Mono from Nixpkgs."
				;;
			esac
		'';
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
