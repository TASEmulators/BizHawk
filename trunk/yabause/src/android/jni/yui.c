/*  Copyright 2011 Guillaume Duhamel

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include <jni.h>
#include <android/bitmap.h>
#include <android/log.h>

#include "../../config.h"
#include "yabause.h"
#include "scsp.h"
#include "vidsoft.h"
#include "peripheral.h"
#include "m68kcore.h"
#include "sh2core.h"
#include "sh2int.h"
#include "cdbase.h"
#include "cs2.h"
#include "debug.h"

#include <stdio.h>
#include <dlfcn.h>

#define _ANDROID_2_2_
#ifdef _ANDROID_2_2_
#include "miniegl.h"
#else
#include <EGL/egl.h>
#endif

#include <GLES/gl.h>
#include <GLES/glext.h>
#include <pthread.h>

#include "sndaudiotrack.h"

JavaVM * yvm;
static jobject yabause;
static jobject ybitmap;

static char biospath[256] = "/mnt/sdcard/jap.rom";
static char cdpath[256] = "\0";
static char buppath[256] = "\0";
static char mpegpath[256] = "\0";
static char cartpath[256] = "\0";

EGLDisplay g_Display = EGL_NO_DISPLAY;
EGLSurface g_Surface = EGL_NO_SURFACE; 
EGLContext g_Context = EGL_NO_CONTEXT; 
GLuint g_FrameBuffer = 0;
GLuint g_VertexBuffer = 0;
int g_buf_width = -1;
int g_buf_height = -1;
pthread_mutex_t g_mtxGlLock = PTHREAD_MUTEX_INITIALIZER;
float vertices [] = { 
   0, 0, 0, 0,
   320, 0, 0, 0, 
   320, 224, 0, 0, 
   0, 224, 0, 0
};


M68K_struct * M68KCoreList[] = {
&M68KDummy,
#ifdef HAVE_C68K
&M68KC68K,
#endif
#ifdef HAVE_Q68
&M68KQ68,
#endif
NULL
};

SH2Interface_struct *SH2CoreList[] = {
&SH2Interpreter,
&SH2DebugInterpreter,
#ifdef SH2_DYNAREC
&SH2Dynarec,
#endif
NULL
};

PerInterface_struct *PERCoreList[] = {
&PERDummy,
NULL
};

CDInterface *CDCoreList[] = {
&DummyCD,
&ISOCD,
NULL
};

SoundInterface_struct *SNDCoreList[] = {
&SNDDummy,
&SNDAudioTrack,
NULL
};

VideoInterface_struct *VIDCoreList[] = {
&VIDDummy,
&VIDSoft,
NULL
};


#define  LOG_TAG    "yabause"

/* Override printf for debug*/
int printf( const char * fmt, ... )
{
   va_list ap;
   va_start(ap, fmt);
   int result = __android_log_vprint(ANDROID_LOG_INFO, LOG_TAG, fmt, ap);
   va_end(ap);
   return result;   
}

void YuiErrorMsg(const char *string)
{
    jclass yclass;
    jmethodID errorMsg;
    jstring message;
    JNIEnv * env;
    if ((*yvm)->GetEnv(yvm, (void**) &env, JNI_VERSION_1_6) != JNI_OK)
        return;

    yclass = (*env)->GetObjectClass(env, yabause);
    errorMsg = (*env)->GetMethodID(env, yclass, "errorMsg", "(Ljava/lang/String;)V");
    message = (*env)->NewStringUTF(env, string);
    (*env)->CallVoidMethod(env, yabause, errorMsg, message);
}

void YuiSwapBuffers(void)
{
   int buf_width, buf_height;
   int error;
   
   
   pthread_mutex_lock(&g_mtxGlLock);
   if( g_Display == EGL_NO_DISPLAY ) 
   {
      pthread_mutex_unlock(&g_mtxGlLock);
      return;
   }

   if( eglMakeCurrent(g_Display,g_Surface,g_Surface,g_Context) == EGL_FALSE )
   {
         printf( "eglMakeCurrent fail %04x",eglGetError());
         pthread_mutex_unlock(&g_mtxGlLock);
         return;
   }   
      
   glClearColor( 0.0f,0.0f,0.0f,1.0f);
   glClear(GL_COLOR_BUFFER_BIT);

   
   if( g_FrameBuffer == 0 )
   {
      glEnable(GL_TEXTURE_2D);
      glGenTextures(1,&g_FrameBuffer);
      glBindTexture(GL_TEXTURE_2D, g_FrameBuffer);
      glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, 1024, 1024, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL);
      glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
      glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);   
      glTexParameteri( GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST );
      glTexParameteri( GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST );
      glTexEnvi(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_REPLACE);
      error = glGetError();
      if( error != GL_NO_ERROR )
      {
         printf("gl error %d", error );
         return;
      }
   }else{
      glBindTexture(GL_TEXTURE_2D, g_FrameBuffer);
   }
   

   VIDCore->GetGlSize(&buf_width, &buf_height);
   glTexSubImage2D(GL_TEXTURE_2D, 0,0,0,buf_width,buf_height,GL_RGBA,GL_UNSIGNED_BYTE,dispbuffer);
   
   
   if( g_VertexBuffer == 0 )
   {
      glGenBuffers(1, &g_VertexBuffer);
      glBindBuffer(GL_ARRAY_BUFFER, g_VertexBuffer);
      glBufferData(GL_ARRAY_BUFFER, sizeof(vertices),vertices,GL_STATIC_DRAW);
      error = glGetError();
      if( error != GL_NO_ERROR )
      {
         printf("gl error %d", error );
         return;
      }      
   }else{
      glBindBuffer(GL_ARRAY_BUFFER, g_VertexBuffer);
   }
  
  if( buf_width != g_buf_width ||  buf_height != g_buf_height )
  {
     vertices[6]=vertices[10]=(float)buf_width/1024.f;
     vertices[11]=vertices[15]=(float)buf_height/1024.f;
     glBufferData(GL_ARRAY_BUFFER, sizeof(vertices),vertices,GL_STATIC_DRAW);
     glVertexPointer(2, GL_FLOAT, sizeof(float)*4, 0);
     glTexCoordPointer(2, GL_FLOAT, sizeof(float)*4, (void*)(sizeof(float)*2));   
     glEnableClientState(GL_VERTEX_ARRAY);
     glEnableClientState(GL_TEXTURE_COORD_ARRAY);
     g_buf_width  = buf_width;
     g_buf_height = buf_height;
  }
    
   glDrawArrays(GL_TRIANGLE_FAN, 0, 4);
   eglSwapBuffers(g_Display,g_Surface);
   
   pthread_mutex_unlock(&g_mtxGlLock);
}

int Java_org_yabause_android_YabauseRunnable_initViewport( int width, int height)
{
   int swidth;
   int sheight;
   int error;
   char * buf;

   g_Display = eglGetCurrentDisplay();
   g_Surface = eglGetCurrentSurface(EGL_READ);
   g_Context = eglGetCurrentContext();

   eglQuerySurface(g_Display,g_Surface,EGL_WIDTH,&swidth);
   eglQuerySurface(g_Display,g_Surface,EGL_HEIGHT,&sheight);
   
   glViewport(0,0,swidth,sheight);
   
   glMatrixMode(GL_PROJECTION);
   glLoadIdentity();
   glOrthof(0, 320, 224, 0, 1, 0);
   
   glMatrixMode(GL_MODELVIEW);
   glLoadIdentity();

   glMatrixMode(GL_TEXTURE);
   glLoadIdentity();
   
   glDisable(GL_BLEND);
   glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
   
   printf(glGetString(GL_VENDOR));
   printf(glGetString(GL_RENDERER));
   printf(glGetString(GL_VERSION));
   printf(glGetString(GL_EXTENSIONS));
   printf(eglQueryString(g_Display,EGL_EXTENSIONS));
   eglSwapInterval(g_Display,0);
   eglMakeCurrent(g_Display,EGL_NO_SURFACE,EGL_NO_SURFACE,EGL_NO_CONTEXT);
   return 0;
}

#ifdef _ANDROID_2_2_
int initEGLFunc()
{
   void * handle;
   char *error;

   handle = dlopen("libEGL.so",RTLD_LAZY);
   if( handle == NULL )
   {
      printf(dlerror());
      return -1;
   }
   
   eglGetCurrentDisplay = dlsym(handle, "eglGetCurrentDisplay");
   if( eglGetCurrentDisplay == NULL){ printf(dlerror()); return -1; }  
   
   eglGetCurrentSurface = dlsym(handle, "eglGetCurrentSurface");
   if( eglGetCurrentSurface == NULL){ printf(dlerror()); return -1; }  
   
   eglGetCurrentContext = dlsym(handle, "eglGetCurrentContext");
   if( eglGetCurrentContext == NULL){ printf(dlerror()); return -1; }  
   
   eglQuerySurface      = dlsym(handle, "eglQuerySurface");
   if( eglQuerySurface == NULL){ printf(dlerror()); return -1; }  
   
   eglSwapInterval      = dlsym(handle, "eglSwapInterval");
   if( eglSwapInterval == NULL){ printf(dlerror()); return -1; }  
   
   eglMakeCurrent       = dlsym(handle, "eglMakeCurrent");
   if( eglMakeCurrent == NULL){ printf(dlerror()); return -1; }  
   
   eglSwapBuffers       = dlsym(handle, "eglSwapBuffers");
   if( eglSwapBuffers == NULL){ printf(dlerror()); return -1; }  
   
   eglQueryString       = dlsym(handle, "eglQueryString");
   if( eglQueryString == NULL){ printf(dlerror()); return -1; }  
   
   eglGetError          = dlsym(handle, "eglGetError");
   if( eglGetError == NULL){ printf(dlerror()); return -1; }   
   
   return 0;
}
#else
int initEGLFunc()
{
   return 0;
}
#endif

int Java_org_yabause_android_YabauseRunnable_lockGL()
{
   pthread_mutex_lock(&g_mtxGlLock);  
}

int Java_org_yabause_android_YabauseRunnable_unlockGL()
{
   pthread_mutex_unlock(&g_mtxGlLock);  
}


jint
Java_org_yabause_android_YabauseRunnable_init( JNIEnv* env, jobject obj, jobject yab, jobject bitmap )
{
    yabauseinit_struct yinit;
    int res;
    void * padbits;
    
    if( initEGLFunc() == -1 ) return -1;

    yabause = (*env)->NewGlobalRef(env, yab);
    ybitmap = (*env)->NewGlobalRef(env, bitmap);

    yinit.m68kcoretype = M68KCORE_C68K;
    yinit.percoretype = PERCORE_DUMMY;
#ifdef SH2_DYNAREC
    yinit.sh2coretype = 2;
#else
    yinit.sh2coretype = SH2CORE_DEFAULT;
#endif
    yinit.vidcoretype = VIDCORE_SOFT;
    yinit.sndcoretype = SNDCORE_AUDIOTRACK;
    yinit.cdcoretype = CDCORE_DEFAULT;
    yinit.carttype = CART_NONE;
    yinit.regionid = 0;
    yinit.biospath = biospath;
    yinit.cdpath = cdpath;
    yinit.buppath = buppath;
    yinit.mpegpath = mpegpath;
    yinit.cartpath = cartpath;
    yinit.videoformattype = VIDEOFORMATTYPE_NTSC;

    res = YabauseInit(&yinit);

    PerPortReset();
    padbits = PerPadAdd(&PORTDATA1);
    PerSetKey(1, PERPAD_LEFT, padbits);
    PerSetKey(4, PERPAD_UP, padbits);
    PerSetKey(6, PERPAD_DOWN, padbits);
    PerSetKey(9, PERPAD_RIGHT, padbits);

    ScspSetFrameAccurate(1);

    return res;
}

void
Java_org_yabause_android_YabauseRunnable_deinit( JNIEnv* env )
{
    YabauseDeInit();
}

void
Java_org_yabause_android_YabauseRunnable_exec( JNIEnv* env )
{
    YabauseExec();
}

void
Java_org_yabause_android_YabauseRunnable_press( JNIEnv* env, jobject obj, jint key )
{
    PerKeyDown(key);
}

void
Java_org_yabause_android_YabauseRunnable_release( JNIEnv* env, jobject obj, jint key )
{
    PerKeyUp(key);
}

void log_callback(char * message)
{
    __android_log_print(ANDROID_LOG_INFO, "yabause", "%s", message);
}

jint JNI_OnLoad(JavaVM * vm, void * reserved)
{
    JNIEnv * env;
    if ((*vm)->GetEnv(vm, (void**) &env, JNI_VERSION_1_6) != JNI_OK)
        return -1;

    yvm = vm;

    LogStart();
    LogChangeOutput(DEBUG_CALLBACK, (char *) log_callback);

    return JNI_VERSION_1_6;
}

