param (
    [string]$PathToSrc = "./src",
    [string]$OutputDir = "./publish",
    [string]$Arch = "x64",
    [string]$Configuration = "Release",
    [string]$BuildType = "All"
)

if($BuildType.Equals("all", [System.StringComparison]::OrdinalIgnoreCase) -or $BuildType.Equals("host", [System.StringComparison]::OrdinalIgnoreCase)) {
    $projects = @("Host")
}
if($BuildType.Equals("all", [System.StringComparison]::OrdinalIgnoreCase) -or $BuildType.StartsWith("dipol", [System.StringComparison]::OrdinalIgnoreCase)) {
    $projects += "Dipol-UF"
}


$regex = [System.Text.RegularExpressions.Regex]::new("^\s*\[assembly:\s*AssemblyVersion\s*\(\s*`"(\d+\.\d+.\d+)`"\s*\)\s*\]");

foreach($proj in $projects) {
    $csprojPath = "$PathToSrc/$proj/$proj.csproj"

    # Finds version string in the AssemblyInfo.cs
    $version = Get-Content "$PathToSrc/$proj/Properties/AssemblyInfo.cs" | `
        ForEach-Object {$regex.Matches($_)} | `
        Where-Object {$_.Success} | `
        ForEach-Object {$_.Groups[1].Value}

    # Creates output directory if it is missing
    $dir = "$OutputDir/$($proj)_$($Configuration)_$($Arch)_v$version"
    New-Item $dir -Force -ItemType Directory -ErrorAction SilentlyContinue

    if(![System.IO.Path]::IsPathRooted($dir)) {
        $dir = "../../$dir"
    }

    # Restore and build project
    MSBuild.exe $csprojPath -t:restore -verbosity:minimal
    MSBuild.exe $csprojPath -p:Configuration=$Configuration -p:Platform=$Arch -verbosity:minimal -p:OutputPath=$dir
}
