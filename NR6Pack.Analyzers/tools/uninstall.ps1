param($installPath, $toolsPath, $package, $project)

$analyzerPath = join-path $toolsPath "analyzers"
$analyzerFilePath = join-path $analyzerPath "NR6Pack.Analyzers.dll"

$project.Object.AnalyzerReferences.Remove("$analyzerFilePath")