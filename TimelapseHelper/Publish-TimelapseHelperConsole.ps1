$ErrorActionPreference = "Stop"

$PublishVersion = get-date -f yyyy-MM-dd-HH-mm

$GitCommit = & git rev-parse --short HEAD

dotnet clean .\TimelapseHelper.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

dotnet restore .\TimelapseHelper.sln -r win-x64 -verbosity:minimal

$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

$msBuild = & $vsWhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

& $msBuild .\TimelapseHelper.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

$publishPath = "M:\PiSlicedDayPhotos\PiSlicedDayPhotos.TimelapseHelperConsole"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PiSlicedDayPhotos.TimelapseHelper\PiSlicedDayPhotos.TimelapseHelper.csproj -t:publish -p:PublishProfile=.\PiSlicedDayPhotos.TimelapseHelper\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }
