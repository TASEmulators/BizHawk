// linux syscall related things, for use in the waterbox
// There are various crates that contain these, but they're #[cfg]'ed to the HOST system.
// We want exactly the ones that waterbox guest MUSL uses, exactly the way they're defined there

use std::{ops::Try, fmt, ops::ControlFlow, ops::FromResidual};

/// the result of a syscall in Rust-friendly form; OK or errno
pub type SyscallResult = Result<(), SyscallError>;
/// map a syscall result as the kernel would return it
pub fn syscall_ret(result: SyscallResult) -> SyscallReturn {
	match result {
		Ok(()) => SyscallReturn::from_ok(0),
		Err(e) => SyscallReturn::from_error(e)
	}
}
/// map a syscall result as the kernel would return it
pub fn syscall_ret_val(result: Result<usize, SyscallError>) -> SyscallReturn {
	match result {
		Ok(v) => SyscallReturn::from_ok(v),
		Err(e) => SyscallReturn::from_error(e)
	}
}
pub fn syscall_ret_i64(result: Result<i64, SyscallError>) -> SyscallReturn {
	match result {
		Ok(v) => SyscallReturn::from_ok(v as usize),
		Err(e) => SyscallReturn::from_error(e)
	}
}
/// map a syscall result as the kernel would return it
pub fn syscall_err(result: SyscallError) -> SyscallReturn {
	SyscallReturn::from_error(result)
}
/// map a syscall result as the kernel would return it
pub fn syscall_ok(result: usize) -> SyscallReturn {
	SyscallReturn::from_ok(result)
}

#[repr(transparent)]
#[derive(Copy, Clone)]
pub struct SyscallReturn(pub usize);
impl SyscallReturn {
	pub const ERROR_THRESH: usize = -4096 as isize as usize;
	pub fn from_ok(v: usize) -> Self {
		Self::from_output(v)
	}
	pub fn from_error(v: SyscallError) -> Self {
		Self::from_residual(v)
	}
}
impl FromResidual for SyscallReturn {
	fn from_residual(v: SyscallError) -> Self {
		SyscallReturn(-v.0 as isize as usize)
	}
}
impl FromResidual<Result<std::convert::Infallible, SyscallError>> for SyscallReturn {
	fn from_residual(v: Result<std::convert::Infallible, SyscallError>) -> Self {
		match v {
			Err(zz) => SyscallReturn(-zz.0 as isize as usize),
		}
	}	
}
impl Try for SyscallReturn {
	type Output = usize;
	type Residual = SyscallError;
	fn branch(self) -> ControlFlow<Self::Residual, Self::Output> {
		if self.0 <= SyscallReturn::ERROR_THRESH {
			ControlFlow::Continue(self.0)
		} else {
			ControlFlow::Break(SyscallError(-(self.0 as i32)))
		}
	}
	fn from_output(v: Self::Output) -> Self {
		assert!(v <= SyscallReturn::ERROR_THRESH);
		SyscallReturn(v)
	}
}

macro_rules! lookup {
	($P:ident: $T:ident { $($N:ident = $E:expr; )+ }) => (
		$(pub const $N: $T = $T($E);)+
		pub fn $P(val: &$T) -> &'static str {
			match val {
				$($T($E) => stringify!($N),)+
				_ => "????"
			}
		}
	);
}

#[derive(Debug, Eq, PartialEq)]
#[repr(transparent)]
pub struct SyscallError(pub i32);
impl From<i32> for SyscallError {
	fn from(err: i32) -> SyscallError {
		SyscallError(err)
	}
}
impl fmt::Display for SyscallError {
	fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
		write!(f, "errno {}", lookup_errno(self))
	}
}
impl std::error::Error for SyscallError {}

lookup! { lookup_errno: SyscallError {
	EPERM = 1;
	ENOENT = 2;
	ESRCH = 3;
	EINTR = 4;
	EIO = 5;
	ENXIO = 6;
	E2BIG = 7;
	ENOEXEC = 8;
	EBADF = 9;
	ECHILD = 10;
	EAGAIN = 11;
	ENOMEM = 12;
	EACCES = 13;
	EFAULT = 14;
	ENOTBLK = 15;
	EBUSY = 16;
	EEXIST = 17;
	EXDEV = 18;
	ENODEV = 19;
	ENOTDIR = 20;
	EISDIR = 21;
	EINVAL = 22;
	ENFILE = 23;
	EMFILE = 24;
	ENOTTY = 25;
	ETXTBSY = 26;
	EFBIG = 27;
	ENOSPC = 28;
	ESPIPE = 29;
	EROFS = 30;
	EMLINK = 31;
	EPIPE = 32;
	EDOM = 33;
	ERANGE = 34;
	EDEADLK = 35;
	ENAMETOOLONG = 36;
	ENOLCK = 37;
	ENOSYS = 38;
	ENOTEMPTY = 39;
	ELOOP = 40;
	// EWOULDBLOCK = EAGAIN;
	ENOMSG = 42;
	EIDRM = 43;
	ECHRNG = 44;
	EL2NSYNC = 45;
	EL3HLT = 46;
	EL3RST = 47;
	ELNRNG = 48;
	EUNATCH = 49;
	ENOCSI = 50;
	EL2HLT = 51;
	EBADE = 52;
	EBADR = 53;
	EXFULL = 54;
	ENOANO = 55;
	EBADRQC = 56;
	EBADSLT = 57;
	// EDEADLOCK = EDEADLK;
	EBFONT = 59;
	ENOSTR = 60;
	ENODATA = 61;
	ETIME = 62;
	ENOSR = 63;
	ENONET = 64;
	ENOPKG = 65;
	EREMOTE = 66;
	ENOLINK = 67;
	EADV = 68;
	ESRMNT = 69;
	ECOMM = 70;
	EPROTO = 71;
	EMULTIHOP = 72;
	EDOTDOT = 73;
	EBADMSG = 74;
	EOVERFLOW = 75;
	ENOTUNIQ = 76;
	EBADFD = 77;
	EREMCHG = 78;
	ELIBACC = 79;
	ELIBBAD = 80;
	ELIBSCN = 81;
	ELIBMAX = 82;
	ELIBEXEC = 83;
	EILSEQ = 84;
	ERESTART = 85;
	ESTRPIPE = 86;
	EUSERS = 87;
	ENOTSOCK = 88;
	EDESTADDRREQ = 89;
	EMSGSIZE = 90;
	EPROTOTYPE = 91;
	ENOPROTOOPT = 92;
	EPROTONOSUPPORT = 93;
	ESOCKTNOSUPPORT = 94;
	EOPNOTSUPP = 95;
	// ENOTSUP = EOPNOTSUPP;
	EPFNOSUPPORT = 96;
	EAFNOSUPPORT = 97;
	EADDRINUSE = 98;
	EADDRNOTAVAIL = 99;
	ENETDOWN = 100;
	ENETUNREACH = 101;
	ENETRESET = 102;
	ECONNABORTED = 103;
	ECONNRESET = 104;
	ENOBUFS = 105;
	EISCONN = 106;
	ENOTCONN = 107;
	ESHUTDOWN = 108;
	ETOOMANYREFS = 109;
	ETIMEDOUT = 110;
	ECONNREFUSED = 111;
	EHOSTDOWN = 112;
	EHOSTUNREACH = 113;
	EALREADY = 114;
	EINPROGRESS = 115;
	ESTALE = 116;
	EUCLEAN = 117;
	ENOTNAM = 118;
	ENAVAIL = 119;
	EISNAM = 120;
	EREMOTEIO = 121;
	EDQUOT = 122;
	ENOMEDIUM = 123;
	EMEDIUMTYPE = 124;
	ECANCELED = 125;
	ENOKEY = 126;
	EKEYEXPIRED = 127;
	EKEYREVOKED = 128;
	EKEYREJECTED = 129;
	EOWNERDEAD = 130;
	ENOTRECOVERABLE = 131;
	ERFKILL = 132;
	EHWPOISON = 133;
}}

pub const S_IFMT: u32 = 0o0170000;

pub const S_IFDIR: u32 = 0o0040000;
pub const S_IFCHR: u32 = 0o0020000;
pub const S_IFBLK: u32 = 0o0060000;
pub const S_IFREG: u32 = 0o0100000;
pub const S_IFIFO: u32 = 0o0010000;
pub const S_IFLNK: u32 = 0o0120000;
pub const S_IFSOCK: u32 = 0o0140000;

pub const S_ISUID: u32 = 0o04000;
pub const S_ISGID: u32 = 0o02000;
pub const S_ISVTX: u32 = 0o01000;
pub const S_IRUSR: u32 = 0o0400;
pub const S_IWUSR: u32 = 0o0200;
pub const S_IXUSR: u32 = 0o0100;
pub const S_IRWXU: u32 = 0o0700;
pub const S_IRGRP: u32 = 0o0040;
pub const S_IWGRP: u32 = 0o0020;
pub const S_IXGRP: u32 = 0o0010;
pub const S_IRWXG: u32 = 0o0070;
pub const S_IROTH: u32 = 0o0004;
pub const S_IWOTH: u32 = 0o0002;
pub const S_IXOTH: u32 = 0o0001;
pub const S_IRWXO: u32 = 0o0007;

/// Kernel stat object
#[repr(C)]
#[derive(Default)]
pub struct KStat {
	pub st_dev: u64,
	pub st_ino: u64,
	pub st_nlink: u64,

	pub st_mode: u32,
	pub st_uid: u32,
	pub st_gid: u32,
	pub __pad0: u32,
	pub st_rdev: u64,
	pub st_size: i64,
	pub st_blksize: i64,
	pub st_blocks: i64,

	pub st_atime_sec: i64,
	pub st_atime_nsec: i64,
	pub st_mtime_sec: i64,
	pub st_mtime_nsec: i64,
	pub st_ctime_sec: i64,
	pub st_ctime_nsec: i64,
	pub __unused0: i64,
	pub __unused1: i64,
	pub __unused2: i64,
}

pub const SEEK_SET: i32 = 0;
pub const SEEK_CUR: i32 = 1;
pub const SEEK_END: i32 = 2;

pub const O_ACCMODE: i32 = O_PATH | O_RDONLY | O_WRONLY | O_RDWR;
pub const O_PATH: i32 = 0o010000000;
pub const O_RDONLY: i32 = 0;
pub const O_WRONLY: i32 = 1;
pub const O_RDWR: i32 = 2;

#[repr(C)]
pub struct Iovec {
	pub iov_base: usize,
	pub iov_len: usize,
}
impl Iovec {
	pub unsafe fn slice(&self) -> &[u8] {
		std::slice::from_raw_parts(self.iov_base as *const u8, self.iov_len)
	}
	pub unsafe fn slice_mut(&self) -> &mut [u8] {
		std::slice::from_raw_parts_mut(self.iov_base as *mut u8, self.iov_len)
	}
}

#[derive(Debug, Eq, PartialEq)]
#[repr(transparent)]
pub struct SyscallNumber(pub usize);

lookup! { lookup_syscall: SyscallNumber {
	NR_READ = 0;
	NR_WRITE = 1;
	NR_OPEN = 2;
	NR_CLOSE = 3;
	NR_STAT = 4;
	NR_FSTAT = 5;
	NR_LSTAT = 6;
	NR_POLL = 7;
	NR_LSEEK = 8;
	NR_MMAP = 9;
	NR_MPROTECT = 10;
	NR_MUNMAP = 11;
	NR_BRK = 12;
	NR_RT_SIGACTION = 13;
	NR_RT_SIGPROCMASK = 14;
	NR_RT_SIGRETURN = 15;
	NR_IOCTL = 16;
	NR_PREAD64 = 17;
	NR_PWRITE64 = 18;
	NR_READV = 19;
	NR_WRITEV = 20;
	NR_ACCESS = 21;
	NR_PIPE = 22;
	NR_SELECT = 23;
	NR_SCHED_YIELD = 24;
	NR_MREMAP = 25;
	NR_MSYNC = 26;
	NR_MINCORE = 27;
	NR_MADVISE = 28;
	NR_SHMGET = 29;
	NR_SHMAT = 30;
	NR_SHMCTL = 31;
	NR_DUP = 32;
	NR_DUP2 = 33;
	NR_PAUSE = 34;
	NR_NANOSLEEP = 35;
	NR_GETITIMER = 36;
	NR_ALARM = 37;
	NR_SETITIMER = 38;
	NR_GETPID = 39;
	NR_SENDFILE = 40;
	NR_SOCKET = 41;
	NR_CONNECT = 42;
	NR_ACCEPT = 43;
	NR_SENDTO = 44;
	NR_RECVFROM = 45;
	NR_SENDMSG = 46;
	NR_RECVMSG = 47;
	NR_SHUTDOWN = 48;
	NR_BIND = 49;
	NR_LISTEN = 50;
	NR_GETSOCKNAME = 51;
	NR_GETPEERNAME = 52;
	NR_SOCKETPAIR = 53;
	NR_SETSOCKOPT = 54;
	NR_GETSOCKOPT = 55;
	NR_CLONE = 56;
	NR_FORK = 57;
	NR_VFORK = 58;
	NR_EXECVE = 59;
	NR_EXIT = 60;
	NR_WAIT4 = 61;
	NR_KILL = 62;
	NR_UNAME = 63;
	NR_SEMGET = 64;
	NR_SEMOP = 65;
	NR_SEMCTL = 66;
	NR_SHMDT = 67;
	NR_MSGGET = 68;
	NR_MSGSND = 69;
	NR_MSGRCV = 70;
	NR_MSGCTL = 71;
	NR_FCNTL = 72;
	NR_FLOCK = 73;
	NR_FSYNC = 74;
	NR_FDATASYNC = 75;
	NR_TRUNCATE = 76;
	NR_FTRUNCATE = 77;
	NR_GETDENTS = 78;
	NR_GETCWD = 79;
	NR_CHDIR = 80;
	NR_FCHDIR = 81;
	NR_RENAME = 82;
	NR_MKDIR = 83;
	NR_RMDIR = 84;
	NR_CREAT = 85;
	NR_LINK = 86;
	NR_UNLINK = 87;
	NR_SYMLINK = 88;
	NR_READLINK = 89;
	NR_CHMOD = 90;
	NR_FCHMOD = 91;
	NR_CHOWN = 92;
	NR_FCHOWN = 93;
	NR_LCHOWN = 94;
	NR_UMASK = 95;
	NR_GETTIMEOFDAY = 96;
	NR_GETRLIMIT = 97;
	NR_GETRUSAGE = 98;
	NR_SYSINFO = 99;
	NR_TIMES = 100;
	NR_PTRACE = 101;
	NR_GETUID = 102;
	NR_SYSLOG = 103;
	NR_GETGID = 104;
	NR_SETUID = 105;
	NR_SETGID = 106;
	NR_GETEUID = 107;
	NR_GETEGID = 108;
	NR_SETPGID = 109;
	NR_GETPPID = 110;
	NR_GETPGRP = 111;
	NR_SETSID = 112;
	NR_SETREUID = 113;
	NR_SETREGID = 114;
	NR_GETGROUPS = 115;
	NR_SETGROUPS = 116;
	NR_SETRESUID = 117;
	NR_GETRESUID = 118;
	NR_SETRESGID = 119;
	NR_GETRESGID = 120;
	NR_GETPGID = 121;
	NR_SETFSUID = 122;
	NR_SETFSGID = 123;
	NR_GETSID = 124;
	NR_CAPGET = 125;
	NR_CAPSET = 126;
	NR_RT_SIGPENDING = 127;
	NR_RT_SIGTIMEDWAIT = 128;
	NR_RT_SIGQUEUEINFO = 129;
	NR_RT_SIGSUSPEND = 130;
	NR_SIGALTSTACK = 131;
	NR_UTIME = 132;
	NR_MKNOD = 133;
	NR_USELIB = 134;
	NR_PERSONALITY = 135;
	NR_USTAT = 136;
	NR_STATFS = 137;
	NR_FSTATFS = 138;
	NR_SYSFS = 139;
	NR_GETPRIORITY = 140;
	NR_SETPRIORITY = 141;
	NR_SCHED_SETPARAM = 142;
	NR_SCHED_GETPARAM = 143;
	NR_SCHED_SETSCHEDULER = 144;
	NR_SCHED_GETSCHEDULER = 145;
	NR_SCHED_GET_PRIORITY_MAX = 146;
	NR_SCHED_GET_PRIORITY_MIN = 147;
	NR_SCHED_RR_GET_INTERVAL = 148;
	NR_MLOCK = 149;
	NR_MUNLOCK = 150;
	NR_MLOCKALL = 151;
	NR_MUNLOCKALL = 152;
	NR_VHANGUP = 153;
	NR_MODIFY_LDT = 154;
	NR_PIVOT_ROOT = 155;
	NR__SYSCTL = 156;
	NR_PRCTL = 157;
	NR_ARCH_PRCTL = 158;
	NR_ADJTIMEX = 159;
	NR_SETRLIMIT = 160;
	NR_CHROOT = 161;
	NR_SYNC = 162;
	NR_ACCT = 163;
	NR_SETTIMEOFDAY = 164;
	NR_MOUNT = 165;
	NR_UMOUNT2 = 166;
	NR_SWAPON = 167;
	NR_SWAPOFF = 168;
	NR_REBOOT = 169;
	NR_SETHOSTNAME = 170;
	NR_SETDOMAINNAME = 171;
	NR_IOPL = 172;
	NR_IOPERM = 173;
	NR_CREATE_MODULE = 174;
	NR_INIT_MODULE = 175;
	NR_DELETE_MODULE = 176;
	NR_GET_KERNEL_SYMS = 177;
	NR_QUERY_MODULE = 178;
	NR_QUOTACTL = 179;
	NR_NFSSERVCTL = 180;
	NR_GETPMSG = 181;
	NR_PUTPMSG = 182;
	NR_AFS_SYSCALL = 183;
	NR_TUXCALL = 184;
	NR_SECURITY = 185;
	NR_GETTID = 186;
	NR_READAHEAD = 187;
	NR_SETXATTR = 188;
	NR_LSETXATTR = 189;
	NR_FSETXATTR = 190;
	NR_GETXATTR = 191;
	NR_LGETXATTR = 192;
	NR_FGETXATTR = 193;
	NR_LISTXATTR = 194;
	NR_LLISTXATTR = 195;
	NR_FLISTXATTR = 196;
	NR_REMOVEXATTR = 197;
	NR_LREMOVEXATTR = 198;
	NR_FREMOVEXATTR = 199;
	NR_TKILL = 200;
	NR_TIME = 201;
	NR_FUTEX = 202;
	NR_SCHED_SETAFFINITY = 203;
	NR_SCHED_GETAFFINITY = 204;
	NR_SET_THREAD_AREA = 205;
	NR_IO_SETUP = 206;
	NR_IO_DESTROY = 207;
	NR_IO_GETEVENTS = 208;
	NR_IO_SUBMIT = 209;
	NR_IO_CANCEL = 210;
	NR_GET_THREAD_AREA = 211;
	NR_LOOKUP_DCOOKIE = 212;
	NR_EPOLL_CREATE = 213;
	NR_EPOLL_CTL_OLD = 214;
	NR_EPOLL_WAIT_OLD = 215;
	NR_REMAP_FILE_PAGES = 216;
	NR_GETDENTS64 = 217;
	NR_SET_TID_ADDRESS = 218;
	NR_RESTART_SYSCALL = 219;
	NR_SEMTIMEDOP = 220;
	NR_FADVISE64 = 221;
	NR_TIMER_CREATE = 222;
	NR_TIMER_SETTIME = 223;
	NR_TIMER_GETTIME = 224;
	NR_TIMER_GETOVERRUN = 225;
	NR_TIMER_DELETE = 226;
	NR_CLOCK_SETTIME = 227;
	NR_CLOCK_GETTIME = 228;
	NR_CLOCK_GETRES = 229;
	NR_CLOCK_NANOSLEEP = 230;
	NR_EXIT_GROUP = 231;
	NR_EPOLL_WAIT = 232;
	NR_EPOLL_CTL = 233;
	NR_TGKILL = 234;
	NR_UTIMES = 235;
	NR_VSERVER = 236;
	NR_MBIND = 237;
	NR_SET_MEMPOLICY = 238;
	NR_GET_MEMPOLICY = 239;
	NR_MQ_OPEN = 240;
	NR_MQ_UNLINK = 241;
	NR_MQ_TIMEDSEND = 242;
	NR_MQ_TIMEDRECEIVE = 243;
	NR_MQ_NOTIFY = 244;
	NR_MQ_GETSETATTR = 245;
	NR_KEXEC_LOAD = 246;
	NR_WAITID = 247;
	NR_ADD_KEY = 248;
	NR_REQUEST_KEY = 249;
	NR_KEYCTL = 250;
	NR_IOPRIO_SET = 251;
	NR_IOPRIO_GET = 252;
	NR_INOTIFY_INIT = 253;
	NR_INOTIFY_ADD_WATCH = 254;
	NR_INOTIFY_RM_WATCH = 255;
	NR_MIGRATE_PAGES = 256;
	NR_OPENAT = 257;
	NR_MKDIRAT = 258;
	NR_MKNODAT = 259;
	NR_FCHOWNAT = 260;
	NR_FUTIMESAT = 261;
	NR_NEWFSTATAT = 262;
	NR_UNLINKAT = 263;
	NR_RENAMEAT = 264;
	NR_LINKAT = 265;
	NR_SYMLINKAT = 266;
	NR_READLINKAT = 267;
	NR_FCHMODAT = 268;
	NR_FACCESSAT = 269;
	NR_PSELECT6 = 270;
	NR_PPOLL = 271;
	NR_UNSHARE = 272;
	NR_SET_ROBUST_LIST = 273;
	NR_GET_ROBUST_LIST = 274;
	NR_SPLICE = 275;
	NR_TEE = 276;
	NR_SYNC_FILE_RANGE = 277;
	NR_VMSPLICE = 278;
	NR_MOVE_PAGES = 279;
	NR_UTIMENSAT = 280;
	NR_EPOLL_PWAIT = 281;
	NR_SIGNALFD = 282;
	NR_TIMERFD_CREATE = 283;
	NR_EVENTFD = 284;
	NR_FALLOCATE = 285;
	NR_TIMERFD_SETTIME = 286;
	NR_TIMERFD_GETTIME = 287;
	NR_ACCEPT4 = 288;
	NR_SIGNALFD4 = 289;
	NR_EVENTFD2 = 290;
	NR_EPOLL_CREATE1 = 291;
	NR_DUP3 = 292;
	NR_PIPE2 = 293;
	NR_INOTIFY_INIT1 = 294;
	NR_PREADV = 295;
	NR_PWRITEV = 296;
	NR_RT_TGSIGQUEUEINFO = 297;
	NR_PERF_EVENT_OPEN = 298;
	NR_RECVMMSG = 299;
	NR_FANOTIFY_INIT = 300;
	NR_FANOTIFY_MARK = 301;
	NR_PRLIMIT64 = 302;
	NR_NAME_TO_HANDLE_AT = 303;
	NR_OPEN_BY_HANDLE_AT = 304;
	NR_CLOCK_ADJTIME = 305;
	NR_SYNCFS = 306;
	NR_SENDMMSG = 307;
	NR_SETNS = 308;
	NR_GETCPU = 309;
	NR_PROCESS_VM_READV = 310;
	NR_PROCESS_VM_WRITEV = 311;
	NR_KCMP = 312;
	NR_FINIT_MODULE = 313;
	NR_SCHED_SETATTR = 314;
	NR_SCHED_GETATTR = 315;
	NR_RENAMEAT2 = 316;
	NR_SECCOMP = 317;
	NR_GETRANDOM = 318;
	NR_MEMFD_CREATE = 319;
	NR_KEXEC_FILE_LOAD = 320;
	NR_BPF = 321;
	NR_EXECVEAT = 322;
	NR_USERFAULTFD = 323;
	NR_MEMBARRIER = 324;
	NR_MLOCK2 = 325;
	NR_COPY_FILE_RANGE = 326;
	NR_PREADV2 = 327;
	NR_PWRITEV2 = 328;
	NR_PKEY_MPROTECT = 329;
	NR_PKEY_ALLOC = 330;
	NR_PKEY_FREE = 331;
	NR_STATX = 332;
	NR_IO_PGETEVENTS = 333;
	NR_RSEQ = 334;
	NR_PIDFD_SEND_SIGNAL = 424;
	NR_IO_URING_SETUP = 425;
	NR_IO_URING_ENTER = 426;
	NR_IO_URING_REGISTER = 427;
	NR_OPEN_TREE = 428;
	NR_MOVE_MOUNT = 429;
	NR_FSOPEN = 430;
	NR_FSCONFIG = 431;
	NR_FSMOUNT = 432;
	NR_FSPICK = 433;
	NR_PIDFD_OPEN = 434;
	NR_CLONE3 = 435;
	NR_WBX_CLONE = 2000;
}}

pub const MAP_FAILED: usize = !0;

pub const MAP_SHARED: usize = 0x01;
pub const MAP_PRIVATE: usize = 0x02;
pub const MAP_SHARED_VALIDATE: usize = 0x03;
pub const MAP_TYPE: usize = 0x0f;
pub const MAP_FIXED: usize = 0x10;
pub const MAP_ANON: usize = 0x20;
pub const MAP_32BIT: usize = 0x40;
pub const MAP_ANONYMOUS: usize = MAP_ANON;
pub const MAP_NORESERVE: usize = 0x4000;
pub const MAP_GROWSDOWN: usize = 0x0100;
pub const MAP_DENYWRITE: usize = 0x0800;
pub const MAP_EXECUTABLE: usize = 0x1000;
pub const MAP_LOCKED: usize = 0x2000;
pub const MAP_POPULATE: usize = 0x8000;
pub const MAP_NONBLOCK: usize = 0x10000;
pub const MAP_STACK: usize = 0x20000;
pub const MAP_HUGETLB: usize = 0x40000;
pub const MAP_SYNC: usize = 0x80000;
pub const MAP_FIXED_NOREPLACE: usize = 0x100000;
pub const MAP_FILE: usize = 0;

pub const MAP_HUGE_SHIFT: usize = 26;
pub const MAP_HUGE_MASK: usize = 0x3f;
pub const MAP_HUGE_64KB: usize = 16 << 26;
pub const MAP_HUGE_512KB: usize = 19 << 26;
pub const MAP_HUGE_1MB: usize = 20 << 26;
pub const MAP_HUGE_2MB: usize = 21 << 26;
pub const MAP_HUGE_8MB: usize = 23 << 26;
pub const MAP_HUGE_16MB: usize = 24 << 26;
pub const MAP_HUGE_32MB: usize = 25 << 26;
pub const MAP_HUGE_256MB: usize = 28 << 26;
pub const MAP_HUGE_512MB: usize = 29 << 26;
pub const MAP_HUGE_1GB: usize = 30 << 26;
pub const MAP_HUGE_2GB: usize = 31 << 26;
pub const MAP_HUGE_16GB: usize = 34 << 26;

pub const PROT_NONE: usize = 0;
pub const PROT_READ: usize = 1;
pub const PROT_WRITE: usize = 2;
pub const PROT_EXEC: usize = 4;
pub const PROT_GROWSDOWN: usize = 0x01000000;
pub const PROT_GROWSUP: usize = 0x02000000;

pub const MS_ASYNC: usize = 1;
pub const MS_INVALIDATE: usize = 2;
pub const MS_SYNC: usize = 4;

pub const MCL_CURRENT: usize = 1;
pub const MCL_FUTURE: usize = 2;
pub const MCL_ONFAULT: usize = 4;

pub const POSIX_MADV_NORMAL: usize = 0;
pub const POSIX_MADV_RANDOM: usize = 1;
pub const POSIX_MADV_SEQUENTIAL: usize = 2;
pub const POSIX_MADV_WILLNEED: usize = 3;
pub const POSIX_MADV_DONTNEED: usize = 4;

pub const MADV_NORMAL: usize = 0;
pub const MADV_RANDOM: usize = 1;
pub const MADV_SEQUENTIAL: usize = 2;
pub const MADV_WILLNEED: usize = 3;
pub const MADV_DONTNEED: usize = 4;
pub const MADV_FREE: usize = 8;
pub const MADV_REMOVE: usize = 9;
pub const MADV_DONTFORK: usize = 10;
pub const MADV_DOFORK: usize = 11;
pub const MADV_MERGEABLE: usize = 12;
pub const MADV_UNMERGEABLE: usize = 13;
pub const MADV_HUGEPAGE: usize = 14;
pub const MADV_NOHUGEPAGE: usize = 15;
pub const MADV_DONTDUMP: usize = 16;
pub const MADV_DODUMP: usize = 17;
pub const MADV_WIPEONFORK: usize = 18;
pub const MADV_KEEPONFORK: usize = 19;
pub const MADV_COLD: usize = 20;
pub const MADV_PAGEOUT: usize = 21;
pub const MADV_HWPOISON: usize = 100;
pub const MADV_SOFT_OFFLINE: usize = 101;

pub const MREMAP_MAYMOVE: usize = 1;
pub const MREMAP_FIXED: usize = 2;

pub const MLOCK_ONFAULT: usize = 0x01;

pub const MFD_CLOEXEC: usize = 0x0001;
pub const MFD_ALLOW_SEALING: usize = 0x0002;
pub const MFD_HUGETLB: usize = 0x0004;

#[repr(C)]
pub struct TimeSpec {
	pub tv_sec: i64,
	pub tv_nsec: i64,
}

pub const FUTEX_WAITERS: u32 = 0x80000000;
pub const FUTEX_OWNER_DIED: u32 = 0x40000000;
pub const FUTEX_TID_MASK: u32 = 0x3fffffff;

pub const FUTEX_WAIT: i32 = 0;
pub const FUTEX_WAKE: i32 = 1;
pub const FUTEX_FD: i32 = 2;
pub const FUTEX_REQUEUE: i32 = 3;
pub const FUTEX_CMP_REQUEUE: i32 = 4;
pub const FUTEX_WAKE_OP: i32 = 5;
pub const FUTEX_LOCK_PI: i32 = 6;
pub const FUTEX_UNLOCK_PI: i32 = 7;
pub const FUTEX_TRYLOCK_PI: i32 = 8;
pub const FUTEX_WAIT_BITSET: i32 = 9;

pub const FUTEX_PRIVATE: i32 = 128;

pub const FUTEX_CLOCK_REALTIME: i32 = 256;
