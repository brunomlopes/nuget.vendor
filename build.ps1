$ErrorActionPreference = "Stop"

$rid = "win-x64"

if (-not (Test-Path local)) {
    new-item local -ItemType Directory -Force
}
dotnet restore

dotnet publish -c Release -r $rid

$archive_files = @()
if($rid -eq "win-x64"){
    $archive_files = @(".\NugetVendor\bin\Release\netcoreapp2.2\$rid\native\NugetVendor.exe",".\NugetVendor\bin\Release\netcoreapp2.2\$rid\publish\clrcompression.dll")
}else{
    $archive_files = @(".\NugetVendor\bin\Release\netcoreapp2.2\$rid\native\NugetVendor")
}

Compress-Archive -Path $archive_files -DestinationPath ".\local\NugetVendor.$rid.zip" -Force -Verbose