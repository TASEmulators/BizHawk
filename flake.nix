{
  description = "EmuHawk is a multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).";
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/release-24.05";
  };
  outputs =
    inputs@{ self, nixpkgs, ... }:
    with builtins;
    let
      std = nixpkgs.lib;
      systems = [
        # this is currently the only supported system, according to https://github.com/TASEmulators/BizHawk/issues/1430#issue-396452488
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
      # import the derivations from default.nix for the given system & package set
      importDefaultDerivationsWith =
        system: pkgs:
        # ./default.nix outputs some non-derivation attributes, so we have to filter those out
        (std.filterAttrs (name: val: std.isDerivation val) (import ./default.nix { inherit system pkgs; }));
    in
    {
      packages = mapAttrs (
        system: pkgs:
        (importDefaultDerivationsWith system pkgs)
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
      overlays.default =
        final: prev:
        # filter derivations to only include `emuhawk` and `discohawk` ones (i.e. excluding `bizhawkAssemblies`)
        std.filterAttrs (name: pkg: (std.hasPrefix "emuhawk" name) || (std.hasPrefix "discohawk" name)) (
          # import `default.nix` with the overlayed package set
          # (i don't know the circumstances under which `final` wouldn't have a `system` attribute, but we may as well account for it)
          importDefaultDerivationsWith (final.system or "") final
        );
    };
}
