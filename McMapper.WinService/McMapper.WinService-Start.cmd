echo off
echo "Installing Service..."
sc create McMapperService binPath= "C:\mcmapper\serviceroot\McMapper.WinService.exe"
echo "Done"
echo "Starting Service..."
sc start McMapperService
echo "Done"