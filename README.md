# McMapper
Minecraft server and mapping services for deployment on a windows environment. Includes a simple ASP.NET MVC map site.
The services rely on the availability of the Minecraft server, client and Chunky java packages (JAR files), and starts, manages and closes these JAR file processes.

Caveats - 
Chunky has a base texture pack which is not compatible with later versions of Minecraft. You have to install the Minecraft client and run Chunky at least once using the UI, and set it to use the Minecraft client installation so that it can read the official texture pack (or whatever resource pack you want it to use).

Project Settings - 
1. .nuget and associated packages not included - please Enable Nuget to download missing references.
2. Services contains multiple app.config files based on build configuration, hacked in the csproj file to transforms and build accordingly.

Project Components - 
1. McMapper.MinecraftService is a service hosting and managing the Minecraft server JAR process
2. McMapper.WinService is the mapping service that uses Chunky to render map tiles in .png format
3. McMapper.Console is a simple console application that can be used to start and test either service from the debugger
4. McMapper.Web is a simple ASP.NET MVC 5.0 site that uses the Google Map API to present the server's world using the rendered tiles.

Prerequisites - 
1. Running McMapper.MinecraftService requires the installation of a Minecraft server, correctly configured via server.properties
2. Running McMapper.WinService requires the installation of Chunky and Minecraft (for correct textures)
