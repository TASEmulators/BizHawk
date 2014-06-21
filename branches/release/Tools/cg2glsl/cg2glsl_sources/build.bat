rem http://http.developer.nvidia.com/Cg/cgc.html
rem https://developer.nvidia.com/cg-toolkit
rem https://github.com/Themaister/RetroArch/blob/master/tools/cg2glsl.py
rem https://github.com/IronLanguages/main/blob/master/Languages/IronPython/Public/Tools/Scripts/pyc.py

"C:\Program Files (x86)\IronPython 2.7\ipy" "C:\Program Files (x86)\IronPython 2.7\tools\scripts\pyc.py" /embed /standalone /target:exe /main:cg2glsl.py lib\_abcoll.py lib\_weakrefset.py lib\abc.py lib\bisect.py lib\collections.py lib\genericpath.py lib\heapq.py lib\keyword.py lib\linecache.py lib\ntpath.py lib\os.py lib\stat.py lib\subprocess.py lib\threading.py lib\traceback.py lib\types.py lib\UserDict.py lib\warnings.py
move /y cg2glsl.exe ..
copy /y cgc.exe ..
copy /y cg.dll ..
copy /y cgGL.dll ..