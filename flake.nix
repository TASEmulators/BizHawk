{
  description = "EmuHawk is a multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).";
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs?ref=24.05";
  };
  outputs =
    inputs@{ self, nixpkgs, ... }:
    with builtins;
    let
      inherit (nixpkgs) lib;
      systems = [
        # this is currently the only supported system, according to https://github.com/TASEmulators/BizHawk/issues/1430#issue-396452488
        "x86_64-linux"
      ];
      nixpkgsFor = lib.genAttrs systems (
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
        (lib.filterAttrs (name: val: lib.isDerivation val) (import ./default.nix { inherit system pkgs; }));
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
        let
          avail = import ./default.nix { inherit system pkgs; };
          mkShellCustom =
            drv:
            pkgs.mkShell {
              packages = with pkgs; [
                git
                powershell
              ];
              inputsFrom = [ drv ];
              shellHook = avail.shellHook drv;
            };
        in
        {
          bizhawkAssemblies-latest = mkShellCustom avail.bizhawkAssemblies-latest;
          discohawk-latest = self.devShells.${system}.bizhawkAssemblies-latest;
          emuhawk-latest = self.devShells.${system}.bizhawkAssemblies-latest;
          default = pkgs.mkShell {
            packages = [ avail.emuhawk.hawkSourceInfo.dotnet-sdk ];
            inputsFrom = [ self.devShells.${system}.emuhawk-latest ];
          };
        }
      ) nixpkgsFor;
    };
}
