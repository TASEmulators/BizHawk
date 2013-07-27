@echo off

set TEMPFILE="%TEMP%\BIZBUILD-SVN-%RANDOM%-%RANDOM%-%RANDOM%-%RANDOM%"
set SVNREV="%~1properties\svnrev.cs"

rem try generating svnrev from svn now. this will fail if svn is nonexistent, so...
"%~1..\SubWCRev.exe" "%~1\.." "%~1properties\svnrev_template" %TEMPFILE% > nul

rem generate a svnrev with sed using no revision number, in case svn isnt available
if not exist %TEMPFILE% (
    "%~1..\sed.exe" s/\$WCREV\$/0/ < "%~1properties\svnrev_template" > %TEMPFILE%
)

rem ... ignore the error
SET ERRORLEVEL=0



rem if we didnt even have a svnrev, then go ahead and copy it
if not exist %SVNREV% (
   copy /y %TEMPFILE% %SVNREV%
) else if exist %TEMPFILE% (
  rem check to see whether its any different, so we dont touch unchanged files
  fc /b %TEMPFILE% %SVNREV% > nul
  if ERRORLEVEL 0 (
    echo Not touching unchanged svnrev file
  )
  if ERRORLEVEL 1 (
    echo Updated svnrev file
    @copy /y %TEMPFILE% %SVNREV%
  )
  if ERRORLEVEL 2 (
    echo Updated svnrev file
    @copy /y %TEMPFILE% %SVNREV%
  )
) else (
  echo Ran into a weird error writing subwcrev output to tempfile: %TEMPFILE%
)

rem zero 27-jul-2013 - once upon a time this was commented out. why?
del %TEMPFILE%

rem always let build proceed
SET ERRORLEVEL=0