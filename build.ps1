$ErrorActionPreference = "Stop"

$rid = "win-x64"

if (-not (Test-Path local)) {
    new-item local -ItemType Directory -Force
}
dotnet restore

dotnet publish -c Release -r $rid --self-contained

$archive_files = @()
$archive_files = @(".\NugetVendor\bin\Release\net6.0\$rid\native\NugetVendor.exe")

Compress-Archive -Path $archive_files -DestinationPath ".\local\NugetVendor.$rid.zip" -Force -Verbose