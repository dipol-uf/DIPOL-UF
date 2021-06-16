param([string]$path)
if ((Get-ChildItem "$path/TestData" -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0) {
    New-Item -Type Directory -ErrorAction SilentlyContinue "$path/TestData"
    Invoke-WebRequest https://fits.gsfc.nasa.gov/samples/UITfuv2582gc.fits -OutFile "$path/TestData/UITfuv2582gc.fits"
    Invoke-WebRequest https://fits.gsfc.nasa.gov/nrao_data/tests/nost_headers/bitpix13.fits -OutFile "$path/TestData/unsupp_bitpix.fits"
}
dotnet test $path -c Release 