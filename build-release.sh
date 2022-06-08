# do not forget to make file executable: chmod +x build-release.sh 

dotnet publish "./src/ConsoleHost/ConsoleHost.csproj" -c Release --self-contained -r win-x64 -o "publish/SoftWell.RtFix.ConsoleHost-winx64-self-contained"

dotnet publish "./src/ConsoleHost/ConsoleHost.csproj" -c Release -o "publish/SoftWell.RtFix.ConsoleHost"