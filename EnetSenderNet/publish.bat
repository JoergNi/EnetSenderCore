dotnet publish EnetSenderNet.csproj -r linux-arm -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/linux-arm
scp publish/linux-arm/EnetSenderNet pi@vipi:/home/pi/EnetSenderNet
ssh pi@vipi "chmod +x /home/pi/EnetSenderNet"
