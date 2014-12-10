# Builds a NuGet package of NR6 Pack project.
#
# Command line:
# build-nuget-package.ps1 [<BuildType>]
# Example: build-nuget-package.ps1 Debug

# Unspecified <BuildType> assumes 'Release'.

$buildType = 'Release'
if (($args.Length -gt 0) -and -not [String]::IsNullOrEmpty($args[0]))
{
	$buildType = $args[0]
}

$projectDirectory = ($MyInvocation.MyCommand.Path | split-path -Parent | split-path -Parent)
$nuGetBinPath = (join-path $projectDirectory ../Packages/NuGet.CommandLine.2.8.2/tools)

pushd $nuGetBinPath
./NuGet.exe pack (join-path $projectDirectory NR6Pack.nuspec) -BasePath (join-path $projectDirectory bin/$buildType) -OutputDirectory (join-path $projectDirectory bin/$buildType)
popd
