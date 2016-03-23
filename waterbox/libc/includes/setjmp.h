#ifndef _SETJMP_H
#define _SETJMP_H

#ifdef __cplusplus
extern "C" {
#endif

#define _JBTYPE long long
#define _JBLEN  8
typedef	_JBTYPE jmp_buf[_JBLEN];

int setjmp (jmp_buf env);
void longjmp (jmp_buf env, int val);

#ifdef __cplusplus
}
#endif

#endif
