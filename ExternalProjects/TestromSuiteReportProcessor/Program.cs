using System.Xml;

using FailingCase = (string DispNameWithTestClass, string Stdout);

if (args.Length < 1) {
	Console.WriteLine("invoke as `dotnet run -- /path/to/proj.coverage.xml`");
	return;
}
FileInfo fi = new(args[0]);
if (!fi.Exists) throw new Exception("no such file");
XmlDocument doc = new();
doc.Load(fi.Open(FileMode.Open));
if (doc.DocumentElement?.Name is not "testsuites") throw new Exception("not a dotnet test results file");
var suite = doc.DocumentElement.FirstChild as XmlElement;
if (!(suite?.GetAttribute("name") ?? string.Empty).StartsWith("BizHawk.Tests.Testroms.", StringComparison.Ordinal)) throw new Exception("not for testroms project");

Dictionary<string, List<FailingCase>> failuresByMessage = new();
List<FailingCase> GetListFor(string message)
	=> failuresByMessage.TryGetValue(message, out var l) ? l : (failuresByMessage[message] = new());
static string NaiveSubstringAfter(string needle, string haystack)
	=> haystack.Substring(haystack.IndexOf(needle, StringComparison.Ordinal) + needle.Length);
foreach (var childNode in suite!) {
	if (childNode is not XmlElement @case) continue;
	if (@case.HasChildNodes && @case.Name is "testcase" && @case.FirstChild is XmlElement { Name: "failure"} fail) {
		GetListFor(fail.GetAttribute("message")).Add((
			@case.GetAttribute("name").Replace("&quot;", "\""),
			NaiveSubstringAfter(needle: "Standard Output:", haystack: @case.InnerText)
		));
	}
}

foreach (var (message, failures) in failuresByMessage) {
	if (message.Contains("screenshot contains correct value unexpectedly", StringComparison.Ordinal)) {
		Console.WriteLine("drop these from known-bad:");
		foreach (var @case in failures) Console.WriteLine($"\t{@case.DispNameWithTestClass}");
	} else {
		Console.WriteLine(message);
		foreach (var (caseDisplayName, stdout) in failures) {
			Console.WriteLine($"\t{caseDisplayName}");
			foreach (var line in stdout.Split('\n')) {
				if (!string.IsNullOrWhiteSpace(line) && !line.Contains("not in DB", StringComparison.Ordinal)) {
					Console.WriteLine($"\t\t{line.Trim()}");
				}
			}
		}
	}
}
