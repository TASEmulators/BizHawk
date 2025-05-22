{
  description = "BizHawk is a multi-system emulator written in C#. BizHawk provides nice features for casual gamers such as full screen, and joypad support in addition to full rerecording and debugging tools for all system cores.";
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/release-24.05";
  };
  outputs =
    inputs@{ self, nixpkgs, ... }:
    with builtins;
    let
      std = nixpkgs.lib;
      systems = [
        # TODO :: can we build for aarch64-linux?
        "x86_64-linux"
      ];
      nixpkgsFor = std.genAttrs systems (
        system:
        import nixpkgs {
          localSystem = builtins.currentSystem or system;
          crossSystem = system;
          overlays = [ ];
        }
      );
    in
    {
      formatter = mapAttrs (system: pkgs: pkgs.nixfmt-rfc-style) nixpkgsFor;
      packages = mapAttrs (
        system: pkgs:
        # ./default.nix outputs some non-derivation attributes, so we have to filter those out
        (std.filterAttrs (name: val: std.isDerivation val) (import ./default.nix { inherit system pkgs; }))
        // {
          default = self.packages.${system}.emuhawk-latest-bin;
        }
      ) nixpkgsFor;
      devShells = mapAttrs (
        system: pkgs:
        # ./shell.nix outputs some non-derivation attributes and some extraneous derivations, so we have to filter those out
        (std.filterAttrs (
          name: val: std.isDerivation val && name != "stdenv" && name != "out" && name != "inputDerivation"
        ) (import ./shell.nix { inherit system pkgs; }))
        // {
          default = self.devShells.${system}.emuhawk-latest;
        }
      ) nixpkgsFor;
      apps =
        let
          # because for some reason the standard library doesn't include this (i think?)
          startsWith = prefix: st: (substring 0 (stringLength prefix) st) == prefix;
          toApps =
            app: pkgs:
            # filter packages to only include ones whose name starts with `app`, then map them to app definitions
            mapAttrs (name: pkg: {
              type = "app";
              # this seems to be correct, but I'm not entirely sure
              program = "${pkg}/bin/${pkg.name}";
            }) (std.filterAttrs (name: val: startsWith app name) pkgs);
        in
        mapAttrs (
          system: pkgs:
          (
            (toApps "emuhawk" pkgs)
            // (toApps "discohawk" pkgs)
            # TODO :: should `bizhawkAssemblies` be included here?
            // {
              default = self.apps.${system}.emuhawk-latest-bin;
            }
          )
        ) self.packages;
    };
}
