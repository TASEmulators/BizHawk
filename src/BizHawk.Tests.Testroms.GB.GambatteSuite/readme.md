See the readme in the main project, `../BizHawk.Tests.Testroms.GB`.

On Linux, run `/res/download_from_ci.sh` to automatically download and extract the CI artifacts containing the necessary testroms.
On Windows, run the same script in WSL, or do it manually (because Yoshi can't be bothered porting the script to PowerShell).
The expected directory structure is:
```
../BizHawk.Tests.Testroms.GB/res
├─ fw
│   ├─ GB__World__DMG.bin
│   └─ GBC__World__CGB.bin
└─ Gambatte-testroms_artifact
```

> This test suite is huge and takes a **really long time** to run. Like several hours.

Summary of `BIZHAWKTEST_RUN_KNOWN_FAILURES=1 ./run_tests_release.sh` should read 22643 passed / 2332 skipped / 0 failed.
