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
    };
}
