//	ATHelpBuilder - Altirra .CHM help file preprocessor
//	Copyright (C) 2010 Avery Lee
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

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Xml;
using namespace System::Xml::Xsl;
using namespace System::IO;
using namespace System::Text::RegularExpressions;

ref class CommandLineParseException : Exception {
public:
	CommandLineParseException(String^ message)
		: Exception(message)
	{
	}
};

ref class BuildException : Exception {
public:
	BuildException(String^ message) : Exception(message) { }
};

ref class CachedTransform {
public:
	XslCompiledTransform^ mTransform;
	XmlDocument^ mDoc;
	XmlNodeList^ mPIs;
};

ref class EntityResolver : XmlResolver {
public:
	EntityResolver(array<unsigned char>^ data) : mData(data) {}

	property System::Net::ICredentials^ Credentials {
		virtual void set(System::Net::ICredentials ^) override {
		}
	}

	virtual System::Object ^GetEntity(System::Uri^ uri, System::String^ name,System::Type^ type) override {
		if (uri->IsFile) {
			if (Path::GetFileName(uri->LocalPath) == "data.xml") {
				return gcnew MemoryStream(mData);
			}
		}

		return nullptr;
	}

	array<unsigned char>^ mData;
};

ref class Program {
	String^ mHelpCompilerPath;
	String^ mSourcePath;
	String^ mOutputPath;
	Regex^ mXslInsnRegex;
	Regex^ mXslPIArgsRegex;
	List<String^>^ mOutputFiles;
	Dictionary<String^, bool>^ mReferencedLinks;
	Dictionary<String^, bool>^ mImages;
	Dictionary<String^, CachedTransform^>^ mXsltCache;

	XmlDocument^ mDataDoc;
	XmlElement^ mDataEl;

public:
	Program() {
		mOutputFiles = gcnew List<String^>();
		mImages = gcnew Dictionary<String^, bool>();
		mReferencedLinks = gcnew Dictionary<String^, bool>();
		mDataDoc = gcnew XmlDocument();
		mDataEl = mDataDoc->CreateElement("data");
		mDataDoc->AppendChild(mDataEl);

		mXsltCache = gcnew Dictionary<String^, CachedTransform^>();
	}

	void Run(cli::array<String^>^ args) {
		try {
			RunInner(args);
		} catch (CommandLineParseException^ ex) {
			System::Console::WriteLine("Usage Error: " + ex->Message);

			PrintUsage();

			System::Environment::Exit(5);
		} catch (BuildException^ ex) {
			System::Console::WriteLine("Error: " + ex->Message);
			System::Environment::Exit(10);
		} catch (Exception^ ex) {
			System::Console::WriteLine("Fatal Error: " + ex->ToString());

			System::Environment::Exit(20);
		}
	}

private:
	static void PrintUsage()
	{
		System::Console::WriteLine(); 
		System::Console::WriteLine("Usage:");
		System::Console::WriteLine("  athelpbuilder [hcpath] [sourcepath] [outputpath]");
	}

	void RunInner(cli::array<String^>^ args)
	{
		if (args->Length < 3)
			throw gcnew CommandLineParseException("No help compiler path was specified.");

		mHelpCompilerPath = args[0];

		if (!File::Exists(mHelpCompilerPath))
			throw gcnew BuildException("Help compiler does not exist: " + mHelpCompilerPath);

		mSourcePath = args[1];

		if (!Directory::Exists(mSourcePath))
			throw gcnew BuildException("Source path does not exist: " + mHelpCompilerPath);

		mOutputPath = args[2];

		if (!Directory::Exists(mOutputPath))
		{
			Directory::CreateDirectory(mOutputPath);
		}

		mXslInsnRegex = gcnew Regex("type=\"text/xsl\" href=\"(.*)\"");
		mXslPIArgsRegex = gcnew Regex("(?:^|[^a-zA-Z])([a-zA-Z][a-zA-Z0-9-]*)(?:\\s*=\\s*\"([^\"]+)\")?");

		for each(String^ file in Directory::GetFiles(mSourcePath, "*.xml")) {
			if (Path::GetFileName(file)->ToLowerInvariant() == "toc.xml")
				continue;

			ProcessFile(file);
		}

		// validate links
		for each(String^ s in mOutputFiles) {
			mReferencedLinks->Remove(s);
		}

		if (mReferencedLinks->Count > 0) {
			for each(String^ s in mReferencedLinks->Keys) {
				Console::WriteLine("Broken link: " + s);
			}

			throw gcnew BuildException("One or more broken links were found.");
		}

		ProcessToc();

		mOutputFiles->Add("layout.css");

		{
			StreamWriter sw(Path::Combine(mOutputPath, "athelp.hhp"));
			{
				StreamReader sr(Path::Combine(mSourcePath, "athelp.hhp"));
				for(;;) {
					String^ s = sr.ReadLine();
					if (s == nullptr)
						break;

					sw.WriteLine(s);
				}
			}

			sw.WriteLine("[FILES]");

			for each(String^ file in mOutputFiles) {
				sw.WriteLine(file);
			}
		}

		String^ cssFile = Path::Combine(mSourcePath, "layout.css");
		String^ cssFileOut = Path::Combine(mOutputPath, "layout.css");
		if (File::Exists(cssFileOut))
			File::SetAttributes(cssFileOut, FileAttributes::Normal);
		File::Copy(cssFile, cssFileOut, true);
		File::SetAttributes(cssFile, FileAttributes::Normal);

		CopyImages();

		// dump out the data doc
		mDataDoc->Save(Path::Combine(mOutputPath, "data.xml"));

		BuildHelpFile();
	}

	void ProcessToc()
	{
		XslCompiledTransform transform;
		transform.Load(Path::Combine(mSourcePath, "toc.xsl"));

		XmlDocument doc;

		StreamWriter writer(Path::Combine(mOutputPath, "athelp.hhc"), false, Encoding::UTF8);

		MemoryStream ms;
		{
			XmlWriter^ writer = XmlWriter::Create(%ms);
			mDataDoc->WriteTo(writer);
			writer->Flush();
		}

		StreamReader sr(Path::Combine(mSourcePath, "toc.xml"));
		XmlReaderSettings xrs;
		xrs.ProhibitDtd = false;
		xrs.XmlResolver = gcnew EntityResolver(ms.ToArray());
		XmlReader^ xr = XmlReader::Create(%sr, %xrs);
		doc.Load(xr);
		doc.Save(Path::Combine(mOutputPath, "toc.xml"));

		transform.Transform(%doc, nullptr, %writer);
	}

	void ProcessFile(String^ file)
	{
		XmlDocument doc;
		String^ filename = Path::GetFileNameWithoutExtension(file);

		try {
			doc.Load(file);
		} catch(System::Xml::XmlException^ e) {
			System::Console::WriteLine("Error processing {0}: {1}", file, e->Message);
			throw;
		}

		String^ xslname = nullptr;
		String^ xslPath = nullptr;
		String^ collectXPath = nullptr;
		XslCompiledTransform^ xslt = nullptr;
		bool split = false;

		Queue<XmlProcessingInstruction^> pis;
		System::Collections::IEnumerator^ localPIs = doc.SelectNodes("processing-instruction()")->GetEnumerator();

		for(;;) {
			XmlProcessingInstruction^ insn;

			if (pis.Count)
				insn = pis.Dequeue();
			else if (localPIs->MoveNext())
				insn = (XmlProcessingInstruction^)localPIs->Current;
			else
				break;

			if (insn->Name == "xml-stylesheet") {
				Match^ match = mXslInsnRegex->Match(insn->Data);

				if (match->Success) {
					xslname = match->Groups[1]->Captures[0]->Value;
					xslPath = Path::Combine(Path::GetDirectoryName(file), xslname);

					CachedTransform^ transform;

					String^ xsltKey = xslPath->ToLowerInvariant();
					if (!mXsltCache->TryGetValue(xsltKey, transform)) {
						transform = gcnew CachedTransform();

						transform->mDoc = gcnew XmlDocument();
						transform->mDoc->Load(xslPath);
						transform->mPIs = transform->mDoc->SelectNodes("processing-instruction()");

						transform->mTransform = gcnew XslCompiledTransform();
						transform->mTransform->Load(transform->mDoc);

						mXsltCache->Add(xsltKey, transform);
					}

					for each(XmlProcessingInstruction^ pi in transform->mPIs)
						pis.Enqueue(pi);

					xslt = transform->mTransform;
				}
			} else if (insn->Name == "ATHelpBuilder") {
				MatchCollection^ args = mXslPIArgsRegex->Matches(insn->Data);

				if (!args->Count) {
					throw gcnew BuildException("Malformed PI in "+file+": "+insn->Data);
				}

				for each(Match^ match in args) {
					String^ arg = match->Groups[1]->Captures[0]->Value;
					String^ value = match->Groups[2]->Captures->Count ? match->Groups[2]->Captures[0]->Value : "";

					if (arg == "split") {
						split = true;
					} else if (arg == "collect") {
						collectXPath = value;
					} else {
						throw gcnew BuildException("Unknown builder option \"" + arg + "\" in file: " + file);
					}
				}
			} else {
				throw gcnew BuildException("Unknown PI in "+file+": "+insn->Name);
			}
		}

		if (collectXPath) {
			Dictionary<XmlNode^, bool> nodeSet;

			for each(XmlNode^ node in doc.SelectNodes(collectXPath)) {
				for(XmlNode^ node2 = node; node2; node2 = node2->ParentNode)
					nodeSet[node2] = false;
			}

			CopyDataNodes(*mDataDoc, *mDataEl, *doc.ChildNodes, nodeSet);
		}

		if (xslname != nullptr) {
			String^ resultFileName = filename + ".html";
			String^ resultPath = Path::Combine(mOutputPath, resultFileName);

			System::Console::WriteLine("[xslt {0}] {1} -> {2}", xslPath, file, resultPath);

			// transform to XML and scan for IMG/split tags
			List<KeyValuePair<String^, String^>> splitunits;

			{
				XmlDocument resultDoc;

				{
					XmlWriter^ writer = resultDoc.CreateNavigator()->AppendChild();
					xslt->Transform(%doc, nullptr, writer);
					writer->Close();
				}

				for each(XmlAttribute^ srcAttr in resultDoc.SelectNodes("//img[@src]/@src")) {
					String^ path = srcAttr->InnerText;

					if (!path->StartsWith("http:"))
						mImages->Add(path, false);
				}

				for each(XmlAttribute^ href in resultDoc.SelectNodes("//a[@href]/@href")) {
					String^ ref = href->Value;
					if (!ref->StartsWith("http:") && !ref->StartsWith("#"))
						mReferencedLinks[ref] = true;
				}

				if (split) {
					for each(XmlElement^ header in resultDoc.SelectNodes("//div[@class='split-unit']/div[@class='split-header']")) {
						String^ splitname = header->SelectSingleNode("span[@class='split-name']")->InnerText;
						String^ splitkey = header->SelectSingleNode("span[@class='split-key']")->InnerText;

						splitunits.Add(KeyValuePair<String^, String^>(splitkey, splitname));
					}
				}
			}	

			// transform to HTML
			if (split) {
				for each(KeyValuePair<String^, String^>^ splitpair in splitunits) {
					String^ splitkey = splitpair->Key;
					String^ splitunit = splitpair->Value;

					Console::WriteLine("    Processing split unit: " + splitunit);

					XsltArgumentList xal;
					xal.AddParam("split-key", String::Empty, splitkey);
					xal.AddParam("split-unit", String::Empty, splitunit);

					String^ unitResultPath = Path::Combine(mOutputPath, splitunit);

					StreamWriter writer(unitResultPath);
					xslt->Transform(%doc, %xal, %writer);

					mOutputFiles->Add(splitunit);
				}
			} else {
				StreamWriter writer(resultPath);
				xslt->Transform(%doc, nullptr, %writer);

				mOutputFiles->Add(resultFileName);
			}
		}
	}

	void CopyImages() {
		List<String^> images(mImages->Keys);
		images.Sort();

		for each(String^ s in images) {
			System::Console::WriteLine("[copying image] {0}", s);

			String^ relPath = s->Replace('/', '\\');
			String^ relDir = Path::GetDirectoryName(relPath);

			Directory::CreateDirectory(Path::Combine(mOutputPath, relDir));

			String^ outPath = Path::Combine(mOutputPath, relPath);
			File::Copy(Path::Combine(mSourcePath, relPath), outPath, true);
			File::SetAttributes(outPath, FileAttributes::Normal);
		}
	}

	void BuildHelpFile() {
		System::Diagnostics::ProcessStartInfo startInfo;

		startInfo.FileName = mHelpCompilerPath;
		startInfo.Arguments = Path::Combine(mOutputPath, "athelp.hhp");
		startInfo.UseShellExecute = false;

		System::Diagnostics::Process^ p = System::Diagnostics::Process::Start(%startInfo);

		p->WaitForExit();

		if (p->ExitCode == 0) {
			System::Console::WriteLine();
			throw gcnew BuildException("HTML Help compilation failed.");
		}
	}

	void CopyDataNodes(XmlDocument% dstDoc, XmlElement% dst, XmlNodeList% srcNodes, Dictionary<XmlNode^, bool>% nodeSet) {
		for each(XmlNode^ node in srcNodes) {
			if (!nodeSet.ContainsKey(node))
				continue;

			switch(node->NodeType) {
				case XmlNodeType::Element:
					{
						XmlElement^ srcElement = (XmlElement^)node;
						String^ nodeName = node->Name;
						XmlElement^ dstNode = dstDoc.CreateElement(nodeName);
						dst.AppendChild(dstNode);

						for each(XmlAttribute^ attr in srcElement->Attributes) {
							XmlAttribute^ attr2 = dstDoc.CreateAttribute(attr->Name);
							attr2->Value = attr->Value;
							dstNode->Attributes->Append(attr2);
						}

						CopyDataNodes(dstDoc, *dstNode, *srcElement->ChildNodes, nodeSet);
					}
					break;

				case XmlNodeType::Text:
					{
						XmlText^ srcText = (XmlText^)node;
						XmlNode^ dstNode = dstDoc.CreateTextNode(srcText->Value);

						dst.AppendChild(dstNode);
					}
					break;

				case XmlNodeType::Whitespace:
					{
						XmlWhitespace^ srcWS = (XmlWhitespace^)node;
						XmlNode^ dstNode = dstDoc.CreateWhitespace(srcWS->Value);

						dst.AppendChild(dstNode);
					}
					break;
			}
		}
	}
};

int main(array<String^>^ args) {
	Program p;

	p.Run(args);
	return 0;
}
