echo off
echo "Stopping Service..."
sc stop McMapperService
echo "Done"
echo "Deleting Service..."
sc delete McMapperService
echo "Done"