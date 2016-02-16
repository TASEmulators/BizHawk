set PATH=%PATH%;C:\Program Files (x86)\git\bin;C:\Program Files\git\bin
set WORKDIR=.build-github-master
rmdir /s /q %WORKDIR%
mkdir %WORKDIR%
cd %WORKDIR%

rem http://stackoverflow.com/questions/13750182/git-how-to-archive-from-remote-repository-directly
git clone --depth=1 --single-branch --branch Release http://github.com/TASVideos/BizHawk.git .

cd dist
call BuildAndPackage_Release.bat

rem make sure we save the output!
move BizHawk.zip ..\..\BizHawk-Github-Master.zip
cd ..\..

rmdir /s /q %WORKDIR%