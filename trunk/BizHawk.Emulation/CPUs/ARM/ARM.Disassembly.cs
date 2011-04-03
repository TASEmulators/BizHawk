using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.Emulation.CPUs.ARM
{
	partial class ARM
	{
		public class DisassemblyOptions
		{
			//TODO - refactor these to NumberOptions or somesuch and have several of those
			//0 = unpadded ; 1,2,4 = numbytes: -1 = no hex
			public int SVC_hex;
			public bool SVC_hexPrefix, SVC_literalPrefix, SVC_caps;
			public bool SVC_showName = true;

			public void SetNstyle()
			{
				SVC_hex = 0;
				SVC_hexPrefix = false;
				SVC_literalPrefix = false;
				SVC_caps = true;
				hideZeroOffset = true;
				//showExplicitAccumulateRegisters = false;
			}

			public bool showoptadd = false;

			//fixes retarded debugger label format by adding #0x in front of it
			public bool fix_nstyle_label = true;

			//TODO - subsume nstyle

			//show all registers when accumulating values, i.e. add r0, r0, #4 (instead of add r0, #4)
			//TODO - obseleted by rd~rx approach
			public bool showExplicitAccumulateRegisters = true;

			//hide offsets of #0 (set to true for more terseness with no real loss of information)
			public bool hideZeroOffset = false;

			//compute PC relative labels for certain operations (like LDR)
			//public bool computePCLabel = true;

			//consider:
			//in RVCT many (all?) thumb instructions dont show {S} suffix in thumb mode, because it is sort of redundant (since they always apply in many thumb instructions)
			//but this is gay. well, consider making it an option
		}

		public class ProcessorOptions
		{
			public void SetNStyle()
			{
				Thumb_LSL_immediate_T1_0_is_MOV_register_T2 = false;
			}

			public bool Thumb_LSL_immediate_T1_0_is_MOV_register_T2 = true;
		}

		public DisassemblyOptions disopt = new DisassemblyOptions();
		public ProcessorOptions procopt = new ProcessorOptions();

		bool DIS_Check(object[] args, int argnum, string str, string find)
		{
			if (args.Length <= argnum) return false;
			int index = str.IndexOf(find);
			if (index == -1) return false;
			return true;
		}
		string disformat_num(object item, bool caps, bool literalprefix, bool hexprefix, int hex)
		{
			string hexformat;
			switch (hex)
			{
				case -1: hexformat = "{0}"; break;
				case 0: hexformat = "{0:x}"; break;
				case 1: hexformat = "{0:x2}"; break;
				case 2: hexformat = "{0:x4}"; break;
				case 4: hexformat = "{0:x8}"; break;
				default: throw new ArgumentException();
			}
			string value = string.Format(hexformat, item);
			if (caps) value = value.ToUpper();
			string ret = string.Format("{0}{1}{2}", literalprefix ? "#" : "", hexprefix ? "0x" : "", value);
			return ret;
		}
		uint DIS(string opcode, string format, params object[] args)
		{
			//options: 
			//capital reg names
			//capital mnemonic
			//tab between mnemonic and arguments (always? sometimes)
			//0 front-pad hex arguments
			const bool capMnemonic = true;

			if (DIS_Check(args, 0, opcode, "/s0/")) opcode = opcode.Replace("/s0/", (bool)args[0] ? "s" : "");

			if (_CurrentCond() == 14)
				opcode = opcode.Replace("/c/", "");
			else opcode = opcode.Replace("/c/", CC_strings[_CurrentCond()]);

			if (capMnemonic) opcode = opcode.ToUpper();

			string ret = string.Format(opcode + " " + format, args);

			if (DIS_Check(args, 0, ret, "/a0/")) ret = ret.Replace("/a0/", string.Format("{0:x}", args[0])); //address format
			if (DIS_Check(args, 1, ret, "/a1/")) ret = ret.Replace("/a1/", string.Format("{0:x}", args[1]));
			if (DIS_Check(args, 0, ret, "/r0/")) ret = ret.Replace("/r0/", Reg_names[(uint)args[0]].ToLower());
			if (DIS_Check(args, 1, ret, "/r1/")) ret = ret.Replace("/r1/", Reg_names[(uint)args[1]].ToLower());
			if (DIS_Check(args, 2, ret, "/r2/")) ret = ret.Replace("/r2/", Reg_names[(uint)args[2]].ToLower());
			if (DIS_Check(args, 0, ret, "/imm8_0/")) ret = ret.Replace("/imm8_0/", string.Format("#{0}", (uint)args[0]));
			if (DIS_Check(args, 1, ret, "/imm8_1/")) ret = ret.Replace("/imm8_1/", string.Format("#{0}", (uint)args[1]));
			if (DIS_Check(args, 1, ret, "/uimm32h_1/")) ret = ret.Replace("/uimm32h_1/", string.Format("#0x{0:x}", (uint)args[1]));
			if (DIS_Check(args, 0, ret, "/label0/")) ret = ret.Replace("/label0/", string.Format("{0:x}", args[0]));
			if (DIS_Check(args, 1, ret, "/label1/")) ret = ret.Replace("/label1/", string.Format("{0:x}", args[1]));
			if (DIS_Check(args, 0, ret, "/svc0/")) ret = ret.Replace("/svc0/", disformat_num(args[0], disopt.SVC_caps, disopt.SVC_literalPrefix, disopt.SVC_hexPrefix, disopt.SVC_hex));
			if (DIS_Check(args, 0, ret, "/const0/")) ret = ret.Replace("/const0/", "#0x" + string.Format("{0:x}", args[0]).ToUpper());
			if (DIS_Check(args, 1, ret, "/const1/")) ret = ret.Replace("/const1/", "#0x" + string.Format("{0:x}", args[1]).ToUpper());
			if (DIS_Check(args, 2, ret, "/const2/")) ret = ret.Replace("/const2/", "#0x" + string.Format("{0:x}", args[2]).ToUpper());
			//TODO - run constN through the disformat scheme
			ret = DIS_Reglist(ret, args);
			disassembly = ret;
			return 0;
		}

		string DISNEW_optaddsub(object value)
		{
			return (bool)value ? (disopt.showoptadd ? "+" : "") : "-";
		}

		uint DISNEW(string opcode, string format, params object[] args)
		{
			bool hasComment = false;

			const bool capMnemonic = true;

			int index = 0;
			int tagindex = -1;
			int argindex = 0;
			uint regReport = 0;

			//-----
			//work on opcode
			if (opcode.IndexOf("<s?>") != -1)
			{
				bool s = (bool)args[argindex++];
				opcode = opcode.Replace("<s?>", s ? "s" : "");
			}

			//TODO - look for cmaybe and get rid of them.
			//alternatively, do a good job placing them and just always choose to put them there.
			//well, we can do that as a separate polish pass later
			bool cmaybe = (opcode.IndexOf("<c?>") != -1);
			bool usec = true;
			if (cmaybe)
				usec = (bool)args[argindex++];
			string crepl = cmaybe ? "<c?>" : "<c>";

			if (usec)
			{
				if (_CurrentCond() == 14)
					opcode = opcode.Replace(crepl, "");
				else opcode = opcode.Replace(crepl, CC_strings[_CurrentCond()]);
			}
			else opcode = opcode.Replace(crepl, "");
			//---------

			string cpcomment = null;
			while (index < format.Length)
			{
				if (tagindex != -1)
				{
					if (format[index] == '>')
					{
						int len = index - tagindex - 1;
						string item = format.Substring(tagindex + 1, len).ToLower();

						switch (item)
						{
							case "rt":
							case "rm":
							case "rn":
							case "rd":
								{
									item = Reg_names[(uint)args[argindex++]].ToLower();
									break;
								}

							case "rt!":
							case "rm!":
							case "rn!":
							case "rd!":
							case "sp!":
								{
									uint reg;
									if (item == "sp!") reg = 13;
									else reg = (uint)args[argindex++];
									item = Reg_names[reg].ToLower();
									regReport |= (uint)(1 << (int)reg);
									break;
								}

							case "{rd!~rm, }":
							case "{rd!~rn, }":
								{
									uint rd = (uint)args[argindex++];
									uint rx = (uint)args[argindex++];
									if (disopt.showExplicitAccumulateRegisters || rd != rx)
										item = string.Format("{0}, ", Reg_names[rd].ToLower());
									else item = "";
									break;
								}

							case "fpscr": item = nstyle ? "fpscr" : "FPSCR"; break;

							case "const": item = string.Format("0x{0:x}", args[argindex++]); break;
							case "imm": item = string.Format("0x{0:x}", args[argindex++]); break;
							case "imm5": item = string.Format("0x{0:x}", args[argindex++]); break;

							case "{ ,#imm}":
								{
									uint temp = (uint)args[argindex++];
									if (temp == 0) item = "";
									else item = string.Format(",#0x{0:x}", temp);
									break;
								}
							case "{,+/-#imm}":
								{
									uint temp = (uint)args[argindex + 1];
									if (temp == 0 && disopt.hideZeroOffset) item = "";
									else item = string.Format(",#{0}0x{1:x}", DISNEW_optaddsub(args[argindex]), temp);
									argindex++;
									break;
								}
							//case "labelaccess":  //same as label but with [] around it 
							case "label":
								if (disopt.fix_nstyle_label)
									item = string.Format("#0x{0:x}", args[argindex++]);
								else
									item = string.Format("{0:x}", args[argindex++]);
								break;
							case "%08x": item = string.Format("0x{0:x8}", args[argindex++]); break;
							case "optaddsub":
							case "+/-": item = DISNEW_optaddsub((bool)args[argindex++]); break;

							case "{, shift}":
								{
									//TODO - consider whether it is necessary to pass in arguments (can they always be pulled from shift_n and shift_t? i think so
									SRType sr = (SRType)args[argindex++];
									int shift_n = (int)args[argindex++];
									switch (sr)
									{
										case SRType.LSL:
											if (shift_n == 0)
											{
												//special case for non-rotated things. theyre usually encoded this way
												item = "";
												break;
											}
											item = ", LSL " + shift_n.ToString(); break;
										case SRType.LSR: item = ", LSR " + shift_n.ToString(); break;
										case SRType.ASR: item = ", ASR " + shift_n.ToString(); break;
										case SRType.ROR: item = ", ROR " + shift_n.ToString(); break;
										case SRType.RRX: item = ", RRX"; break;
									}
									break;
								}
							case "{, rotation}":
								{
									uint rotation = (uint)args[argindex++] * 8;
									if (rotation == 0) item = "";
									else item = "ROR #" + rotation;
									break;
								}

							case "registers":
								item = DISNEW_Reglist((uint)args[argindex++]);
								break;
							case "svc":
								{
									uint svc = (uint)args[argindex++];
									if (nstyle) opcode = opcode.Replace("XXX", "SWI");
									else opcode = opcode.Replace("XXX", "SVC");
									item = disformat_num(svc, disopt.SVC_caps, disopt.SVC_literalPrefix, disopt.SVC_hexPrefix, disopt.SVC_hex);
									if (disopt.SVC_showName) { item += " ; " + sys.svc_name(svc); hasComment = true; }
									break;
								}
							case "{wback!}":
								if ((bool)args[argindex++])
									item = "!";
								else item = "";
								break;

							case "coproc":
								item = "cp" + args[argindex++].ToString();
								break;
							case "opc1":
							case "opc2":
								item = "#" + args[argindex++].ToString();
								break;
							case "coproc_rt!":
								{
									uint reg = (uint)args[argindex++];
									if (reg == 15)
									{
										item = "APSR_nzcv";
									}
									else
										regReport |= (uint)(1 << (int)reg);
									item = Reg_names[reg].ToLower();
									break;
								}
							case "crn":
							case "crm":
								item = "c" + args[argindex++].ToString();
								break;
							case "{ ;cp_comment}":
								{
									item = "";
									cpcomment = args[argindex++] as string;
								}

								break;

							default: item = "<" + item + ">"; break;
						}

						format = format.Substring(0, tagindex) + item + format.Substring(tagindex + len + 2);
						index = tagindex + item.Length - 1;
						tagindex = -1;
					}
				}
				else
					if (format[index] == '<')
						tagindex = index;
				index++;
			}

			if (capMnemonic) opcode = opcode.ToUpper();

			disassembly = opcode + " " + format;
			if (unpredictable)
			{
				disassembly = disassembly + " ; UNPREDICTABLE";
				hasComment = true;
			}

			//report any relevant registers
			bool hasRegReport = false;
			for (int i = 0; i < 16; i++)
			{
				uint doit = regReport & 1;
				regReport >>= 1;
				if (doit == 1)
				{
					if (!hasComment) disassembly += " ; ";
					if (hasRegReport) disassembly += ", ";
					hasComment = true;
					hasRegReport = true;
					disassembly += string.Format("{0}={1:x8}", Reg_names[i].ToLower(), r[i]);
				}
			}

			if (!string.IsNullOrEmpty(cpcomment))
			{
				disassembly += " ;" + cpcomment;
			}

			return 0;
		}

		string DIS_Reglist(string str, object[] args)
		{
			int index = str.IndexOf("/reglist/");
			if (index == -1) return str;
			uint regs = (uint)args[0];
			StringBuilder sb = new StringBuilder(32);
			sb.Append("{");
			//TODO - coalesce these to the range style!!
			bool first = true;
			for (int i = 0; i <= 15; i++)
			{
				if (_.BITN(i, regs) == 1)
				{
					if (!first) sb.Append(",");
					sb.Append(Reg_names[i].ToLower());
					first = false;
				}
			}
			sb.Append("}");
			return str.Replace("/reglist/", sb.ToString());
		}

		//TODO - unit test this
		unsafe string DISNEW_Reglist(uint regs)
		{
			StringBuilder sb = new StringBuilder(32);
			//TODO - coalesce these to the range style!!
			const int firstname = 13;
			bool first = true;
			int range = -1;
			int lastrange = -1;
			int* ra = stackalloc int[8];
			int* rb = stackalloc int[8];
			for (int i = 0; i <= 15; i++)
			{
				bool bit = _.BITN(i, regs) != 0;
				if (bit && i < firstname)
				{
					if (lastrange == -1)
					{
						lastrange = i;
					}
					else
					{

					}
				}
				if (!bit || i >= firstname)
				{
					if (bit && i >= firstname)
					{
						range++;
						ra[range] = i;
						rb[range] = i;
					}
					if (lastrange != -1)
					{
						range++;
						ra[range] = lastrange;
						rb[range] = i - 1;
						lastrange = -1;
					}
				}
			}

			sb.Append("{");
			for (int i = 0; i <= range; i++)
			{
				int a = ra[i];
				int b = rb[i];
				if (!first) sb.Append(",");
				if (a == b) sb.Append(Reg_names[a].ToLower());
				else sb.AppendFormat("{0}-{1}", Reg_names[a].ToLower(), Reg_names[b].ToLower());
				first = false;
			}
			sb.Append("}");

			//sb.Append("{");
			//for (int i = 0; i <= 15; i++)
			//{
			//    if (_.BITN(i, regs) == 1)
			//    {
			//        if (!first) sb.Append(",");
			//        sb.Append(Reg_names[i].ToLower());
			//        first = false;
			//    }
			//}
			//sb.Append("}");

			return sb.ToString();
		}

	}
}