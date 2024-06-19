See the readme in the main project, `../BizHawk.Tests.Testroms.GB`.

On Linux, run `/res/download_from_ci.sh` to automatically download and extract the CI artifacts containing the necessary testroms.
On Windows, run the same script in WSL, or do it manually (because Yoshi can't be bothered porting the script to PowerShell).
For this project, the expected directory structure is:
```
res
└─ Gambatte-testroms_artifact
```

Note that firmware does not need to be copied here. They are taken from `../BizHawk.Tests.Testroms.GB/res/fw` if present.

> This test suite is huge and takes a **really long time** to run. Like several hours.

Summary of `BIZHAWKTEST_RUN_KNOWN_FAILURES=1 ./run_tests_release.sh` should read 22479 passed / 2496 skipped / 0 failed.
