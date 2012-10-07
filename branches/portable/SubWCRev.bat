@echo off

rem first, generate one with sed using no revision number, in case svn isnt available
"%~1..\sed.exe" s/\$WCREV\$/0/ < "%~1properties\svnrev_template" > "%~1properties\svnrev.cs"

rem try generating one from svn now. this will fail if svn is nonexistent, so...
"%~1..\SubWCRev.exe" "%~1\.." "%~1properties\svnrev_template" "%~1properties\svnrev.cs"

rem ... ignore the error
SET ERRORLEVEL=0
