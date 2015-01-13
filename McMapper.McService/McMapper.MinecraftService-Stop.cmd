echo off
echo "Stopping Service..."
sc stop McRuntimeService
echo "Done"
echo "Deleting Service..."
sc delete McRuntimeService
echo "Done"