param(
	[Parameter(Position = 0)]
	[string] $Path = '../../../dll/dsda.wbx.zst',
	[string[]] $SourceFilter = 'ld-temp.o',
	[regex] $NameFilter = '[^A-Za-z0-9_.]|^__|^_Unwind|^_GLOBAL__',
	[switch] $ListAll
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$Temp = New-TemporaryFile
try {
	zstd --decompress --quiet --force -o $Temp $Path
	if (-not $?) { throw 'Decompression failed' }
	$Lines = readelf --syms --demangle --wide $Temp
	if (-not $?) { throw 'Reading symbols failed' }
}
finally {
	Remove-Item $Temp
}

if ($ListAll) {
	return $Lines
}

$AddressMask = [UInt64]0xFFFFFFFFL
$Symbols = New-Object System.Collections.Hashtable
$CurrentSource = $null

foreach ($line in $lines) {
	if ($line -match '^\s*\d+:\s+(?<Addr>[0-9A-Fa-f]+)\s+(?<Size>[0-9a-fx]+)\s+(?<Type>\w+)\s+(?<Bind>\w+)\s+(?<Vis>\w+)\s+(?<Ndx>\w+)\s+(?<Name>\S.+)$') {
		$Symbol = $Matches
		$Addr = [UInt64]::Parse($Symbol.Addr, 'HexNumber')
		if ($Symbol.Type -ieq 'FILE') {
			$CurrentSource = $Symbol.Name
		}
		elseif ($Addr -ne 0 -and $Symbol.Type -ne 'NOTYPE' -and ($NameFilter -eq $null -or $Symbol.Name -notmatch $NameFilter) -and ($SourceFilter -eq $null -or $SourceFilter.Contains($CurrentSource))) {
			try {
				$Symbols.Add($Symbol.Name, $Addr -band $AddressMask)
			}
			catch [ArgumentException] {
				Write-Warning "Duplicate symbol ""$($Symbol.Name)"""
			}
		}
	}
}

$Reserved = [System.Collections.Generic.HashSet[String]] @(
	"and",
	"break",
	"do",
	"else",
	"elseif",
	"end",
	"false",
	"for",
	"function",
	"goto",
	"if",
	"in",
	"local",
	"nil",
	"not",
	"or",
	"repeat",
	"return",
	"then",
	"true",
	"until",
	"while"
)

'return {'
foreach ($Symbol in $Symbols.GetEnumerator() | Sort-Object Key) {
	if ($Symbol.Key -imatch '^[a-z_][a-z0-9_]*$' -and -not $Reserved.Contains($Symbol.Key)) {
		'	{0} = 0x{1:X8},' -f $Symbol.Key, $Symbol.Value
	}
	else {
		'	["{0}"] = 0x{1:X8},' -f $Symbol.Key, $Symbol.Value
	}
}
'}'
