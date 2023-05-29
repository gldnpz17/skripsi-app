Param([Parameter(Mandatory)][String]$TargetPath)

$TargetDir = Split-Path -Path $TargetPath
$WorkingDir = $pwd

Write-Output "Building front end"
cd ./../../../frontend
npm run build

Write-Output "Target directory : $TargetDir/frontend"
cp ./build -Destination $TargetDir/frontend -Recurse

Write-Output "Moving back to original working directory : $WorkingDir"
cd $WorkingDir

Write-Output "Front end build script completed."