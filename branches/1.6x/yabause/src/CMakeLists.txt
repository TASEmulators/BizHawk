project(yabause)

include(CheckFunctionExists)
include(CheckIncludeFile)

set(yabause_SOURCES
	bios.c
	cdbase.c cheat.c coffelf.c cs0.c cs1.c cs2.c
	debug.c
	error.c
	m68kcore.c m68kd.c memory.c movie.c
	netlink.c
	osdcore.c
	peripheral.c profile.c
	scu.c sh2core.c sh2d.c sh2idle.c sh2int.c sh2trace.c smpc.c snddummy.c
	titan/titan.c
	vdp1.c vdp2.c vdp2debug.c vidogl.c vidshared.c vidsoft.c
	yabause.c ygl.c yglshader.c)

# new SCSP
option(YAB_USE_SCSP2 "Use the new SCSP implementation.")
if (YAB_USE_SCSP2)
	add_definitions(-DUSE_SCSP2=1)
	set(yabause_SOURCES ${yabause_SOURCES} scsp2.c)
else()
	set(yabause_SOURCES ${yabause_SOURCES} scsp.c)
endif()

# disable strdup warning in MSVC
if (MSVC)
	add_definitions(/wd4996)
endif ()

# math library
if (UNIX)
    set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} "m")
endif()

# Bigendian
include(TestBigEndian)
test_big_endian(WORDS_BIGENDIAN)
if (WORDS_BIGENDIAN)
	add_definitions(-DWORDS_BIGENDIAN=1)
endif (WORDS_BIGENDIAN)

include(CheckCSourceCompiles)

# variadic macros
check_c_source_compiles("#define MACRO(...) puts(__VA_ARGS__)
	int main(int argc, char ** argv) { MACRO(\"foo\"); }"
	VARIADIC_MACROS_OK)
if (VARIADIC_MACROS_OK)
	add_definitions(-DHAVE_C99_VARIADIC_MACROS=1)
endif (VARIADIC_MACROS_OK)

# gettimeofday
check_function_exists(gettimeofday GETTIMEOFDAY_OK)
if (GETTIMEOFDAY_OK)
    add_definitions(-DHAVE_GETTIMEOFDAY=1)
endif ()

# floorf
set(CMAKE_REQUIRED_LIBRARIES ${CMAKE_REQUIRED_LIBRARIES} "-lm")
check_function_exists(floorf FLOORF_OK)
if (FLOORF_OK)
	add_definitions(-DHAVE_FLOORF=1)
endif ()

# sys/time.h
check_include_file("sys/time.h" SYSTIME_OK)
if (SYSTIME_OK)
	add_definitions(-DHAVE_SYS_TIME_H=1)
endif()

# Find stdint.h
check_include_file("stdint.h" STDINT_H_FOUND)
if (STDINT_H_FOUND)
	add_definitions(-DHAVE_STDINT_H=1)
endif()

# OpenGL
option(YAB_WANT_OPENGL "use OpenGL for video output (most ports require it)" ON)
if (YAB_WANT_OPENGL)
	include(FindOpenGL)
	if (OPENGL_FOUND)
		add_definitions(-DHAVE_LIBGL=1)
		set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${OPENGL_LIBRARIES})

		include(FindGLUT)
		if (GLUT_FOUND)
			include_directories(${GLUT_INCLUDE_DIR})
			add_definitions(-DHAVE_LIBGLUT=1)
			set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${GLUT_LIBRARIES})
		endif()

		# glXGetProcAddress
		set(CMAKE_REQUIRED_LIBRARIES ${CMAKE_REQUIRED_LIBRARIES} ${OPENGL_LIBRARIES})
		check_function_exists(glXGetProcAddress GLXGETPROCADDRESS_OK)
		if (GLXGETPROCADDRESS_OK)
			add_definitions(-DHAVE_GLXGETPROCADDRESS=1)
		endif()
	endif(OPENGL_FOUND)
endif (YAB_WANT_OPENGL)

# SDL
option(YAB_WANT_SDL "use SDL cores if available" ON)
if (YAB_WANT_SDL)
	include(FindSDL)
	if (SDL_FOUND)
		add_definitions(-DHAVE_LIBSDL=1)
		include_directories(${SDL_INCLUDE_DIR})
		set(yabause_SOURCES ${yabause_SOURCES} persdljoy.c sndsdl.c)
		set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${SDL_LIBRARY})
	endif (SDL_FOUND)
endif (YAB_WANT_SDL)

# OpenAL
option(YAB_WANT_OPENAL "use OpenAL sound core if available" ON)
if (YAB_WANT_OPENAL)
	include(FindOpenAL)
	if (OPENAL_FOUND)
		find_package(Threads)
		add_definitions(-DHAVE_LIBAL=1)
		include_directories(${OPENAL_INCLUDE_DIR})
		set(yabause_SOURCES ${yabause_SOURCES} sndal.c)
		set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${OPENAL_LIBRARY} ${CMAKE_THREAD_LIBS_INIT})
	endif (OPENAL_FOUND)
endif (YAB_WANT_OPENAL)

# mini18n
find_path(MINI18N_INCLUDE_DIR mini18n.h)
find_library(MINI18N_LIBRARY mini18n)
if (NOT MINI18N_INCLUDE_DIR STREQUAL "MINI18N_INCLUDE_DIR-NOTFOUND" AND NOT MINI18N_LIBRARY STREQUAL "MINI18N_LIBRARY-NOTFOUND")
	set(MINI18N_FOUND TRUE)
	include_directories(${MINI18N_INCLUDE_DIR})
	add_definitions(-DHAVE_LIBMINI18N=1)
	set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${MINI18N_LIBRARY})
endif (NOT MINI18N_INCLUDE_DIR STREQUAL "MINI18N_INCLUDE_DIR-NOTFOUND" AND NOT MINI18N_LIBRARY STREQUAL "MINI18N_LIBRARY-NOTFOUND")

if (MINI18N_FOUND)
	if (UNIX)
		add_definitions(-DYTSDIR=\"${CMAKE_INSTALL_PREFIX}/share/${YAB_PACKAGE}/yts\")
	elseif (WIN32)
		add_definitions(-DYTSDIR=\"trans\")
	endif()
endif()

# APPLE // not necessary mac os x, but i don't care ;)
if (APPLE)
	FIND_LIBRARY(COREFOUNDATION_LIBRARY NAMES CoreFoundation )
	FIND_LIBRARY(IOKIT_LIBRARY NAMES IOKit )
	set(yabause_SOURCES ${yabause_SOURCES} macjoy.c permacjoy.c cd-macosx.c sndmac.c)
	set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${COREFOUNDATION_LIBRARY} ${IOKIT_LIBRARY})

	check_function_exists(glBindRenderbuffer HAVE_FBO)
	if (HAVE_FBO)
		add_definitions(-DHAVE_FBO=1)
	endif()
endif (APPLE)

# Visual Studio
if (MSVC)
        # Find DDK
        if (EXISTS "$ENV{SYSTEMDRIVE}/WINDDK/3790.1830/")
		set(DDK_DIR "$ENV{SYSTEMDRIVE}/WINDDK/3790.1830/")
        elseif (EXISTS "$ENV{SYSTEMDRIVE}/WINDDK/6000/")
		set(DDK_DIR "$ENV{SYSTEMDRIVE}/WINDDK/6000/")
        elseif (EXISTS "$ENV{SYSTEMDRIVE}/WINDDK/7600.16385.0/")
		set(DDK_DIR "$ENV{SYSTEMDRIVE}/WINDDK/7600.16385.0/")
        endif (EXISTS "$ENV{SYSTEMDRIVE}/WINDDK/3790.1830/")

	add_definitions(-DHAVE_C99_VARIADIC_MACROS -D_CRT_SECURE_NO_WARNINGS -DC68K_NO_JUMP_TABLE 
			-D_UNICODE -DUNICODE)
endif (MSVC)

# Windows ddk
if (WIN32)
	option(YAB_WANT_DDK "Use the real DDK instead of the built-in one")
	if(YAB_WANT_DDK)
		# Find ntddcdrm.h
		find_path(ntddcdrm_INCLUDE_DIR ntddcdrm.h
			PATHS "${DDK_DIR}" "${DDK_DIR}/inc" PATH_SUFFIXES ddk api)

		if (ntddcdrm_INCLUDE_DIR)
			include_directories(${ntddcdrm_INCLUDE_DIR})
			message(STATUS "Found ntddcdrm.h: ${ntddcdrm_INCLUDE_DIR}")
			add_definitions(-DHAVE_NTDDCDRM=1)
		else (ntddcdrm_INCLUDE_DIR)
			message(STATUS "Could not find ntddcdrm.h")
		endif (ntddcdrm_INCLUDE_DIR)
	endif(YAB_WANT_DDK)

	set(yabause_SOURCES ${yabause_SOURCES} cd-windows.c)

        option(YAB_WANT_DIRECTSOUND "use DirectX sound core if available")
        option(YAB_WANT_DIRECTINPUT "use DirectX input core if available")

	if (YAB_WANT_DIRECTSOUND OR YAB_WANT_DIRECTINPUT)
		find_path(DirectX_INCLUDE_DIR dxerr9.h "$ENV{DXSDK_DIR}/Include")
		if (NOT DirectX_INCLUDE_DIR)
			find_path(DirectX_INCLUDE_DIR "dxerr.h" "$ENV{DXSDK_DIR}/Include")
			if (DirectX_INCLUDE_DIR)
				set(DXERRH_IS_BROKEN 1 CACHE INTERNAL "dxerr is broken")
        		endif (DirectX_INCLUDE_DIR)
		endif(NOT DirectX_INCLUDE_DIR)

		find_library(DirectX_GUID_LIBRARY dxguid "$ENV{DXSDK_DIR}/Lib/x86" "$ENV{DXSDK_DIR}/Lib")
		if (YAB_WANT_DIRECTINPUT)
			find_library(DirectX_INPUT8_LIBRARY dinput8 "$ENV{DXSDK_DIR}/Lib/x86" "$ENV{DXSDK_DIR}/Lib")
		endif(YAB_WANT_DIRECTINPUT)
		if (YAB_WANT_DIRECTSOUND)
			find_library(DirectX_SOUND_LIBRARY dsound "$ENV{DXSDK_DIR}/Lib/x86" "$ENV{DXSDK_DIR}/Lib")
		endif(YAB_WANT_DIRECTSOUND)

		if (DXERRH_IS_BROKEN)
			find_library(DirectX_ERR_LIBRARY dxerr "$ENV{DXSDK_DIR}/Lib/x86" "$ENV{DXSDK_DIR}/Lib")
		elseif(MINGW)
			find_library(DirectX_ERR_LIBRARY dxerr8 "$ENV{DXSDK_DIR}/Lib/x86" "$ENV{DXSDK_DIR}/Lib")
		else()
			find_library(DirectX_ERR_LIBRARY dxerr9 "$ENV{DXSDK_DIR}/Lib/x86" "$ENV{DXSDK_DIR}/Lib")
		endif()

		if (DirectX_INCLUDE_DIR AND DirectX_GUID_LIBRARY AND DirectX_ERR_LIBRARY)
			set(DIRECTX_FOUND "found")
			include_directories(${DirectX_INCLUDE_DIR})
			set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${DirectX_GUID_LIBRARY} ${DirectX_ERR_LIBRARY})

			if (DirectX_SOUND_LIBRARY AND DirectX_INPUT8_LIBRARY)
				add_definitions(-DHAVE_DIRECTINPUT)
				set(yabause_SOURCES ${yabause_SOURCES} snddx.c)
				set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${DirectX_INPUT8_LIBRARY})
			endif (DirectX_SOUND_LIBRARY AND DirectX_INPUT8_LIBRARY)
        		if (YAB_WANT_DIRECTSOUND AND DirectX_SOUND_LIBRARY)
				add_definitions(-DHAVE_DIRECTSOUND)
				set(yabause_SOURCES ${yabause_SOURCES} perdx.c)
				set(YABAUSE_LIBRARIES ${YABAUSE_LIBRARIES} ${DirectX_SOUND_LIBRARY})
			endif (YAB_WANT_DIRECTSOUND AND DirectX_SOUND_LIBRARY)

			if (DXERRH_IS_BROKEN)
				add_definitions(-DDXERRH_IS_BROKEN)
				message(STATUS "Using work-around for dxerr.h")
			endif(DXERRH_IS_BROKEN)
		endif (DirectX_INCLUDE_DIR AND DirectX_GUID_LIBRARY AND DirectX_ERR_LIBRARY)
	endif (YAB_WANT_DIRECTSOUND OR YAB_WANT_DIRECTINPUT)
endif (WIN32)

if (WII)
	set(CMAKE_C_FLAGS "-mrvl -mcpu=750 -meabi -mhard-float")
	add_definitions(-DGEKKO=1)
	# that shouldn't be hardcoded, either use an ENV variable or try to detect it...
	include_directories(/opt/devkitpro/libogc/include/)
endif()

option(YAB_WANT_ARM7 "Build a binary with arm7 support")

# SH2 dynamic recompiler
message(STATUS "CMAKE_SYSTEM_NAME ${CMAKE_SYSTEM_NAME}")
message(STATUS "CMAKE_SYSTEM_PROCESSOR ${CMAKE_SYSTEM_PROCESSOR}")
option(SH2_DYNAREC "SH2 dynamic recompiler" ON)
if (SH2_DYNAREC)
	if("${CMAKE_SYSTEM_NAME}" STREQUAL "Linux")
		if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "i686")
			enable_language(ASM-ATT)
			set(yabause_SOURCES ${yabause_SOURCES}
				sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_x86.s)
			set_source_files_properties(sh2_dynarec/sh2_dynarec.c PROPERTIES COMPILE_FLAGS "-Wno-pointer-to-int-cast -Wno-int-to-pointer-cast")
			add_definitions(-DSH2_DYNAREC=1)
		endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "i686")
		if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "x86_64")
			enable_language(ASM-ATT)
			set(yabause_SOURCES ${yabause_SOURCES}
				sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_x64.s)
			set_source_files_properties(sh2_dynarec/sh2_dynarec.c PROPERTIES COMPILE_FLAGS "-Wno-pointer-to-int-cast -Wno-int-to-pointer-cast")
			add_definitions(-DSH2_DYNAREC=1)
		endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "x86_64")
		if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv5tel")
			enable_language(ASM-ATT)
			set(yabause_SOURCES ${yabause_SOURCES}
				sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_arm.s)
			set_source_files_properties(sh2_dynarec/sh2_dynarec.c PROPERTIES COMPILE_FLAGS "-Wno-pointer-to-int-cast -Wno-int-to-pointer-cast")
			add_definitions(-DSH2_DYNAREC=1)
		endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv5tel")
		if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv7l")
			enable_language(ASM-ATT)
			set(yabause_SOURCES ${yabause_SOURCES}
				sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_arm.s)
			set_source_files_properties(sh2_dynarec/sh2_dynarec.c PROPERTIES COMPILE_FLAGS "-Wno-pointer-to-int-cast -Wno-int-to-pointer-cast")
			add_definitions(-DSH2_DYNAREC=1 -DHAVE_ARMv6=1 -DHAVE_ARMv7=1)
		endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv7l")
		if (ANDROID)
			enable_language(ASM-ATT)
			set(yabause_SOURCES ${yabause_SOURCES}
				sh2_dynarec/sh2_dynarec.c sh2_dynarec/linkage_arm.s)
			set_source_files_properties(sh2_dynarec/sh2_dynarec.c PROPERTIES COMPILE_FLAGS "-Wno-pointer-to-int-cast -Wno-int-to-pointer-cast")
			add_definitions(-DSH2_DYNAREC=1)
			add_definitions(-DANDROID=1)
			if (YAB_WANT_ARM7)
				add_definitions(-DHAVE_ARMv6=1 -DHAVE_ARMv7=1)
			endif()
		endif ()
	endif("${CMAKE_SYSTEM_NAME}" STREQUAL "Linux")
endif (SH2_DYNAREC)

# c68k
option(YAB_WANT_C68K "enable c68k compilation" ON)
if (YAB_WANT_C68K)
	include(ExternalProject)
	ExternalProject_Add(c68kinc
		DOWNLOAD_COMMAND ""
		SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/c68k
		CMAKE_GENERATOR "${CMAKE_GENERATOR}"
		INSTALL_COMMAND ""
		BINARY_DIR ${CMAKE_CURRENT_BINARY_DIR}/c68k
	)

	add_definitions(-DHAVE_C68K=1)
	include_directories(${CMAKE_CURRENT_BINARY_DIR}/c68k)
	set(yabause_SOURCES ${yabause_SOURCES} c68k/c68kexec.c c68k/c68k.c c68k/gen68k.c m68kc68k.c)
	if (MSVC)
		set_source_files_properties(c68k/c68kexec.c PROPERTIES COMPILE_FLAGS "/Od /wd4146")
	else()
		set_source_files_properties(c68k/c68kexec.c PROPERTIES COMPILE_FLAGS "-O0")
	endif()
endif(YAB_WANT_C68K)

# q68
option(YAB_WANT_Q68 "enable q68 compilation" OFF)
if (YAB_WANT_Q68)
	add_definitions(-DHAVE_Q68=1)
	set(yabause_SOURCES ${yabause_SOURCES}
		m68kq68.c q68/q68.c q68/q68-core.c q68/q68-disasm.c
		q68/q68-const.h q68/q68.h q68/q68-internal.h q68/q68-jit.h q68/q68-jit-psp.h q68/q68-jit-x86.h)
endif()

# *DEBUG
set(YAB_DEBUG "" CACHE STRING "List of enabled debug information") 
foreach(DEBUG IN LISTS YAB_DEBUG)
	if (${DEBUG} STREQUAL "main")
		add_definitions(-DDEBUG=1)
	elseif (${DEBUG} STREQUAL "cd")
		add_definitions(-DCDDEBUG=1)
	elseif (${DEBUG} STREQUAL "idle")
		add_definitions(-DIDLE_DETECT_VERBOSE=1)
	else (${DEBUG} STREQUAL "main")
		string(TOUPPER ${DEBUG} UPDEBUG)
		add_definitions(-D${UPDEBUG}_DEBUG=1)
	endif (${DEBUG} STREQUAL "main")
endforeach(DEBUG)

# Network
option(YAB_NETWORK "Enable network")
if (YAB_NETWORK)
	add_definitions(-DUSESOCKET=1)
endif()

# Peripheral key name
option(YAB_PERKEYNAME "Try to display key names instead of cryptic values" OFF)
if (YAB_PERKEYNAME)
	add_definitions(-DPERKEYNAME=1)
endif()

option(YAB_PORT_OSD "Let ports provides their own OSD core list" OFF)
if (YAB_PORT_OSD)
	add_definitions(-DYAB_PORT_OSD=1)
endif()

# Exec from cache
option(YAB_EXEC_FROM_CACHE "Allow code execution from 0xC0000000" OFF)
if (YAB_EXEC_FROM_CACHE)
	add_definitions(-DEXEC_FROM_CACHE=1)
endif()

# Optimized DMA
option(YAB_OPTIMIZED_DMA "Use optimized DMA when possible" OFF)
if (YAB_OPTIMIZED_DMA)
	add_definitions(-DOPTIMIZED_DMA=1)
endif()

# Yabause Arch
if (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
	add_definitions(-DARCH_IS_MACOSX=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-dummy.c)
elseif (${CMAKE_SYSTEM_NAME} MATCHES "FreeBSD")
	add_definitions(-DARCH_IS_FREEBSD=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-dummy.c cd-freebsd.c)
elseif (${CMAKE_SYSTEM_NAME} MATCHES "Linux")
	add_definitions(-DARCH_IS_LINUX=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-linux.c cd-linux.c)

	check_include_file("linux/joystick.h" LINUX_HAS_JOYSTICK)
	if (LINUX_HAS_JOYSTICK)
		set(yabause_SOURCES ${yabause_SOURCES} perlinuxjoy.c)
	endif()

	check_c_source_compiles("
		#include <linux/cdrom.h>
		int main(int argc, char ** argv) { int i = CDSL_CURRENT; }
	" LINUX_CDROM_H_OK)
	if (NOT LINUX_CDROM_H_OK)
       		add_definitions(-DLINUX_CDROM_H_IS_BROKEN)
	endif (NOT LINUX_CDROM_H_OK)
elseif (${CMAKE_SYSTEM_NAME} MATCHES "NetBSD")
	add_definitions(-DARCH_IS_NETBSD=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-dummy.c cd-netbsd.c)
elseif (${CMAKE_SYSTEM_NAME} MATCHES "OpenBSD")
	add_definitions(-DARCH_IS_NETBSD=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-dummy.c cd-netbsd.c)
elseif (${CMAKE_SYSTEM_NAME} MATCHES "Windows")
	add_definitions(-DARCH_IS_WINDOWS=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-dummy.c)
else ()
	add_definitions(-DUNKNOWN_ARCH=1)
	set(yabause_SOURCES ${yabause_SOURCES} thr-dummy.c)
endif ()

set(YAB_OPTIMIZATION "-O3" CACHE STRING "Override optimization level")

if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "i686")
	set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${YAB_OPTIMIZATION} -march=i686 -msse")
endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "i686")
if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "x86_64")
	set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${YAB_OPTIMIZATION}")
endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "x86_64")
if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv5tel")
	set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${YAB_OPTIMIZATION}")
endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv5tel")
if("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv7l")
       set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${YAB_OPTIMIZATION}")
endif("${CMAKE_SYSTEM_PROCESSOR}" STREQUAL "armv7l")
if(ANDROID)
	set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${YAB_OPTIMIZATION}")
endif()

# Warnings defined to know when we're breaking compilation with MSVC
if (CMAKE_COMPILER_IS_GNUCC)
	set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wdeclaration-after-statement")
endif ()

if (MSVC)
	set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /wd4018 /wd4244")
	set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /wd4244")
endif ()

add_definitions(-DPACKAGE=\"${YAB_PACKAGE}\")
add_definitions(-DVERSION=\"${YAB_VERSION}\")

add_library(yabause ${yabause_SOURCES})

if (YAB_WANT_C68K)
	add_dependencies(yabause c68kinc)
endif(YAB_WANT_C68K)

macro(yab_port_start)
	if (YAB_PORT_BUILT AND NOT YAB_MULTIBUILD)
		return()
	endif ()
endmacro(yab_port_start)

macro(yab_port_stop)
	set(YAB_PORT_BUILT TRUE PARENT_SCOPE)
endmacro(yab_port_stop)

macro(yab_port_success YAB_TARGET)
	if (NOT YAB_MULTIBUILD)
		set_target_properties(${YAB_TARGET} PROPERTIES OUTPUT_NAME yabause)
		set(YAB_PORT_NAME "yabause")
	else ()
		set(YAB_PORT_NAME ${YAB_TARGET})
	endif ()
	set(YAB_PORT_BUILT TRUE PARENT_SCOPE)
endmacro(yab_port_success)

set(YAB_MAN_DIR "share/man")
if (NOT $ENV{PKGMANDIR} STREQUAL "")
    set(YAB_MAN_DIR $ENV{PKGMANDIR})
endif ()

option(YAB_MULTIBUILD "Choose wether to build all ports or only a single one")
set(YAB_PORT_BUILT FALSE)
set(YAB_PORTS "gtk;qt;windows;dreamcast;wii;carbon;cocoa" CACHE STRING "List of ports to build")
foreach(PORT IN LISTS YAB_PORTS)
	add_subdirectory(${PORT})
endforeach(PORT)

# this is stupid, but CMake automatic definitions are based on variables...
if (YAB_WANT_C68K)
	set(HAVE_C68K ON)
endif()
if (YAB_WANT_Q68)
	set(HAVE_Q68 ON)
endif()
configure_file(${CMAKE_CURRENT_SOURCE_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
