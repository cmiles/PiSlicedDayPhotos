$publishPath = "M:\PointlessWaymarksPublications\PiSlicedDayPhotos"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }
Remove-Item -Path $publishPath\* -Recurse

dotnet publish .\PiSlicedDayPhotos\PiSlicedDayPhotos.csproj /p:PublishProfile=.\PiSlicedDayPhotos\Properties\PublishProfiles\FolderProfile.pubxml

$publishVersion = (Get-Date).ToString("yyyy-MM-dd-HH-mm")
$destinationZipFile = "M:\PointlessWaymarksPublications\PiSlicedDayPhotos-Zip--{0}.zip" -f $publishVersion

Compress-Archive -Path M:\PointlessWaymarksPublications\PiSlicedDayPhotos -DestinationPath $destinationZipFile

Write-Output "PiSlicedDayPhotos zipped to '$destinationZipFile'"

if ($lastexitcode -ne 0) {throw ("Exec: " + $errorMessage) }