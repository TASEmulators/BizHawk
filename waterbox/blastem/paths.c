#include <string.h>
#include <stdlib.h>
#include "blastem.h"
#include "util.h"

static char **current_path;

static void persist_path(void)
{
	char *pathfname = alloc_concat(get_userdata_dir(), PATH_SEP "blastem" PATH_SEP "sticky_path");
	FILE *f = fopen(pathfname, "wb");
	if (f) {
		if (fwrite(*current_path, 1, strlen(*current_path), f) != strlen(*current_path)) {
			warning("Failed to save menu path");
		}
		fclose(f);
	} else {
		warning("Failed to save menu path: Could not open %s for writing\n", pathfname);
		
	}
	free(pathfname);
}

#ifdef __ANDROID__
#include <SDL.h>
#include <jni.h>
static char *get_external_storage_path()
{
	static char *ret;
	if (ret) {
		return ret;
	}
	JNIEnv *env = SDL_AndroidGetJNIEnv();
	if ((*env)->PushLocalFrame(env, 8) < 0) {
		return NULL;
	}

	jclass Environment = (*env)->FindClass(env, "android/os/Environment");
	jmethodID getExternalStorageDirectory =
		(*env)->GetStaticMethodID(env, Environment, "getExternalStorageDirectory", "()Ljava/io/File;");
	jobject file = (*env)->CallStaticObjectMethod(env, Environment, getExternalStorageDirectory);
	if (!file) {
		goto cleanup;
	}

	jmethodID getAbsolutePath = (*env)->GetMethodID(env, (*env)->GetObjectClass(env, file),
		"getAbsolutePath", "()Ljava/lang/String;");
	jstring path = (*env)->CallObjectMethod(env, file, getAbsolutePath);

	char const *tmp = (*env)->GetStringUTFChars(env, path, NULL);
	ret = strdup(tmp);
	(*env)->ReleaseStringUTFChars(env, path, tmp);

cleanup:
	(*env)->PopLocalFrame(env, NULL);
	return ret;
}
#endif

void get_initial_browse_path(char **dst)
{
	char *base = NULL;
	char *remember_path = tern_find_path(config, "ui\0remember_path\0", TVAL_PTR).ptrval;
	if (!remember_path || !strcmp("on", remember_path)) {
		char *pathfname = alloc_concat(get_userdata_dir(), PATH_SEP "blastem" PATH_SEP "sticky_path");
		FILE *f = fopen(pathfname, "rb");
		if (f) {
			long pathsize = file_size(f);
			if (pathsize > 0) {
				base = malloc(pathsize + 1);
				if (fread(base, 1, pathsize, f) != pathsize) {
					warning("Error restoring saved file browser path");
					free(base);
					base = NULL;
				} else {
					base[pathsize] = 0;
				}
			}
			fclose(f);
		}
		free(pathfname);
		if (!current_path) {
			atexit(persist_path);
			current_path = dst;
		}
	}
	if (!base) {
		base = tern_find_path(config, "ui\0initial_path\0", TVAL_PTR).ptrval;
	}
	if (!base){
#ifdef __ANDROID__
		base = get_external_storage_path();
#else
		base = "$HOME";
#endif
	}
	tern_node *vars = tern_insert_ptr(NULL, "HOME", get_home_dir());
	vars = tern_insert_ptr(vars, "EXEDIR", get_exe_dir());
	*dst = replace_vars(base, vars, 1);
	free(base);
	tern_free(vars);
}

char *path_append(const char *base, const char *suffix)
{
	if (!strcmp(suffix, "..")) {
#ifdef _WIN32
		//handle transition from root of a drive to virtual root
		if (base[1] == ':' && !base[2]) {
			return strdup(PATH_SEP);
		}
#endif
		size_t len = strlen(base);
		while (len > 0) {
			--len;
			if (is_path_sep(base[len])) {
				if (!len) {
					//special handling for /
					len++;
				}
				char *ret = malloc(len+1);
				memcpy(ret, base, len);
				ret[len] = 0;
				return ret;
			}
		}
		return strdup(PATH_SEP);
	} else {
#ifdef _WIN32
		if (base[0] == PATH_SEP[0] && !base[1]) {
			//handle transition from virtual root to root of a drive
			return strdup(suffix);
		}
#endif
		if (is_path_sep(base[strlen(base) - 1])) {
			return alloc_concat(base, suffix);
		} else {
			char const *pieces[] = {base, PATH_SEP, suffix};
			return alloc_concat_m(3, pieces);
		}
	}
}
