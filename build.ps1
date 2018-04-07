$ErrorActionPreference = "Stop"

$rid = "win-x64"

if (-not (Test-Path local)) {
    New-Item local -force
}

dotnet publish -c Release -r $rid /p:NativeCompilationDuringPublish=false 

if($rid -eq "win-x64"){
    Copy-Item .\NugetVendor\bin\Release\netcoreapp2.0\$rid\publish\clrcompression.dll local\clrcompression.dll
}

dotnet publish -c Release -r $rid

if($rid -eq "win-x64"){
    Copy-Item  local\clrcompression.dll .\NugetVendor\bin\Release\netcoreapp2.0\$rid\native\clrcompression.dll
}

$archive_files = @()
if($rid -eq "win-x64"){
    $archive_files = @(".\NugetVendor\bin\Release\netcoreapp2.0\$rid\native\NugetVendor.exe",".\NugetVendor\bin\Release\netcoreapp2.0\$rid\native\clrcompression.dll")
}else{
   $archive_files = @(".\NugetVendor\bin\Release\netcoreapp2.0\$rid\native\NugetVendor")
}

Compress-Archive -Path $archive_files -DestinationPath .\local\NugetVendor.$rid.zip -Force