/*
 * z64
 *
 * Copyright (C) 2007  ziggy
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *
**/

#include <string.h>

#include <SDL.h>

#include "rgl_glut.h"

#ifdef RGL_USE_GLUT
#include <GL/glui.h>

extern int screen_width, screen_height;

/** These are the live variables passed into GLUI ***/
int   wireframe = 0;
int   segments = 8;
int   main_window;

static GLUI *glui;

static SDL_sem * commandSem;
static SDL_cond * commandCond;
static SDL_mutex * commandMutex;
static SDL_sem * commandFinishedSem;
static rglGlutCommand_f command, nextCommand;

/***************************************** myGlutIdle() ***********/

void myGlutIdle( )
{
//   SDL_LockMutex(commandMutex);
//   if (!SDL_CondWaitTimeout(commandCond, commandMutex, 1)) {
  if (!SDL_SemWaitTimeout(commandSem, 1)) {
  //if (!SDL_SemWait(commandSem)) {
    //printf("receive a command\n");
    if ( glutGetWindow() && glutGetWindow() != main_window ) 
      glutSetWindow(main_window);
    nextCommand = command;
    command = 0;
    glutPostRedisplay();
  }
//   SDL_UnlockMutex(commandMutex);
}

void myGlutTimer( int dummy )
{

  /* According to the GLUT specification, the current window is 
     undefined during an idle callback.  So we need to explicitly change
     it if necessary */
  if ( glutGetWindow() != main_window ) 
    glutSetWindow(main_window);  

  glutPostRedisplay();
}


/**************************************** myGlutReshape() *************/

void myGlutReshape( int x, int y )
{
  float xy_aspect;

  xy_aspect = (float)x / (float)y;
  glViewport( 0, 0, x, y );

//   glMatrixMode( GL_PROJECTION );
//   glLoadIdentity();
//   glFrustum( -xy_aspect*.08, xy_aspect*.08, -.08, .08, .1, 15.0 );

  glutPostRedisplay();
}

/***************************************** myGlutDisplay() *****************/

void myGlutDisplay( void )
{
  if (nextCommand) {
    nextCommand();
    nextCommand = 0;
    SDL_SemPost(commandFinishedSem);
  }
  
  //glutSwapBuffers(); 

  //glutTimerFunc(1, myGlutTimer, 0);
}


/**************************************** main() ********************/

static int glutmain(int argc, char* argv[])
{
  /****************************************/
  /*   Initialize GLUT and create window  */
  /****************************************/

  glutInit(&argc, argv);
  glutInitDisplayMode( GLUT_RGB | GLUT_DOUBLE | GLUT_DEPTH );
  //glutInitWindowPosition( 50, 50 );
  glutInitWindowSize( screen_width, screen_height );
 
  main_window = glutCreateWindow( "z64gl" );
  glutDisplayFunc( myGlutDisplay );
  glutReshapeFunc( myGlutReshape );  

  /****************************************/
  /*         Here's the GLUI code         */
  /****************************************/
  
  glui = GLUI_Master.create_glui( "GLUI" );
  new GLUI_Checkbox( glui, "Wireframe", &wireframe );
  (new GLUI_Spinner( glui, "Segments:", &segments ))
    ->set_int_limits( 3, 60 ); 
   
  glui->set_main_gfx_window( main_window );

  /* We register the idle callback with GLUI, *not* with GLUT */
  GLUI_Master.set_glutIdleFunc( myGlutIdle );
  //glutTimerFunc(1, myGlutTimer, 0);

  glutMainLoop();

  return EXIT_SUCCESS;
}





static SDL_Thread * thread;

int rglGlutThread(void * dummy)
{
  int argc = 1;
  char * argv[2] = { "z64gl", 0 };

  glutmain(argc, argv);

  thread = 0;

  // in case of, but glutMainLoop never exits anyway
  exit(0);
}

void rglGlutMinimizeWindow()
{
  //glutDestroyWindow(main_window);
  myGlutReshape(64, 64);
}

void rglGlutRecreateWindow()
{
  int oldmain = main_window;
  
  glutInitWindowSize( screen_width, screen_height );
  main_window = glutCreateWindow( "z64gl" );
  glutDisplayFunc( myGlutDisplay );
  glutReshapeFunc( myGlutReshape );  

  glui->set_main_gfx_window( main_window );
  /* We register the idle callback with GLUI, *not* with GLUT */
  GLUI_Master.set_glutIdleFunc( myGlutIdle );
  //glutTimerFunc(1, myGlutTimer, 0);

  glutSetWindow(main_window);  
  
  glutDestroyWindow(oldmain);
}

void rglGlutCreateThread(int recreate)
{
  if (!thread) {
    commandSem = SDL_CreateSemaphore(0);
    commandCond = SDL_CreateCond();
    commandMutex = SDL_CreateMutex();
    commandFinishedSem = SDL_CreateSemaphore(0);
    
    thread = SDL_CreateThread(rglGlutThread, 0);
  } else if (recreate)
    rglGlutPostCommand(rglGlutRecreateWindow);
}

void rglGlutPostCommand(rglGlutCommand_f c)
{
  command = c;
  SDL_SemPost(commandSem);
//   SDL_LockMutex(commandMutex);
//   SDL_CondSignal(commandCond);
//   SDL_UnlockMutex(commandMutex);
  SDL_SemWait(commandFinishedSem);
}

void rglSwapBuffers()
{
  glutSwapBuffers(); 
}

#endif
