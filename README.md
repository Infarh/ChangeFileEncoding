# ChangeFileEncoding
Программа по исправлению кодировки файлов

## Сборка

git clone https://github.com/Infarh/ChangeFileEncoding

cd ChangeFileEncoding

dotnet publish -r win-x64 -c Release --self-contained -o release /p:PublishSingleFile=true /p:PublishTrimmed=true
