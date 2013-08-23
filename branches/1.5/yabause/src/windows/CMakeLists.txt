project(yabause-windows)

yab_port_start()

if (NOT WIN32)
	return()
endif(NOT WIN32)

add_definitions(-D_WIN32_IE=0x0500 -D_WIN32_WINNT=0x0500 -DUNICODE -D_UNICODE)

# DirectX
if (DIRECTX_FOUND)
	message(STATUS "Found DirectX SDK 9.0 or greater")
else (DIRECTX_FOUND)
	message(FATAL_ERROR "Could not find DirectX SDK 9.0 or greater")
endif (DIRECTX_FOUND)

set(yabause_windows_HEADERS
	aviout.h
	cd.h
	cheats.h
	custctl.h
	disasm.h
	hexedit.h
	hotkey.h
	ramwatch.h
	ram_search.h
	resource.h
	yuiwin.h
	cpudebug/yuidebug.h
	settings/settings.h)

set(yabause_windows_RESOURCES
	resource.rc)

set(yabause_windows_SOURCES
	aviout.c
	bup-manager.c
	cheats.c
	custctl.c
	disasm.c
	hexedit.c
	hotkey.cpp
	ramwatch.cpp
	ram_search.cpp
	yui.cpp
	cpudebug/debug-68k.c
	cpudebug/debug-scsp.c
	cpudebug/debug-scu.c
	cpudebug/debug-sh2.c
	cpudebug/debug-smpc.c
	cpudebug/debug-vdp1.c
	cpudebug/debug-vdp2.c
	cpudebug/yuidebug.c
	settings/settings.c
	settings/settings-basic.c
	settings/settings-input.c
	settings/settings-log.c
	settings/settings-netlink.c
	settings/settings-sound.c
	settings/settings-video.c)

if(MINGW)
	set(CMAKE_RC_COMPILE_OBJECT
		"<CMAKE_RC_COMPILER> <FLAGS> -O coff <DEFINES> -i <SOURCE> -o <OBJECT>")
endif(MINGW)

add_executable(yabause-windows WIN32 ${yabause_windows_SOURCES} ${yabause_windows_RESOURCES} ${yabause_windows_HEADERS})
target_link_libraries(yabause-windows yabause ${YABAUSE_LIBRARIES} ${PORT_LIBRARIES} comctl32 ${DIRECTX_LIBRARIES} vfw32)

if(MINGW AND CMAKE_CROSSCOMPILING)
	set_target_properties(yabause-windows PROPERTIES LINK_FLAGS "-static-libgcc -static-libstdc++")
endif()

yab_port_success(yabause-windows)
