cmake_minimum_required(VERSION 3.12 FATAL_ERROR)

if(POLICY CMP0092)
	cmake_policy(SET CMP0092 NEW)
endif()

project(sameboy C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS ON)
set(CMAKE_C_VISIBILITY_PRESET hidden)

if(MSVC)
	# max warnings
	add_compile_options(/W4)

	# ignore some warnings on clang
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		add_compile_options(-Wno-unused-parameter -Wno-sign-compare -Wno-missing-braces -Wno-missing-field-initializers)
	endif()

	# all files are utf8
	add_compile_options(/utf-8)

	# max conformance mode
	add_compile_options(/permissive-)
	add_compile_options(/volatile:iso)
	add_compile_options(/fp:precise)
else()
	# max warnings
	add_compile_options(-Wall -Wextra)
	# ignore a few warnings
	add_compile_options(-Wno-unused-parameter -Wno-sign-compare -Wno-missing-braces -Wno-missing-field-initializers)

	# strip in release, optimize for gdb usage in debug
	add_link_options($<$<CONFIG:RELEASE>:-s>)
	add_compile_options($<$<CONFIG:DEBUG>:-ggdb>)

	# use lld for clang (needed if doing lto)
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		add_link_options(-fuse-ld=lld)
	endif()
endif()

# make sure we have a "lib" prefix (currently expected across all platforms)
set(CMAKE_STATIC_LIBRARY_PREFIX "lib")
set(CMAKE_SHARED_LIBRARY_PREFIX "lib")

set(CORE_DIR ${CMAKE_SOURCE_DIR}/libsameboy/Core)

add_library(
	core
	STATIC
	${CORE_DIR}/apu.c
	${CORE_DIR}/apu.h
	${CORE_DIR}/camera.c
	${CORE_DIR}/camera.h
	${CORE_DIR}/defs.h
	${CORE_DIR}/display.c
	${CORE_DIR}/display.h
	${CORE_DIR}/gb.c
	${CORE_DIR}/gb.h
	${CORE_DIR}/joypad.c
	${CORE_DIR}/joypad.h
	${CORE_DIR}/mbc.c
	${CORE_DIR}/mbc.h
	${CORE_DIR}/memory.c
	${CORE_DIR}/memory.h
	${CORE_DIR}/model.h
	${CORE_DIR}/printer.c
	${CORE_DIR}/printer.h
	${CORE_DIR}/random.c
	${CORE_DIR}/random.h
	${CORE_DIR}/rumble.c
	${CORE_DIR}/rumble.h
	${CORE_DIR}/save_state.c
	${CORE_DIR}/save_state.h
	${CORE_DIR}/sgb.c
	${CORE_DIR}/sgb.h
	${CORE_DIR}/sm83_cpu.c
	${CORE_DIR}/sm83_cpu.h
	${CORE_DIR}/timing.c
	${CORE_DIR}/timing.h
)

target_compile_definitions(core PUBLIC _USE_MATH_DEFINES _GNU_SOURCE _CRT_SECURE_NO_WARNINGS)
target_compile_definitions(core PUBLIC GB_DISABLE_DEBUGGER GB_DISABLE_CHEATS GB_DISABLE_TIMEKEEPING GB_DISABLE_REWIND GB_VERSION="BizHawk")
target_compile_definitions(core PRIVATE GB_INTERNAL)
target_include_directories(core PUBLIC ${CORE_DIR})

if(MSVC)
	target_include_directories(core PUBLIC ${CMAKE_SOURCE_DIR}/libsameboy/Windows)
endif()

add_library(
	sameboy
	SHARED
	${CMAKE_SOURCE_DIR}/BizInterface.c
	${CMAKE_SOURCE_DIR}/blip_buf.c
)

target_link_libraries(sameboy PRIVATE core)

add_custom_command(
	TARGET sameboy
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:sameboy> ${CMAKE_SOURCE_DIR}/../../Assets/dll
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:sameboy> ${CMAKE_SOURCE_DIR}/../../output/dll
)

if(MSVC)
	set(PDB_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/../../output/dll)
endif()
