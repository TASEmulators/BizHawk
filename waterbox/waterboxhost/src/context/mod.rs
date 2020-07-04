

#[repr(C)]
pub struct JmpBuf {
	/// Saved right after the `call` instr
	pub rsp: usize,
	pub rbp: usize,
	pub rbx: usize,
	pub r12: usize,
	pub r13: usize,
	pub r14: usize,
	pub r15: usize,
}

#[repr(C)]
pub struct GuestCall {
	/// rax
	pub nr: usize,
	/// rdi
	pub a1: usize,
	/// rsi
	pub a2: usize,
	/// rdx
	pub a3: usize,
	/// rcx
	pub a4: usize,
	/// r8
	pub a5: usize,
	/// r9
	pub a6: usize,
}

pub struct Context {
	pub host: JmpBuf,
	pub guest: JmpBuf,
	pub call: GuestCall
}
impl Context {
	pub fn new() -> Context {

	}
}