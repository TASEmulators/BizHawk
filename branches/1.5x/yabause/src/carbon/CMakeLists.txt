project(yabause-carbon)

find_library(CARBON_LIBRARY carbon)

if (NOT CARBON_LIBRARY)
	return()
endif()

set(yabause_carbon_SOURCES main.c settings.c settings.h cpustatus.c cpustatus.h)

add_executable(yabause-carbon ${yabause_carbon_SOURCES})
target_link_libraries(yabause-carbon yabause ${YABAUSE_LIBRARIES} ${CARBON_LIBRARY})

if (NOT CMAKE_CURRENT_SOURCE_DIR STREQUAL CMAKE_CURRENT_BINARY_DIR)
	add_custom_command(
		OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app"
		COMMAND ${CMAKE_COMMAND} -E copy_directory "${CMAKE_CURRENT_SOURCE_DIR}/Yabause.app" "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app"
		DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/Yabause.app"
	)
endif()

add_custom_command(
	OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app/Contents/MacOS"
	COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app/Contents/MacOS"
	DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app"
)

add_custom_command(
	OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app/Contents/MacOS/yabause"
	COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:yabause-carbon> "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app/Contents/MacOS/yabause"
	DEPENDS yabause-carbon
	DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app/Contents/MacOS"
)

add_custom_target(yabause-carbon-app ALL
	DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/Yabause.app/Contents/MacOS/yabause")
