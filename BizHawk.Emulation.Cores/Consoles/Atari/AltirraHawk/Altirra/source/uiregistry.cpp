//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>
#include <vd2/system/file.h>
#include <vd2/system/registry.h>
#include <vd2/system/registrymemory.h>
#include "settings.h"

void ATUILoadRegistry(const wchar_t *path) {
	VDTextInputFile ini(path);

	vdautoptr<VDRegistryKey> key;
	VDStringA token;
	VDStringW strvalue;
	vdfastvector<char> binvalue;
	while(const char *s = ini.GetNextLine()) {
		while(*s == ' ' || *s == '\t')
			++s;

		if (!*s || *s == ';')
			continue;

		if (*s == '[') {
			key = NULL;

			++s;
			const char *end = strchr(s, ']');
			if (!end)
				continue;

			if (!strncmp(s, "Shared\\", 7)) {
				token.assign(s + 7, end);
				key = new VDRegistryKey(token.c_str(), true, true);
			} else if (!strncmp(s, "User\\", 5)) {
				token.assign(s + 5, end);
				key = new VDRegistryKey(token.c_str(), false, true);
			}

			continue;
		}

		if (!key)
			continue;

		// expect lines of form:
		//
		//	"key" = <int-value>
		//	"key" = "<string-value>"
		//	"key" = [<binary-value>]

		if (*s != '"')
			continue;

		++s;
		const char *nameStart = s;
		while(*s != '"')
			++s;

		if (!*s)
			continue;

		token.assign(nameStart, s);
		++s;

		while(*s == ' ' || *s == '\t')
			++s;

		if (*s != '=')
			continue;
		++s;

		while(*s == ' ' || *s == '\t')
			++s;

		static const uint8 kUnhexTab[32]={
			0,10,11,12,13,14,15,0,0,0,0,0,0,0,0,0,
			0,1,2,3,4,5,6,7,8,9,0,0,0,0,0,0
		};

		if (*s == '-' || (*s >= '0' && *s <= '9')) {
			// integer
			long v = strtol(s, NULL, 0);

			key->setInt(token.c_str(), (int)v);
		} else if (*s == '"') {
			// string
			++s;

			strvalue.clear();
			for(;;) {
				char c = *s++;

				if (!c || c == '"')
					break;

				if (c == '\\') {
					c = *s++;

					if (!c)
						break;

					switch(c) {
						case 'n':	c = '\n'; break;
						case 't':	c = '\t'; break;
						case 'v':	c = '\v'; break;
						case 'b':	c = '\b'; break;
						case 'r':	c = '\r'; break;
						case 'f':	c = '\f'; break;
						case 'a':	c = '\a'; break;
						case '\\':	break;
						case '?':	break;
						case '\'':	break;
						case '"':	break;
						case 'x':
						case 'u':
							{
								int limit = (c == 'u') ? 4 : 100;

								c = *s++;
								if (!isxdigit((uint8)c))
									goto stop;

								uint32 v = 0;
								do {
									v = (v << 4) + kUnhexTab[c & 0x1f];

									c = *s++;
								} while(isxdigit((uint8)c) && --limit);

								--s;

								strvalue.push_back((wchar_t)v);
							}
							continue;

						default:
							goto stop;

					}
				}

				strvalue.push_back((wchar_t)(uint8)c);
			}

stop:
			key->setString(token.c_str(), strvalue.c_str());
		} else if (*s == '[') {
			binvalue.clear();

			++s;
			for(;;) {
				uint8 a = s[0];
				if (!isxdigit(a))
					break;

				uint8 b = s[1];
				if (!isxdigit(b))
					break;

				binvalue.push_back(kUnhexTab[b & 0x1f] + (kUnhexTab[a & 0x1f] << 4));

				if (s[2] != ' ')
					break;

				s += 3;
			}

			key->setBinary(token.c_str(), binvalue.data(), (int)binvalue.size());
		}
	}
}

void ATUISaveRegistryPath(VDTextOutputStream& os, VDStringA& path, bool global) {
	VDRegistryKey key(path.c_str(), global, false);
	if (!key.isReady())
		return;

	size_t baseLen = path.size();

	VDRegistryValueIterator valIt(key);
	VDStringW strval;
	vdfastvector<uint8> binval;

	bool wroteGroup = false;
	while(const char *name = valIt.Next()) {
		if (!wroteGroup) {
			wroteGroup = true;

			os.PutLine();
			os.FormatLine("[%s%s]", global ? "Shared" : "User", path.c_str());
		}

		VDRegistryKey::Type type = key.getValueType(name);
		switch(type) {
			case VDRegistryKey::kTypeInt:
				os.FormatLine("\"%s\" = %d", name, key.getInt(name));
				break;

			case VDRegistryKey::kTypeString:
				if (key.getString(name, strval)) {
					os.Format("\"%s\" = \"", name);

					bool lastWasHexEscape = false;
					for(VDStringW::const_iterator it(strval.begin()), itEnd(strval.end()); it != itEnd; ++it) {
						uint32 c = *it;

						if ((c >= 0x20 && c < 0x7f) && c != '"' && c != '\\') {
							if (!lastWasHexEscape || !isxdigit(c)) {
								lastWasHexEscape = false;
								char c8 = (char)c;
								os.Write(&c8, 1);
								continue;
							}
						}

						lastWasHexEscape = false;

						switch(c) {
							case L'\n':	os.Write("\\n");	break;
							case L'\t':	os.Write("\\t");	break;
							case L'\v':	os.Write("\\v");	break;
							case L'\b':	os.Write("\\b");	break;
							case L'\r':	os.Write("\\r");	break;
							case L'\f':	os.Write("\\f");	break;
							case L'\a':	os.Write("\\a");	break;
							case L'\"':	os.Write("\\\"");	break;
							case L'\\':	os.Write("\\\\");		break;
							default:
								lastWasHexEscape = true;
								os.Format("\\u%04X", c);
								break;
						}
					}

					os.PutLine("\"");
				}
				break;

			case VDRegistryKey::kTypeBinary:
				{
					int len = key.getBinaryLength(name);

					if (len >= 0) {
						binval.resize(len);

						if (key.getBinary(name, (char *)binval.data(), len)) {
							os.Format("\"%s\" = ", name);
							for(int i=0; i<len; ++i)
								os.Format("%c%02X", i ? ' ' : '[', (uint8)binval[i]);
							os.PutLine("]");
						}
					}
				}
				break;
		}
	}

	VDRegistryKeyIterator keyIt(key);
	while(const char *name = keyIt.Next()) {
		path += '\\';
		path.append(name);

		ATUISaveRegistryPath(os, path, global);

		path.resize((uint32)baseLen);
	}
}

void ATUISaveRegistry(const wchar_t *fnpath) {
	VDFileStream fs(fnpath, nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways);
	VDTextOutputStream os(&fs);

	os.PutLine("; Altirra settings file. EDIT AT YOUR OWN RISK.");

	VDStringA path;
	ATUISaveRegistryPath(os, path, false);
	ATUISaveRegistryPath(os, path, true);
}

void ATUIMigrateSettingsToPortable() {
	VDRegistryProviderMemory pvmem;
	VDRegistryCopy(pvmem, VDRegistryAppKey::getDefaultKey(), *VDGetRegistryProvider(), VDRegistryAppKey::getDefaultKey());

	// we are single threaded by this point, so this is safe(-ish)
	IVDRegistryProvider *pvprev = VDGetRegistryProvider();
	VDSetRegistryProvider(&pvmem);

	ATUISaveRegistry(ATSettingsGetDefaultPortablePath().c_str());

	VDSetRegistryProvider(pvprev);
}

void ATUIMigrateSettingsToRegistry() {
	IVDRegistryProvider *pvdst = VDGetDefaultRegistryProvider();
	const char *dstPath = VDRegistryAppKey::getDefaultKey();

	// we're about to blow away an entire subkey in the Registry, so put in
	// a safeguard against accidentally doing the equivalent of rm -rf /
	// in case the default path didn't get initialized
	if (!dstPath || !*dstPath)
		return;

	pvdst->RemoveKeyRecursive(pvdst->GetUserKey(), dstPath);

	VDRegistryCopy(*pvdst, dstPath, *VDGetRegistryProvider(), dstPath);
}

void ATUIMigrateSettings() {
	if (!ATSettingsIsMigrationScheduled())
		return;

	if (ATSettingsIsInPortableMode())
		ATUIMigrateSettingsToRegistry();
	else
		ATUIMigrateSettingsToPortable();
}
