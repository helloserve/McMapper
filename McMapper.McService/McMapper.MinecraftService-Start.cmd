echo off
echo "Installing Service..."
sc create McRuntimeService binPath= "C:\mcmapper\runtimeroot\McMapper.McService.exe"
echo "Done"
echo "Starting Service..."
sc start McRuntimeService
echo "Done"