AC_DEFUN([YAB_CHECK_HOST_TOOLS],
	[
		AC_CHECK_TOOLS([$1], [$2])
		if test `expr x$[$1] : x$host_alias` -eq 0 ; then
			[$1]=""
		fi
	])

AC_DEFUN([YAB_DEP_DISABLED],
	[
		depdisabled=no
		for i in $ac_configure_args ; do
			if test $i = "'--disable-dependency-tracking'" ; then
				depdisabled=yes
			fi
		done
		if test "$depdisabled" = "no" ; then
			AC_MSG_ERROR([You must disable dependency tracking
run the configure script again with --disable-dependency-tracking])
		fi
	])

AC_DEFUN([YAB_LINK_MINI18N],
	[
		AC_ARG_ENABLE(static-mini18n,
			AC_HELP_STRING(--enable-static-mini18n, Use a static dependency on mini18n),
			[use_static_mini18n=$enableval])
		if test "x$use_static_mini18n" = "xyes" ; then
			LIBS="-Wl,-Bstatic -lmini18n -Wl,-Bdynamic $LIBS"
		else
			LIBS="-lmini18n $LIBS"
		fi
		AC_DEFINE(HAVE_LIBMINI18N)
	])
