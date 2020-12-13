// These are mostly unrelated to Waterbox, and are only here because I was too lazy to put them elsewhere.

#[no_mangle]
pub unsafe extern "win64" fn depart0() -> usize {
	let mut fp: extern "sysv64" fn() -> usize;
	asm!("", out("rax") fp);
	fp()
}

#[no_mangle]
pub unsafe extern "win64" fn depart1(a: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a)
}

#[no_mangle]
pub unsafe extern "win64" fn depart2(a: usize, b: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b)
}

#[no_mangle]
pub unsafe extern "win64" fn depart3(a: usize, b: usize, c: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c)
}

#[no_mangle]
pub unsafe extern "win64" fn depart4(a: usize, b: usize, c: usize, d: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize, d: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c, d)
}

#[no_mangle]
pub unsafe extern "win64" fn depart5(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c, d, e)
}

#[no_mangle]
pub unsafe extern "win64" fn depart6(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize {
	let mut fp: extern "sysv64" fn(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c, d, e, f)
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive0() -> usize {
	let mut fp: extern "win64" fn() -> usize;
	asm!("", out("rax") fp);
	fp()
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive1(a: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a)
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive2(a: usize, b: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b)
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive3(a: usize, b: usize, c: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c)
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive4(a: usize, b: usize, c: usize, d: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize, d: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c, d)
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive5(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize, d: usize, e: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c, d, e)
}

#[no_mangle]
pub unsafe extern "sysv64" fn arrive6(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize {
	let mut fp: extern "win64" fn(a: usize, b: usize, c: usize, d: usize, e: usize, f: usize) -> usize;
	asm!("", out("rax") fp);
	fp(a, b, c, d, e, f)
}
