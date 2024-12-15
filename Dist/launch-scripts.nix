{ lib
# infrastructure
, writeShellScript
, writeText
# rundeps
, bizhawkAssemblies
, mkfifo
, mktemp
, nixGL
# other parameters
, debugPInvokes
, debugDotnetHostCrashes
, initConfig # pretend this is JSON; the following env. vars will be substituted by the wrapper script (if surrounded by double-percent e.g. `%%BIZHAWK_DATA_HOME%%`): `BIZHAWK_DATA_HOME`
, isManualLocalBuild # i.e. dotnet build in nix-shell; skips everything involving BIZHAWK_DATA_HOME, such as copying Assets and initConfig
, profileManagedCalls
}: let
	/**
	 * you can make use of the call duration data by seeing which methods it's spending the longest in: `mprof-report --reports=call --method-sort=self output/*.flame.mlpd` (really you'd want to sort by the ratio `self/count` but that's not an option)
	 *
	 * the other useful profiling mode is allocations: `nix-shell --argstr profileManagedCalls " --profile=log:alloc,nocalls,output=%t.alloc.mlpd"`
	 * you can make use of the allocation data by listing which types had the most instances allocated: `mprof-report --reports=alloc --alloc-sort=count output/*.alloc.mlpd`
	 */
	monoProfilerFlag = if builtins.isString profileManagedCalls
		then profileManagedCalls
		else if profileManagedCalls then " --profile=log:noalloc,calls,zip,output=%t.flame.mlpd" else "";
	hawkVersion = bizhawkAssemblies.hawkSourceInfo.version;
	commentLineIf = b: lib.optionalString b "# ";
	/**
	 * shell script to copy std{out,err} to files but leave them going to console;
	 * call as `redirect-output-to-files /target/stdout.txt /target/stderr.txt /path/to/bin arg1 arg2...`
	 */
	redirectOutputToFiles = writeShellScript "redirect-output-to-files" ''
		mktemp='${lib.getBin mktemp}/bin/mktemp'
		o="$("$mktemp" -u)"
		e="$("$mktemp" -u)"
		'${lib.getBin mkfifo}/bin/mkfifo' "$o" "$e"
		tee "$1" <"$o" &
		shift
		tee "$1" <"$e" | sed "s/.*/$(tput setaf 1)&$(tput sgr0)/" >&2 &
		shift
		exec "$@" >"$o" 2>"$e"
	'';
	emuhawk = let
		initConfigFile = writeText "config.json" (builtins.toJSON ({
			LastWrittenFrom = if builtins.length (builtins.splitVersion hawkVersion) < 3 then "${hawkVersion}.0" else hawkVersion; # 2.x.0 never included the trailing 0 in display name, and didn't include it in the internal value after 2.5(?); need to do version range check here
			PathEntries = {
				Paths = [
					({ System = "Global_NULL"; Type = "Base"; Path = "%%BIZHAWK_DATA_HOME%%"; }
						// lib.optionalAttrs bizhawkAssemblies.hawkSourceInfo.pathConfigNeedsOrdinal { Ordinal = 1; })
				];
			};
		} // initConfig));
		assetManagementScript = ''if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		export BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		if [ ! -e "$BIZHAWK_DATA_HOME" ]; then
			mkdir -p "$BIZHAWK_DATA_HOME"
			cd '${bizhawkAssemblies.assets}'
			find . -type f -not -wholename './nix-support/*' -exec install -DvT {} "$BIZHAWK_DATA_HOME/{}" \;
		fi
		cd "$BIZHAWK_DATA_HOME"
		if [ ! -e 'config.json' ]; then
			sed "s@%%BIZHAWK_DATA_HOME%%@$PWD@g" '${initConfigFile}' >config.json
			printf "wrote initial config to %s\n" "$PWD/config.json"
		fi

		export BIZHAWK_INT_SYSLIB_PATH='${bizhawkAssemblies.extraUnmanagedDeps}/lib'
		'';
		noAssetManagementScript = ''cd "$BIZHAWK_HOME"

		export BIZHAWK_INT_SYSLIB_PATH="$PWD/dll"
		'';
	in writeShellScript "emuhawk-mono-wrapper" ''
		set -e

		mainAppPath="$BIZHAWK_HOME/${bizhawkAssemblies.hawkSourceInfo.mainAppFilename}"
		if [ ! -e "$mainAppPath" ]; then
			printf "no such file: %s\n" "$mainAppPath"
			exit 1
		fi

		${if isManualLocalBuild then noAssetManagementScript else assetManagementScript}
		${""/*
			here's the breakdown for the GTK theme problem:
			problem 1: `Gtk not found (missing LD_LIBRARY_PATH to libgtk-x11-2.0.so.0?), using built-in colorscheme` printed to stderr
				fixed by adding `${pkgs.gtk2-x11.out}/lib` to `$LD_LIBRARY_PATH`
				we're now in Adwaita (light) instead of ugly beige!
				this does add a new warning to stderr though: `Unable to locate theme engine in module_path: "adwaita"`
					fixed by adding `${pkgs.gnome3.gnome-themes-extra}/lib/gtk-2.0` to `$GTK_PATH`
			sadly, it still doesn't seem to respect `$GTK_RC_FILES` or even `$GTK_THEME` :(
		*/}ldLibPath="$BIZHAWK_INT_SYSLIB_PATH:${lib.makeLibraryPath bizhawkAssemblies.buildInputs}"
		if [ -z "$LD_LIBRARY_PATH" ]; then
			export LD_LIBRARY_PATH="$ldLibPath"
		else
			export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:$ldLibPath"
		fi
		if [ -z "$GTK_PATH" ]; then
			export GTK_PATH='${bizhawkAssemblies.gnome-themes-extra}/lib/gtk-2.0'
		else
			export GTK_PATH="${bizhawkAssemblies.gnome-themes-extra}/lib/gtk-2.0:$GTK_PATH"
		fi
		${if profileManagedCalls == false then "" else ''printf "Will write profiling results to %s/*.mlpd\n" "$PWD"
		''}${if bizhawkAssemblies.hawkSourceInfo.hasAssemblyResolveHandler then "" else ''export MONO_PATH="$BIZHAWK_HOME/dll/nlua:$BIZHAWK_HOME/dll"
		''}${lib.optionalString (!debugPInvokes) "# "}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll # pass `--arg debugPInvokes true` to nix-build to enable
		${lib.optionalString debugDotnetHostCrashes "# "}export MONO_CRASH_NOFILE=1 # pass `--arg debugDotnetHostCrashes true` to nix-build to disable
		if [ "$1" = '--mono-no-redirect' ]; then
			printf "(passing --mono-no-redirect is no longer necessary)\n" >&2
			shift
		fi
		printf "(capturing output in %s/EmuHawkMono_last*.txt)\n" "$PWD" >&2
		exec '${redirectOutputToFiles}' EmuHawkMono_laststdout.txt EmuHawkMono_laststderr.txt \
			'${lib.getBin bizhawkAssemblies.mono}/bin/mono'${monoProfilerFlag} \
				"$mainAppPath" --config=config.json "$@"
	'';
in {
	inherit emuhawk;
	discohawk = writeShellScript "discohawk-mono-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/DiscoHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/DiscoHawk.exe"
			exit 1
		fi

		if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		export BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		mkdir -p "$BIZHAWK_DATA_HOME"
		cd "$BIZHAWK_DATA_HOME"

		${if profileManagedCalls == false then "" else ''printf "Will write profiling results to %s/*.mlpd\n" "$PWD"
		''}export MONO_PATH="$BIZHAWK_HOME/dll"
		${lib.optionalString (!debugPInvokes) "# "}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll # pass `--arg debugPInvokes true` to nix-build to enable
		exec '${lib.getBin bizhawkAssemblies.mono}/bin/mono'${monoProfilerFlag} \
			"$BIZHAWK_HOME/DiscoHawk.exe" "$@"
	'';
	emuhawkNonNixOS = writeShellScript "emuhawk-mono-wrapper-non-nixos" ''exec '${nixGL}/bin/nixGL' '${emuhawk}' "$@"'';
}
