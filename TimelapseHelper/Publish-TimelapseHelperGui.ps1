$ErrorActionPreference = "Stop"

$PublishVersion = get-date -f yyyy-MM-dd-HH-mm

$GitCommit = & git rev-parse --short HEAD

dotnet clean .\TimelapseHelper.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

dotnet restore .\TimelapseHelper.sln -r win-x64 -verbosity:minimal

$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

$msBuild = & $vsWhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

& $msBuild .\TimelapseHelper.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

$publishPath = "M:\PointlessWaymarksPublications\PiSlicedDayPhotos.TimelapseHelperGui"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PiSlicedDayPhotos.TimelapseHelperGui\PiSlicedDayPhotos.TimelapseHelperGui.csproj -t:publish -p:PublishProfile=.\PiSlicedDayPhotos.TimelapseHelperGui\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

& 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' .\Publish-InnoSetupInstaller-TimelapseHelperGui.iss /DVersion=$PublishVersion /DGitCommit=$GitCommit

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


