// These are mostly unrelated to Waterbox, and are only here because I was too lazy to put them elsewhere.

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 0 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart0() -> usize {
	let mut fp: extern "sysv64" fn() -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp()
}

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 1 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart1(a: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a)
}

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 2 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart2(a: usize, b: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b)
}

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 3 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart3(a: usize, b: usize, c: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c)
}

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 4 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart4(a: usize, b: usize, c: usize, d: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize, d: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c, d)
}

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 5 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart5(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c, d, e)
}

/// win64 function that calls a sysv64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 6 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "win64" fn depart6(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c, d, e, f)
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 0 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive0() -> usize {
	let mut fp: extern "win64" fn() -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp()
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 1 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive1(a: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a)
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 2 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive2(a: usize, b: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b)
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 3 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive3(a: usize, b: usize, c: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c)
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 4 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive4(a: usize, b: usize, c: usize, d: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize, d: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c, d)
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 5 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive5(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c, d, e)
}

/// sysv64 function that calls a win64 function and returns its result.
/// The function is passed as a hidden parameter in rax, and should take 6 arguments, all of which are pointer or integer type.
#[no_mangle]
pub unsafe extern "sysv64" fn arrive6(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize;
	asm!("", out("rax") fp); // Technically, this is wrong as the function could have mangled rax already.  In practice, that doesn't happen.
	fp(a, b, c, d, e, f)
}
