{ lib
# infrastructure
, commentUnless
, versionAtLeast
, writeShellScriptBin
, writeText
# rundeps
, bizhawk
, mesa
, mono
, openal
# other parameters
, debugPInvokes
, hawkVersion
, initConfig # pretend this is JSON; the following env. vars will be substituted by the wrapper script (if surrounded by double-percent e.g. `%%BIZHAWK_DATA_HOME%%`): `BIZHAWK_DATA_HOME`
}:
let
	glHackLibs = rec {
		Fedora_33 = [
			"libdrm_amdgpu.so.1" "libdrm_nouveau.so.2" "libdrm_radeon.so.1" "libedit.so.0" "libelf.so.1" "libffi.so.6" "libLLVM-11.so" "libtinfo.so.6" "libvulkan.so.1"
		];
		Manjaro_21_0_3 = [ # should match Arch and Manjaro releases from '20/'21
			"libdrm_amdgpu.so.1" "libdrm_nouveau.so.2" "libdrm_radeon.so.1" "libedit.so.0" "libelf.so.1" "libffi.so.7" "libGLdispatch.so.0" "libicudata.so.69" "libicuuc.so.69" "libLLVM-11.so" "liblzma.so.5" "libncursesw.so.6" "libsensors.so.5" "libstdc++.so.6" "libvulkan.so.1" "libxml2.so.2" "libz.so.1" "libzstd.so.1"
		];
		Manjaro_21_2_1 = [ # should match Arch and Manjaro releases from '22
			"libdrm_amdgpu.so.1" "libdrm_nouveau.so.2" "libdrm_radeon.so.1" "libedit.so.0" "libelf.so.1" "libffi.so.8" "libicudata.so.70" "libicuuc.so.70" "libLLVM-13.so" "libncursesw.so.6" "libsensors.so.5" "libstdc++.so.6" "libvulkan.so.1" "libxml2.so.2" "libzstd.so.1"
		];
		LinuxMint_20_2 = [ # should match Ubuntu 20.04 and similar distros
			"libbsd.so.0" "libedit.so.2" "libLLVM-12.so.1" "libtinfo.so.6"
		] ++ Manjaro_21_0_3; #TODO split
	};
	glHackLibsFlat = lib.unique (lib.flatten (builtins.attrValues glHackLibs));
	initConfigFile = writeText "config.json" (builtins.toJSON ({
		LastWrittenFrom = if builtins.length (builtins.splitVersion hawkVersion) < 3 then "${hawkVersion}.0" else hawkVersion;
		PathEntries = {
			Paths = [
				({ "System" = "Global_NULL"; Type = "Base"; Path = "%%BIZHAWK_DATA_HOME%%"; } // lib.optionalAttrs (!versionAtLeast "2.7.1" hawkVersion) { "Ordinal" = 1; })
			];
		};
	} // initConfig));
in rec {
	discoWrapper = writeShellScriptBin "discohawk-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/EmuHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/EmuHawk.exe"
			exit 1
		fi

		export LD_LIBRARY_PATH="$BIZHAWK_HOME/dll"
		${commentUnless debugPInvokes}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll
		exec ${mono}/bin/mono "$BIZHAWK_HOME/DiscoHawk.exe" "$@"
	'';
	wrapperScript = writeShellScriptBin "emuhawk-wrapper" ''
		set -e

		if [ ! -e "$BIZHAWK_HOME/EmuHawk.exe" ]; then
			printf "no such file: %s\n" "$BIZHAWK_HOME/EmuHawk.exe"
			exit 1
		fi

		if [ "$XDG_DATA_HOME" ]; then
			BIZHAWK_DATA_HOME="$XDG_DATA_HOME"
		else
			BIZHAWK_DATA_HOME="$HOME/.local/share"
		fi
		BIZHAWK_DATA_HOME="$BIZHAWK_DATA_HOME/emuhawk-monort-${hawkVersion}"
		if [ ! -e "$BIZHAWK_DATA_HOME" ]; then
			mkdir -p "$BIZHAWK_DATA_HOME"
			cd "${bizhawk.out}"
			find . -type f -not -wholename "./nix-support/*" -exec install -DvT "{}" "$BIZHAWK_DATA_HOME/{}" \;
		fi
		if [ ! -e "$BIZHAWK_DATA_HOME/config.json" ]; then
			sed "s@%%BIZHAWK_DATA_HOME%%@$BIZHAWK_DATA_HOME@g" "${initConfigFile}" >"$BIZHAWK_DATA_HOME/config.json"
			printf "wrote initial config to %s\n" "$BIZHAWK_DATA_HOME/config.json"
		fi
		cd "$BIZHAWK_DATA_HOME"

		export LD_LIBRARY_PATH="$BIZHAWK_HOME/dll:$BIZHAWK_GLHACKDIR:${lib.makeLibraryPath [ openal ]}"
		${commentUnless debugPInvokes}export MONO_LOG_LEVEL=debug MONO_LOG_MASK=dll
		if [ "$1" = "--mono-no-redirect" ]; then
			shift
			printf "(received --mono-no-redirect, stdout was not captured)\n" >EmuHawkMono_laststdout.txt
			printf "(received --mono-no-redirect, stderr was not captured)\n" >EmuHawkMono_laststderr.txt
			exec ${mono}/bin/mono "$BIZHAWK_HOME/EmuHawk.exe" --config=config.json "$@"
		else
			exec ${mono}/bin/mono "$BIZHAWK_HOME/EmuHawk.exe" --config=config.json "$@" >EmuHawkMono_laststdout.txt 2>EmuHawkMono_laststderr.txt
		fi
	'';
	wrapperScriptNonNixOS = writeShellScriptBin "emuhawk-wrapper-non-nixos" ''
		set -e

		if [ "$XDG_STATE_HOME" ]; then
			BIZHAWK_GLHACKDIR="$XDG_STATE_HOME"
		else
			BIZHAWK_GLHACKDIR="$HOME/.local/state"
		fi
		export BIZHAWK_GLHACKDIR="$BIZHAWK_GLHACKDIR/emuhawk-monort-${hawkVersion}-non-nixos"
		mkdir -p "$BIZHAWK_GLHACKDIR"
		if [ ! -e "$BIZHAWK_GLHACKDIR/libGLX_indirect.so.0" ]; then
			ln -fsvT "${lib.getOutput "drivers" mesa}/lib/libGLX_mesa.so.0" "$BIZHAWK_GLHACKDIR/libGLX_indirect.so.0"
		fi
		# collect links to certain GL libs (and their deps) from host, added to LD_LIBRARY_PATH without polluting it with all libs from host
		for l in ${builtins.concatStringsSep " " glHackLibsFlat}; do
			if [ -e "$BIZHAWK_GLHACKDIR/$l" ]; then continue; fi
			# else it's either a broken link or it doesn't exist, we use ln -f to cover both
			for d in /usr/lib64 /usr/lib /usr/lib/x86_64-linux-gnu /lib64 /lib; do
				if [ -e "$d/$l" ]; then
					ln -fsvT "$d/$l" "$BIZHAWK_GLHACKDIR/$l"
					break
				fi
			done
		done

		for d in /usr/lib64/dri /usr/lib/dri /usr/lib/x86_64-linux-gnu/dri; do
			if [ -e "$d" ]; then
				export LIBGL_DRIVERS_PATH=$d
				break
			fi
		done

		exec ${wrapperScript}/bin/emuhawk-wrapper "$@"
	'';
}
