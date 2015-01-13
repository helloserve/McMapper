# McMapper
Minecraft server and mapping services for deployment on a windows environment. Includes a simple ASP.NET MVC map site.
The services rely on the availability of the Minecraft server, client and Chunky java packages (JAR files).

Project Settings
1. .nuget and associated packages not included - please Enable Nuget to downlaod missing references.
2. Services contains multiple app.config files based on build configuration, hacked in the csproj file to transforms and build accordingly.

Prerequisites
1. McMapper.MinecraftService requires the installation of a minecraft server, correctly configured via server.properties
2. McMapper.WinService rerquires the installation of Chunky and Minecraft (for correct textures)
