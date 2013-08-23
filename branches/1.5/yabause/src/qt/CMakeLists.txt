project( yabause-qt )

yab_port_start()

find_package(Qt4)

if (NOT QT4_FOUND)
	message(STATUS "NO QT4_FOUND")
	return()
endif (NOT QT4_FOUND)

set( QT_USE_QTCORE TRUE )
set( QT_USE_QTGUI TRUE )
if( YAB_WANT_OPENGL )
	set( QT_USE_QTOPENGL TRUE )
endif()
if (WIN32)
	set( QT_USE_QTMAIN TRUE )
endif()

# dunno what it does exactly ... but seem required
include( ${QT_USE_FILE} )

# qt resources file
set( yabause_qt_RESOURCES resources/resources.qrc )

if ( WIN32 )
	# Windows port libraries
	set( yabause_qt_windows_LIBRARIES
		vfw32 )

	# Windows port sources
	set( yabause_qt_windows_SOURCES
		../windows/aviout.c )
	
	# resources
	set ( yabause_qt_windows_RESOURCES
		resources/icons/yabause.rc )
endif ( WIN32 )

# qt forms
set( yabause_qt_FORMS
	ui/UIYabause.ui
	ui/UISettings.ui
	ui/UIAbout.ui
	ui/UICheats.ui
	ui/UICheatAR.ui
	ui/UICheatRaw.ui
	ui/UICheatSearch.ui
	ui/UIWaitInput.ui
	ui/UIBackupRam.ui
	ui/UIPortManager.ui
	ui/UIPadSetting.ui 
    ui/UIDebugCPU.ui
    ui/UIDebugSCSP.ui
    ui/UIDebugVDP1.ui
    ui/UIDebugVDP2.ui
    ui/UIDebugVDP2Viewer.ui
    ui/UIHexInput.ui
    ui/UIMemoryTransfer.ui 
    ui/UIMemoryEditor.ui )

# pure C headers
set( yabause_qt_HEADERS
	QtYabause.h
	CommonDialogs.h
	PerQt.h
	Arguments.h )

# C headers needing moc
set( yabause_qt_MOC_HEADERS
	ui/UIYabause.h
	ui/UISettings.h
	ui/UIAbout.h
	ui/UICheats.h
	ui/UICheatAR.h
	ui/UICheatRaw.h
	ui/UICheatSearch.h
	ui/UIWaitInput.h
	ui/UIBackupRam.h
	ui/UIPortManager.h
	ui/UIPadSetting.h
    ui/UIDebugCPU.h
    ui/UIDebugM68K.h
    ui/UIDebugSCSP.h
    ui/UIDebugSCUDSP.h
    ui/UIDebugSH2.h
    ui/UIDebugVDP1.h
    ui/UIDebugVDP2.h
    ui/UIDebugVDP2Viewer.h
    ui/UIDisasm.h
    ui/UIHexInput.h
    ui/UIMemoryTransfer.h
    ui/UIHexEditor.h
    ui/UIMemoryEditor.h
	YabauseGL.h
	VolatileSettings.h
	Settings.h
	YabauseThread.h )

# C sources
set( yabause_qt_SOURCES
	main.cpp
	QtYabause.cpp
	CommonDialogs.cpp
	PerQt.c
	ui/UIYabause.cpp
	ui/UISettings.cpp
	ui/UIAbout.cpp
	ui/UICheats.cpp
	ui/UICheatAR.cpp
	ui/UICheatRaw.cpp
	ui/UICheatSearch.cpp
	ui/UIWaitInput.cpp
	ui/UIBackupRam.cpp
	ui/UIPortManager.cpp
	ui/UIPadSetting.cpp
    ui/UIDebugCPU.cpp
    ui/UIDebugM68K.cpp
    ui/UIDebugSCSP.cpp
    ui/UIDebugSCUDSP.cpp
    ui/UIDebugSH2.cpp
    ui/UIDebugVDP1.cpp
    ui/UIDebugVDP2.cpp
    ui/UIDebugVDP2Viewer.cpp
    ui/UIDisasm.cpp
    ui/UIHexInput.cpp
    ui/UIMemoryTransfer.cpp
    ui/UIHexEditor.cpp
    ui/UIMemoryEditor.cpp
	Settings.cpp
	VolatileSettings.cpp
	YabauseThread.cpp
	Arguments.cpp )

if( OPENGL_FOUND )
	set( yabause_qt_SOURCES ${yabause_qt_SOURCES} YabauseGL.cpp )
else()
	set( yabause_qt_SOURCES ${yabause_qt_SOURCES} YabauseSoftGL.cpp )
endif()

QT4_ADD_RESOURCES( yabause_qt_RCC_RESOURCES ${yabause_qt_RESOURCES} )
QT4_WRAP_UI( yabause_qt_UI_FORMS ${yabause_qt_FORMS} )
QT4_WRAP_CPP( yabause_qt_MOC_SOURCES ${yabause_qt_MOC_HEADERS} )

set( yabause_qt_SOURCES ${yabause_qt_SOURCES} resources/icons/yabause.icns )
set_source_files_properties( resources/icons/yabause.icns PROPERTIES MACOSX_PACKAGE_LOCATION Resources )

add_executable( yabause-qt WIN32
	${yabause_qt_windows_SOURCES}
	${yabause_qt_windows_RESOURCES}
	${yabause_qt_RCC_RESOURCES}
	${yabause_qt_HEADERS}
	${yabause_qt_MOC_HEADERS}
	${yabause_qt_SOURCES}
	${yabause_qt_UI_FORMS}
	${yabause_qt_MOC_SOURCES} )

include_directories(
	.
	${CMAKE_CURRENT_BINARY_DIR}
	${CMAKE_CURRENT_SOURCE_DIR}/ui
	${QT_INCLUDE_DIR}
	${QT_QTCORE_INCLUDE_DIR}
	${QT_QTGUI_INCLUDE_DIR}
	${QT_QTOPENGL_INCLUDE_DIR} )

# APPLE // not necessary mac os x, but i don't care ;)
if (APPLE)
	set_target_properties( yabause-qt PROPERTIES MACOSX_BUNDLE true )
	set_target_properties( yabause-qt PROPERTIES MACOSX_BUNDLE_ICON_FILE yabause.icns )
	set_target_properties( yabause-qt PROPERTIES MACOSX_BUNDLE_LONG_VERSION_STRING "${YAB_VERSION}" )
	set_target_properties( yabause-qt PROPERTIES MACOSX_BUNDLE_BUNDLE_NAME Yabause )
	set_target_properties( yabause-qt PROPERTIES MACOSX_BUNDLE_SHORT_VERSION_STRING "${YAB_VERSION}" )
	set_target_properties( yabause-qt PROPERTIES MACOSX_BUNDLE_COPYRIGHT "(c) Yabause Team" )
endif (APPLE)

target_link_libraries( yabause-qt yabause )
target_link_libraries( yabause-qt ${YABAUSE_LIBRARIES} )
target_link_libraries( yabause-qt ${PORT_LIBRARIES} )
target_link_libraries( yabause-qt ${yabause_qt_windows_LIBRARIES} )
target_link_libraries( yabause-qt ${QT_LIBRARIES} )

yab_port_success(yabause-qt)
configure_file(yabause.desktop.in ${YAB_PORT_NAME}.desktop)

if (WIN32)
	install(TARGETS yabause-qt DESTINATION ".")
	if (GLUT_FOUND)
		install(FILES ${GLUT_INCLUDE_DIR}/../freeglut.dll DESTINATION ".")
	endif ()
	if (SDL_FOUND)
		install(FILES ${SDL_INCLUDE_DIR}/../lib/SDL.dll DESTINATION ".")
	endif ()
	install(FILES ${QT_BINARY_DIR}/QtCore4.dll DESTINATION ".")
	install(FILES ${QT_BINARY_DIR}/QtGui4.dll DESTINATION ".")
	install(FILES ${QT_BINARY_DIR}/QtOpenGL4.dll DESTINATION ".")

	install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/../../AUTHORS    DESTINATION "." RENAME AUTHORS.txt)
	install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/../../ChangeLog  DESTINATION "." RENAME ChangeLog.txt)
	install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/../../COPYING    DESTINATION "." RENAME COPYING.txt)
	install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/../../README     DESTINATION "." RENAME README.txt)
	install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/../../README.WIN DESTINATION "." RENAME README.WIN.txt)
	
	if (MINGW)
		get_filename_component( Mingw_Path ${CMAKE_CXX_COMPILER} PATH )
		install(FILES ${Mingw_Path}/libgcc_s_dw2-1.dll DESTINATION ".")
		install(FILES ${Mingw_Path}/mingwm10.dll DESTINATION ".")
	endif ()
else ()
	install(TARGETS yabause-qt DESTINATION "bin")
	install(FILES ${CMAKE_CURRENT_BINARY_DIR}/${YAB_PORT_NAME}.desktop DESTINATION "share/applications")
	install(FILES "doc/yabause.1" DESTINATION "${YAB_MAN_DIR}/man1" RENAME "${YAB_PORT_NAME}.1")
	install(FILES "resources/icons/yabause.png" DESTINATION "share/pixmaps")
endif ()