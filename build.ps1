dotnet publish -c Release -r win-x64
Copy-Item binary-drops\ClrCompression.dll .\NugetVendor\bin\Release\netcoreapp2.0\win-x64\native


