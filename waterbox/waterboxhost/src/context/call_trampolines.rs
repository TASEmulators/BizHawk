

use crate::host::ActivatedWaterboxHost;

// each generated trampoline should look like this:
// 49 ba .. .. .. .. .. .. .. ..             mov r10, <host>
// 49 bb .. .. .. .. .. .. .. ..             mov r11, <native_rip>
// 49 b8 .. .. .. .. .. .. .. ..             mov rax, <call_trampoline_impl>
// ff e0                                     jmp rax

// every time we remount, the tramponlines need to be updated with the new r10 value



/// trampoline impl inputs:
/// convention specific reg/stack: 6 args
/// r10: &mut ActivatedWaterboxHost
/// r11: native rip
/// trampoline impl outputs:
/// call ActivatedWaterboxHost.run_guest_thread
/// return rax to caller


#[cfg(unix)]
#[naked]
unsafe fn call_trampoline_impl() {
	asm!(
		"sub rsp, 0x38",
		"mov [rsp + 0x08], rdi",
		"mov [rsp + 0x10], rsi",
		"mov [rsp + 0x18], rdx",
		"mov [rsp + 0x20], rcx",
		"mov [rsp + 0x28], r8",
		"mov [rsp + 0x30], r9",
		"mov rdi, r10",
		"mov rsi, r11",
		"mov rdx, rsp",
		"call {rgt}",
		"add rsp, 0x38",
		"ret",
		rgt = sym ActivatedWaterboxHost::run_guest_thread
	);
}

#[cfg(windows)]
#[naked]
unsafe fn call_trampoline_impl() {
	asm!(
		"sub rsp, 0x38",
		"mov [rsp + 0x08], rcx",
		"mov [rsp + 0x10], rdx",
		"mov [rsp + 0x18], r8",
		"mov [rsp + 0x20], r9",
		"mov [rsp + 0x28], [rsp + 0x60]",
		"mov [rsp + 0x30], [rsp + 0x68]",
		"mov rcx, r10",
		"mov rdx, r11",
		"mov r8, rsp",
		"sub rsp, 0x20",
		"call {rgt}",
		"add rsp, 0x58",
		"ret",
		rgt = sym ActivatedWaterboxHost::run_guest_thread
	);
}
