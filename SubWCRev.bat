@echo off

set TEMPFILE="%TEMP%-%RANDOM%-%RANDOM%-%RANDOM%-%RANDOM%"
set SVNREV="%~1properties\svnrev.cs"

rem generate a wvnrev with sed using no revision number, in case svn isnt available
@"%~1..\sed.exe" s/\$WCREV\$/0/ < "%~1properties\svnrev_template" > %TEMPFILE%

rem try generating svnrev from svn now. this will fail if svn is nonexistent, so...
@"%~1..\SubWCRev.exe" "%~1\.." "%~1properties\svnrev_template" %TEMPFILE% > nul

rem ... ignore the error
SET ERRORLEVEL=0

rem if we didnt even have a svnrev, then go ahead and copy it
if not exist %SVNREV% (
   @copy /y %TEMPFILE% %SVNREV%
) else if exist %TEMPFILE% (
  rem check to see whether its any different, so we dont touch unchanged files
  fc /b %TEMPFILE% %SVNREV% > nul
  if %ERRORLEVEL% neq 0 (
    echo Updated svnrev file
	@copy /y %TEMPFILE% %SVNREV%
  ) else (
    echo Not touching unchanged svnrev file
  )
) else (
  echo Ran into a weird error writing subwcrev output to tempfile: %TEMPFILE%
)

del %TEMPFILE%

rem always let build proceed
SET ERRORLEVEL=0