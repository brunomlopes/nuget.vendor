$ErrorActionPreference = "Stop"

mkdir local -force

dotnet publish -c Release -r win-x64 /p:NativeCompilationDuringPublish=false 

Copy-Item .\NugetVendor\bin\Release\netcoreapp2.0\win-x64\publish\clrcompression.dll local\clrcompression.dll

dotnet publish -c Release -r win-x64

Copy-Item  local\clrcompression.dll .\NugetVendor\bin\Release\netcoreapp2.0\win-x64\native\clrcompression.dll
Compress-Archive -Path .\NugetVendor\bin\Release\netcoreapp2.0\win-x64\native\clrcompression.dll,.\NugetVendor\bin\Release\netcoreapp2.0\win-x64\native\NugetVendor.exe -DestinationPath .\local\NugetVendor.zip