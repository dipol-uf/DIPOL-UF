param (
    [string]$PathToSrc,
    [string]$OutputDir = "./Hosts",
    [string]$Arch = "x64"
)

$csprojPath = "$PathToSrc/Host/Host.csproj"

# Restore and build project
MSBuild.exe $csprojPath -t:restore -verbosity:minimal
MSBuild.exe $csprojPath -p:Configuration=Release -p:Platform=$Arch -verbosity:minimal

$regex = [System.Text.RegularExpressions.Regex]::new("^\s*\[assembly:\s*AssemblyVersion\s*\(\s*`"(\d+\.\d+.\d+)`"\s*\)\s*\]");

# Finds version string in the AssemblyInfo.cs
$version = Get-Content "$PathToSrc/Host/Properties/AssemblyInfo.cs" | `
    ForEach-Object {$regex.Matches($_)} | `
    Where-Object {$_.Success} | `
    ForEach-Object {$_.Groups[1].Value}

# Creates output directory if it is missing
$dir = "$OutputDir/Host $version"
New-Item $dir -Force -ItemType Directory -ErrorAction SilentlyContinue

# Copies items one by one
Get-ChildItem "$PathToSrc/Host/bin/$Arch/Release/" | `
    ForEach-Object {Copy-Item -Path $($_.FullName) -Destination "$dir\"}
