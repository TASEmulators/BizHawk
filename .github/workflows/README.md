# GitHub Actions Workflows

This directory contains GitHub Actions workflows for BizHawkRafaelia.

## Workflows

### 1. `ci.yml` - Main CI/CD Pipeline
**Trigger**: Push, Pull Request, Manual  
**Purpose**: Build and test the main BizHawk solution

**Jobs**:
- `analyzer-build`: Build with analyzers (Debug + Release)
- `test`: Run tests on Windows and Linux
- `package`: Package output for Linux and Windows

**Note**: This workflow builds the core BizHawk solution. Rafaelia modules and Android APK are handled separately.

### 2. `apk-build.yml` - APK Build & Bug Mitigation (NEW)
**Trigger**: Push to main/develop/apk branches, Pull Request, Manual  
**Purpose**: Validate APK build process and run bug mitigation framework

**Jobs**:
- `bug-mitigation-analysis`: Run comprehensive bug detection (7 phases)
- `rafaelia-modules-build`: Build Rafaelia optimization modules
- `activation-validation`: Run ZIPRAF_OMEGA activation script
- `comprehensive-tests`: Execute 10-phase test suite
- `apk-build-check`: Validate Android project structure
- `documentation-check`: Verify all required docs exist
- `summary`: Generate workflow summary

**Artifacts**:
- `bug-mitigation-report`: Static analysis results
- `rafaelia-modules`: Compiled Rafaelia modules
- `activation-report`: Compliance validation results

**Note**: Full APK compilation requires Android SDK and is not performed in CI. The workflow validates prerequisites and scripts.

### 3. Other Workflows
- `quickernes.yml`: QuickerNES core builds
- `waterbox-cores.yml`: Waterbox core builds
- `waterbox.yml`: Waterbox toolchain builds
- `release.yml`: Release builds
- `nix-deps.yml`: Nix dependencies
- `mame.yml`: MAME core builds

## Rafaelia Framework Integration

The Rafaelia performance optimization framework includes:

**Modules** (built by `apk-build.yml`):
- `TesteDeMesaValidator.cs` - Runtime validation
- `MemoryLeakDetector.cs` - Memory leak detection
- `LagMitigator.cs` - Performance monitoring
- `ComplianceImplementation.cs` - ISO/IEEE/NIST/RFC compliance

**Scripts**:
- `bug-mitigation-framework.sh` - 7-phase static analysis
- `test-apk-build.sh` - 10-phase comprehensive tests
- `generate-apk.sh` - APK generation (requires Android SDK)
- `ativar.py` - ZIPRAF_OMEGA activation and validation

## Local Development

To run workflows locally:

```bash
# Bug mitigation analysis
./scripts/bug-mitigation-framework.sh

# Comprehensive tests
./scripts/test-apk-build.sh

# Build Rafaelia modules
dotnet build rafaelia/BizHawk.Rafaelia.csproj -c Release

# Activation validation
python3 rafaelia/ativar.py

# APK generation (requires Android SDK)
./generate-apk.sh
```

## Workflow Status

Check workflow status in GitHub Actions tab:
- ✅ Green: All jobs passed
- ⚠️ Yellow: Some jobs passed with warnings (acceptable for informational jobs)
- ❌ Red: Critical jobs failed

**Note**: `bug-mitigation-analysis` and `activation-validation` jobs may show warnings but won't fail the workflow. They're informational.

## Troubleshooting

### Common Issues

**"Rafaelia modules not in solution"**
- Expected behavior. Rafaelia is a separate project not included in main BizHawk.sln
- Built independently by `apk-build.yml` workflow

**"Android workload not installed"**
- Expected in CI. Full APK build requires Android SDK
- Workflow validates prerequisites only

**"Bug mitigation found issues"**
- Informational. Review `bug-mitigation-report` artifact
- Framework identifies potential issues but doesn't block build

**"Activation script returned warnings"**
- Normal. Compliance checks are ongoing validation
- Check `activation-report.json` artifact for details

## Adding New Workflows

When adding new workflows:

1. Place YAML file in `.github/workflows/`
2. Validate YAML syntax: `python3 -c "import yaml; yaml.safe_load(open('file.yml'))"`
3. Test locally if possible
4. Add documentation to this README
5. Consider artifact upload for important outputs

## CI/CD Philosophy

**Core BizHawk** (`ci.yml`):
- Strict: Warnings as errors
- Comprehensive: All platforms tested
- Fast-fail: Issues block merge

**Rafaelia/APK** (`apk-build.yml`):
- Informational: Issues logged but don't block
- Comprehensive: Multiple validation phases
- Artifact-focused: Results uploaded for review

This separation allows rapid iteration on Rafaelia framework while maintaining strict quality gates for core BizHawk.

---

**Last Updated**: November 23, 2025  
**Maintained By**: Rafael Melo Reis (rafaelmeloreisnovo)
