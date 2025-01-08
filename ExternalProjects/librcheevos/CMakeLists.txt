cmake_minimum_required(VERSION 3.12 FATAL_ERROR)

if(POLICY CMP0092)
	cmake_policy(SET CMP0092 NEW)
endif()

project(rcheevos C)

set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS ON)

# ensure lib shared library prefix is used
set(CMAKE_SHARED_LIBRARY_PREFIX "lib")

if(MSVC)
	# max warnings, treat as errors
	add_compile_options(/W4)
	add_compile_options(/WX)

	# ignore some warnings on cl
	if(CMAKE_C_COMPILER_ID MATCHES "MSVC")
		add_compile_options(
			/wd4244 # "conversion from 'type1' to 'type2', possible loss of data"
			/wd4245 # "conversion from 'type1' to 'type2', signed/unsigned mismatch"
		)
	endif()

	# all files are utf8
	add_compile_options(/utf-8)

	# max conformance mode
	add_compile_options(/permissive-)
	add_compile_options(/volatile:iso)
	add_compile_options(/fp:precise)
else()
	# max warnings, treat as errors
	add_compile_options(-Wall -Wextra)
	add_compile_options(-Werror)

	# strip in release, optimize for gdb usage in debug
	add_link_options($<$<CONFIG:RELEASE>:-s>)
	add_compile_options($<$<CONFIG:DEBUG>:-ggdb>)

	# use lld for clang (needed if doing lto)
	if(CMAKE_C_COMPILER_ID MATCHES "Clang")
		add_link_options(-fuse-ld=lld)
	endif()
endif()

set(RC_SRC_DIR ${CMAKE_SOURCE_DIR}/rcheevos/src)
set(RC_INCLUDE_DIR ${CMAKE_SOURCE_DIR}/rcheevos/include)

add_library(
	rcheevos
	SHARED
	${RC_SRC_DIR}/rc_compat.c
	${RC_SRC_DIR}/rc_compat.h
	${RC_SRC_DIR}/rc_util.c
	${RC_SRC_DIR}/rc_version.c
	${RC_SRC_DIR}/rc_version.h
	${RC_SRC_DIR}/rapi/rc_api_common.c
	${RC_SRC_DIR}/rapi/rc_api_common.h
	${RC_SRC_DIR}/rapi/rc_api_editor.c
	${RC_SRC_DIR}/rapi/rc_api_info.c
	${RC_SRC_DIR}/rapi/rc_api_runtime.c
	${RC_SRC_DIR}/rapi/rc_api_user.c
	${RC_SRC_DIR}/rcheevos/alloc.c
	${RC_SRC_DIR}/rcheevos/condition.c
	${RC_SRC_DIR}/rcheevos/condset.c
	${RC_SRC_DIR}/rcheevos/consoleinfo.c
	${RC_SRC_DIR}/rcheevos/format.c
	${RC_SRC_DIR}/rcheevos/lboard.c
	${RC_SRC_DIR}/rcheevos/memref.c
	${RC_SRC_DIR}/rcheevos/operand.c
	${RC_SRC_DIR}/rcheevos/rc_internal.h
	${RC_SRC_DIR}/rcheevos/rc_validate.c
	${RC_SRC_DIR}/rcheevos/rc_validate.h
	${RC_SRC_DIR}/rcheevos/richpresence.c
	${RC_SRC_DIR}/rcheevos/runtime.c
	${RC_SRC_DIR}/rcheevos/runtime_progress.c
	${RC_SRC_DIR}/rcheevos/trigger.c
	${RC_SRC_DIR}/rcheevos/value.c
	${RC_SRC_DIR}/rhash/aes.c
	${RC_SRC_DIR}/rhash/aes.h
	${RC_SRC_DIR}/rhash/cdreader.c
	${RC_SRC_DIR}/rhash/hash.c
	${RC_SRC_DIR}/rhash/md5.c
	${RC_SRC_DIR}/rhash/md5.h
	${RC_INCLUDE_DIR}/rc_api_editor.h
	${RC_INCLUDE_DIR}/rc_api_info.h
	${RC_INCLUDE_DIR}/rc_api_request.h
	${RC_INCLUDE_DIR}/rc_api_runtime.h
	${RC_INCLUDE_DIR}/rc_api_user.h
	${RC_INCLUDE_DIR}/rc_consoles.h
	${RC_INCLUDE_DIR}/rc_error.h
	${RC_INCLUDE_DIR}/rc_export.h
	${RC_INCLUDE_DIR}/rc_hash.h
	${RC_INCLUDE_DIR}/rc_runtime.h
	${RC_INCLUDE_DIR}/rc_runtime_types.h
	${RC_INCLUDE_DIR}/rc_util.h
	${RC_INCLUDE_DIR}/rcheevos.h
)

target_compile_definitions(rcheevos PRIVATE RC_DISABLE_LUA RC_NO_THREADS RC_SHARED)
target_include_directories(rcheevos PRIVATE ${RC_INCLUDE_DIR})

add_custom_command(
	TARGET rcheevos
	POST_BUILD
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:rcheevos> ${CMAKE_SOURCE_DIR}/../../Assets/dll
	COMMAND ${CMAKE_COMMAND}
	ARGS -E copy $<TARGET_FILE:rcheevos> ${CMAKE_SOURCE_DIR}/../../output/dll
)

if(MSVC)
	set(PDB_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/../../output/dll)
endif()
