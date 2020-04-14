@echo off
dotnet publish -r win-x64 -c Release --self-contained -o Release /p:PublishSingleFile=true /p:PublishTrimmed=true