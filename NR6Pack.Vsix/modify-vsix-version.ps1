# Automatically update the version in VSIX manifest
if ($args.Length -lt 1)
{
	exit
}

$debugMode = ($args[0] -eq "Debug")
$workingDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$templateFile = (join-path $workingDir "source.extension.template.vsixmanifest")
$targetFile = (join-path $workingDir "source.extension.vsixmanifest")

$version = 0
if ($debugMode)
{
	# Replace version placeholder with random generated version
	$random = new-object System.Random
	$version = $random.Next(65534) + 1;
}

Get-Content $templateFile | ForEach-Object { $_.Replace('%%version%%', $version) } | Set-Content $targetFile
