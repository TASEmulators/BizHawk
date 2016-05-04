/* Errors <errno.h>

   This file is part of the Public Domain C Library (PDCLib).
   Permission is granted to use, modify, and / or redistribute at will.
*/

#ifndef _PDCLIB_ERRNO_H
#define _PDCLIB_ERRNO_H _PDCLIB_ERRNO_H

#include "_PDCLIB_int.h"

#ifdef __cplusplus
extern "C" {
#endif

extern int * _PDCLIB_errno_func( void );
#define errno (*_PDCLIB_errno_func())

/* C only requires the following three */
#define ERANGE          _PDCLIB_ERANGE
#define EDOM            _PDCLIB_EDOM
#define EILSEQ          _PDCLIB_EILSEQ

/* C++11 additionally requires the following (taken from POSIX) */
#define E2BIG           _PDCLIB_E2BIG
#define EACCES          _PDCLIB_EACCES
#define EADDRINUSE      _PDCLIB_EADDRINUSE
#define EADDRNOTAVAIL   _PDCLIB_EADDRNOTAVAIL
#define EAFNOSUPPORT    _PDCLIB_EAFNOSUPPORT
#define EAGAIN          _PDCLIB_EAGAIN
#define EALREADY        _PDCLIB_EALREADY
#define EBADF           _PDCLIB_EBADF
#define EBADMSG         _PDCLIB_EBADMSG
#define EBUSY           _PDCLIB_EBUSY
#define ECANCELED       _PDCLIB_ECANCELED
#define ECHILD          _PDCLIB_ECHILD
#define ECONNABORTED    _PDCLIB_ECONNABORTED
#define ECONNREFUSED    _PDCLIB_ECONNREFUSED
#define ECONNRESET      _PDCLIB_ECONNRESET
#define EDEADLK         _PDCLIB_EDEADLK
#define EDESTADDRREQ    _PDCLIB_EDESTADDRREQ
#define EEXIST          _PDCLIB_EEXIST
#define EFAULT          _PDCLIB_EFAULT
#define EFBIG           _PDCLIB_EFBIG
#define EHOSTUNREACH    _PDCLIB_EHOSTUNREACH
#define EIDRM           _PDCLIB_EIDRM
#define EINPROGRESS     _PDCLIB_EINPROGRESS
#define EINTR           _PDCLIB_EINTR
#define EINVAL          _PDCLIB_EINVAL
#define EIO             _PDCLIB_EIO
#define EISCONN         _PDCLIB_EISCONN
#define EISDIR          _PDCLIB_EISDIR
#define ELOOP           _PDCLIB_ELOOP
#define EMFILE          _PDCLIB_EMFILE
#define EMLINK          _PDCLIB_EMLINK
#define EMSGSIZE        _PDCLIB_EMSGSIZE
#define ENAMETOOLONG    _PDCLIB_ENAMETOOLONG
#define ENETDOWN        _PDCLIB_ENETDOWN
#define ENETRESET       _PDCLIB_ENETRESET
#define ENETUNREACH     _PDCLIB_ENETUNREACH
#define ENFILE          _PDCLIB_ENFILE
#define ENOBUFS         _PDCLIB_ENOBUFS
#define ENODATA         _PDCLIB_ENODATA
#define ENODEV          _PDCLIB_ENODEV
#define ENOENT          _PDCLIB_ENOENT
#define ENOEXEC         _PDCLIB_ENOEXEC
#define ENOLCK          _PDCLIB_ENOLCK
#define ENOLINK         _PDCLIB_ENOLINK
#define ENOMEM          _PDCLIB_ENOMEM
#define ENOMSG          _PDCLIB_ENOMSG
#define ENOPROTOOPT     _PDCLIB_ENOPROTOOPT
#define ENOSPC          _PDCLIB_ENOSPC
#define ENOSR           _PDCLIB_ENOSR
#define ENOSTR          _PDCLIB_ENOSTR
#define ENOSYS          _PDCLIB_ENOSYS
#define ENOTCONN        _PDCLIB_ENOTCONN
#define ENOTDIR         _PDCLIB_ENOTDIR
#define ENOTEMPTY       _PDCLIB_ENOTEMPTY
#define ENOTRECOVERABLE _PDCLIB_ENOTRECOVERABLE
#define ENOTSOCK        _PDCLIB_ENOTSOCK
#define ENOTSUP         _PDCLIB_ENOTSUP
#define ENOTTY          _PDCLIB_ENOTTY
#define ENXIO           _PDCLIB_ENXIO
#define EOPNOTSUPP      _PDCLIB_EOPNOTSUPP
#define EOVERFLOW       _PDCLIB_EOVERFLOW
#define EOWNERDEAD      _PDCLIB_EOWNERDEAD
#define EPERM           _PDCLIB_EPERM
#define EPIPE           _PDCLIB_EPIPE
#define EPROTO          _PDCLIB_EPROTO
#define EPROTONOSUPPORT _PDCLIB_EPROTONOSUPPORT
#define EPROTOTYPE      _PDCLIB_EPROTOTYPE
#define EROFS           _PDCLIB_EROFS
#define ESPIPE          _PDCLIB_ESPIPE
#define ESRCH           _PDCLIB_ESRCH
#define ETIME           _PDCLIB_ETIME
#define ETIMEDOUT       _PDCLIB_ETIMEDOUT
#define ETXTBSY         _PDCLIB_ETXTBSY
#define EWOULDBLOCK     _PDCLIB_EWOULDBLOCK
#define EXDEV           _PDCLIB_EXDEV

#ifdef __cplusplus
}
#endif

#endif
