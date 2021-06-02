param (
    [string]$PathToSrc,
    [string]$OutputDir = "./Releases",
    [string]$Arch = "x64",
    [string]$Configuration = "Release"
)

$csprojPath = "$PathToSrc/DIPOL-UF/DIPOL-UF.csproj"

# Restore and build project
MSBuild.exe $csprojPath -t:restore -verbosity:minimal
MSBuild.exe $csprojPath -p:Configuration=$Configuration -p:Platform=$Arch -verbosity:minimal

$regex = [System.Text.RegularExpressions.Regex]::new("^\s*\[assembly:\s*AssemblyVersion\s*\(\s*`"(\d+\.\d+.\d+)`"\s*\)\s*\]");

# Finds version string in the AssemblyInfo.cs
$version = Get-Content "$PathToSrc/DIPOL-UF/Properties/AssemblyInfo.cs" | `
    ForEach-Object {$regex.Matches($_)} | `
    Where-Object {$_.Success} | `
    ForEach-Object {$_.Groups[1].Value}

# Creates output directory if it is missing
$dir = "$OutputDir/DIPOL-UF_$($Configuration)_$($Arch)_v$version"
New-Item $dir -Force -ItemType Directory -ErrorAction SilentlyContinue

# Copies items one by one
Get-ChildItem "$PathToSrc/DIPOL-UF/bin/$Arch/$Configuration/" | `
    ForEach-Object {Copy-Item -Path $($_.FullName) -Destination "$dir\"}
