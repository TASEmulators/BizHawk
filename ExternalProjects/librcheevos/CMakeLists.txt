cmake_minimum_required(VERSION 3.12 FATAL_ERROR)

if(POLICY CMP0092)
	cmake_policy(SET CMP0092 NEW)
endif()

project(rcheevos C)

set(CMAKE_C_STANDARD 90)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

if(MSVC)
	# silence dumb warnings over CRT functions be "unsafe"
	add_compile_definitions(_CRT_SECURE_NO_WARNINGS)

	# MSVC targets don't export all symbols unless this is on
	set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)

	# max warnings, treat as errors
	add_compile_options(/W4)
	add_compile_options(/WX)

	# ignore some warnings
	# this differs between clang-cl and cl
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		add_compile_options(
			-Wno-missing-field-initializers
			-Wno-unused-parameter
			-Wno-null-pointer-subtraction
		)
	else()
		add_compile_options(
			/wd4100 # "unreferenced formal parameter"
			/wd4701 # "potentially uninitialized local variable used" (detection here is just faulty)
		)
	endif()

	# all files are utf8
	add_compile_options(/utf-8)

	# max conformance mode
	add_compile_options(/permissive-)
	add_compile_options(/volatile:iso)
	add_compile_options(/fp:precise)

	# cmake will not insert a lib prefix for libraries on MSVC targets
	set(RC_TARGET librcheevos)
else()
	# max warnings, treat as errors
	add_compile_options(-Wall -Wextra)
	add_compile_options(-Werror)

	# ignore some warnings
	add_compile_options(
		-Wno-missing-field-initializers
		-Wno-unused-parameter
		-Wno-null-pointer-subtraction
		-Wno-implicit-fallthrough
		-Wno-array-bounds # interestingly this warning isn't triggered with lto active, it's likely just a false positive when there isn't lto
	)

	# strip in release, optimize for gdb usage in debug
	add_link_options($<$<CONFIG:RELEASE>:-s>)
	add_compile_options($<$<CONFIG:DEBUG>:-ggdb>)

	# use lld for clang (needed if doing lto)
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		add_link_options(-fuse-ld=lld)
	endif()

	# cmake will insert a lib prefix for libraries on non-MSVC targets 
	set(RC_TARGET rcheevos)
endif()

set(RC_RAPI_DIR ${CMAKE_SOURCE_DIR}/rcheevos/src/rapi)
set(RC_RCHEEVOS_DIR ${CMAKE_SOURCE_DIR}/rcheevos/src/rcheevos)
set(RC_RHASH_DIR ${CMAKE_SOURCE_DIR}/rcheevos/src/rhash)
set(RC_INCLUDE_DIR ${CMAKE_SOURCE_DIR}/rcheevos/include)

add_library(
	${RC_TARGET}
	SHARED
	${RC_RAPI_DIR}/rc_api_common.c
	${RC_RAPI_DIR}/rc_api_common.h
	${RC_RAPI_DIR}/rc_api_editor.c
	${RC_RAPI_DIR}/rc_api_info.c
	${RC_RAPI_DIR}/rc_api_runtime.c
	${RC_RAPI_DIR}/rc_api_user.c
	${RC_RCHEEVOS_DIR}/alloc.c
	${RC_RCHEEVOS_DIR}/compat.c
	${RC_RCHEEVOS_DIR}/condition.c
	${RC_RCHEEVOS_DIR}/condset.c
	${RC_RCHEEVOS_DIR}/consoleinfo.c
	${RC_RCHEEVOS_DIR}/format.c
	${RC_RCHEEVOS_DIR}/lboard.c
	${RC_RCHEEVOS_DIR}/memref.c
	${RC_RCHEEVOS_DIR}/operand.c
	${RC_RCHEEVOS_DIR}/rc_compat.h
	${RC_RCHEEVOS_DIR}/rc_internal.h
	${RC_RCHEEVOS_DIR}/rc_validate.c
	${RC_RCHEEVOS_DIR}/rc_validate.h
	${RC_RCHEEVOS_DIR}/richpresence.c
	${RC_RCHEEVOS_DIR}/runtime.c
	${RC_RCHEEVOS_DIR}/runtime_progress.c
	${RC_RCHEEVOS_DIR}/trigger.c
	${RC_RCHEEVOS_DIR}/value.c
	${RC_RHASH_DIR}/cdreader.c
	${RC_RHASH_DIR}/hash.c
	${RC_RHASH_DIR}/md5.c
	${RC_RHASH_DIR}/md5.h
	${RC_INCLUDE_DIR}/rc_api_editor.h
	${RC_INCLUDE_DIR}/rc_api_info.h
	${RC_INCLUDE_DIR}/rc_api_request.h
	${RC_INCLUDE_DIR}/rc_api_runtime.h
	${RC_INCLUDE_DIR}/rc_api_user.h
	${RC_INCLUDE_DIR}/rc_consoles.h
	${RC_INCLUDE_DIR}/rc_error.h
	${RC_INCLUDE_DIR}/rc_hash.h
	${RC_INCLUDE_DIR}/rc_runtime.h
	${RC_INCLUDE_DIR}/rc_runtime_types.h
	${RC_INCLUDE_DIR}/rc_url.h
	${RC_INCLUDE_DIR}/rcheevos.h
)

target_compile_definitions(${RC_TARGET} PRIVATE -DRC_DISABLE_LUA)
target_include_directories(${RC_TARGET} PRIVATE ${RC_INCLUDE_DIR})

add_custom_command(
	TARGET ${RC_TARGET}
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:${RC_TARGET}> ${CMAKE_SOURCE_DIR}/../../Assets/dll
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:${RC_TARGET}> ${CMAKE_SOURCE_DIR}/../../output/dll
)

if(MSVC)
	set(PDB_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/../../output/dll)
endif()
