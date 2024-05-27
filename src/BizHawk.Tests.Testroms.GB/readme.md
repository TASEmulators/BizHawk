Before building, testroms and firmware need to be placed under `/res` in this project.
You *should* be able to omit any suite or firmware and the relevant cases will be skipped.
Firmware needs to be manually copied into a `fw` dir;
testroms need to be copied into a separate dir per suite, with a hierarchy matching the CI artifacts of [this repo](https://gitlab.com/tasbot/libre-roms-ci).
On Linux, run `/res/download_from_ci.sh` to automatically download and extract said artifacts.
On Windows, run the same script in WSL, or do it manually (because Yoshi can't be bothered porting the script to PowerShell).
All told, the expected directory structure is:
```
res
├─ BullyGB_artifact
├─ cgb-acid-hell_artifact
├─ cgb-acid2_artifact
├─ dmg-acid2_artifact
├─ fw
│   ├─ GB__World__DMG.bin
│   └─ GBC__World__CGB.bin
├─ mealybug-tearoom-tests_artifact
└─ rtc3test_artifact
```

As with EmuHawk, the target framework and configuration for all the BizHawk project deps is dictated by this project. That means .NET Standard 2.0, or .NET 6 if the project supports it.
To build and run the tests in `Release` configuration (or `Debug` if you need that for some reason):
- On Linux, run `run_tests_release.sh` or `run_tests_debug.sh`.
- On Windows, pass `-c Release` to `dotnet test` (must `cd` to this project). Omitting `-c` will use `Debug`.

> You can at this point run the tests, but you should probably keep reading to see your options.

To run only some suites, comment out applications of the `[DataTestMethod]` attribute in the source. (Or applications of `[TestClass]`.)
You can also disable individual test cases programmatically by modifying `TestUtils.ShouldIgnoreCase`—
note that "ignored" here means cases are completely removed, and do not count as "skipped".

By default, known failures are counted as "skipped" *without actually running them*.
Set the env. var `BIZHAWKTEST_RUN_KNOWN_FAILURES=1` to run them as well. They will count as "skipped" if they fail, or "failed" if they succeed unexpectedly.

On Linux, all cases for unavailable cores (N/A currently) are counted as "skipped".

Screenshots may be saved under `/test_output/<suite>` **in the repo**.
For ease of typing, a random prefix is chosen for each case e.g. `DEADBEEF_{expected,actual}_*.png`. This is included in stdout (Windows users, see below for how to enable stdout).

The env. var `BIZHAWKTEST_SAVE_IMAGES` determines when to save screenshots (usually an expect/actual pair) to disk.
- With `BIZHAWKTEST_SAVE_IMAGES=all`, all screenshots are saved.
- With `BIZHAWKTEST_SAVE_IMAGES=failures` (the default), only screenshots of failed tests are saved.
- With `BIZHAWKTEST_SAVE_IMAGES=none`, screenshots are never saved.

Test results are output using the logger(s) specified on the command-line.
(Without the `console` logger, the results are summarised in the console, but prints to stdout are not shown.)
- On Linux, the shell scripts add the `console` and `junit` (to file, for GitLab CI) loggers.
- On Windows, pass `-l "console;verbosity=detailed"` to `dotnet`.

> Note that the results and stdout for each test case are not printed immediately.
> Cases are grouped by test method, and once the set of test cases is finished executing, the outputs are sent to the console all at once.

Linux examples:
```sh
# default: simple regression testing, all test suites, saving failures to disk
./run_tests_release.sh

# every test from every suite, not saving anything to disk (as might be used in CI)
BIZHAWKTEST_RUN_KNOWN_FAILURES=1 BIZHAWKTEST_SAVE_IMAGES=none ./run_tests_release.sh
```

Windows examples:
```pwsh
# reminder that if you have WSL, you can use that to run /res/download_from_ci.sh first

# default: simple regression testing, all test suites, saving failures to disk
dotnet test -c Release -l "console;verbosity=detailed"

# same as Linux CI example
$Env:BIZHAWKTEST_RUN_KNOWN_FAILURES = 1
$Env:BIZHAWKTEST_SAVE_IMAGES = "all"
dotnet test -c Release -l "console;verbosity=detailed"
```

Summary of `BIZHAWKTEST_RUN_KNOWN_FAILURES=1 ./run_tests_release.sh` should read 202 passed / 168 skipped / 0 failed.
