{ pkgs ? import (fetchTarball "https://github.com/NixOS/nixpkgs/archive/4077d9e705d08460d565ded6c7ee76c0ab7eaca3.tar.gz") {}
, stdenv ? pkgs.stdenv
, fetchgit ? pkgs.fetchgit
, hasSuffix ? pkgs.lib.hasSuffix
}:
stdenv.mkDerivation {
	pname = "bizhawk-wbox-musl";
	version = "1.2.0+593caa456-hawk"; # UPD <-- touch all of these lines when updating, and remember to also pull the submodule!
	buildInputs = [ stdenv ];
	srcs = [
		(fetchgit {
			url = "git://git.musl-libc.org/musl";
			rev = "593caa456309714402ca4cb77c3770f4c24da9da"; # UPD
			sha256 = "1kf7naaaqy109p9hmf922yxaky9ldifcyzk7n6h6ql9aipfjh7hk"; # UPD
		})
		./hawk-src-overlay
		(builtins.path {
			path = ./.;
			name = "scripts";
			filter = path: type: type == "regular" && (let
				ext = builtins.baseNameOf path;
			in hasSuffix ".sh" ext || hasSuffix ".patch" ext);
		})
	];
	sourceRoot = "musl-593caa4"; # UPD
	prePatch = ''
		chmod -R u+w ../hawk-src-overlay # 555->755, but why was it 555? without this, patch will fail (or make if -R omitted), as the modes are preserved on copy (and will also overwrite modes where a dir already exists)
		../scripts/.copy-dirs-for-wbox-arch.sh
	'';
	patches = [ ./waterbox.patch ];
	configurePhase = ''
		export SYSROOT="$out"
		../scripts/.wrapped-configure.sh
	'';
	buildPhase = ''
		make
	'';
	# install phase does `make install`
	postInstall = ''
		../scripts/.postpatch.sh
	'';
}
