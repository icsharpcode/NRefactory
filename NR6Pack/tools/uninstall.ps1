param($installPath, $toolsPath, $package, $project)

$analyzerPath = join-path $toolsPath "analyzers"
$analyzerFilePath = join-path $analyzerPath "NR6Pack.dll"

$project.Object.AnalyzerReferences.Remove("$analyzerFilePath")