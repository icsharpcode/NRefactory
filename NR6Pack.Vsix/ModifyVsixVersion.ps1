# Automatically update the version in VSIX manifest
if ($args.Length -lt 2)
{
	exit
}

$debugMode = ($args[0] -eq "Debug")
$workingDir = $args[1]
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
