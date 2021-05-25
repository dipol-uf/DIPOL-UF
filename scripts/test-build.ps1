param([string]$path)


MSBuild.exe "$path\Host\Host.csproj" -t:restore -verbosity:minimal
MSBuild.exe "$path\Host\Host.csproj" -p:Configuration=Release -p:Platform=x86 -verbosity:minimal
MSBuild.exe "$path\Host\Host.csproj" -p:Configuration=Release -p:Platform=x64 -verbosity:minimal

MSBuild.exe "$path\Dipol-UF\Dipol-UF.csproj" -t:restore -verbosity:minimal
MSBuild.exe "$path\Dipol-UF\Dipol-UF.csproj" -p:Configuration=Release -p:Platform=x86 -verbosity:minimal
MSBuild.exe "$path\Dipol-UF\Dipol-UF.csproj" -p:Configuration=Release -p:Platform=x64 -verbosity:minimal

