### Run HelloDotNet
cd HelloDotNet
dotnet run

### Run TodoApi (with Swagger)
cd TodoApi
dotnet restore
dotnet run
# then open https://localhost:5001/swagger (or the URL shown in console)

### FOLDER STRUCTURE
tutorials/
├─ dotnet-8-essentials/
│  ├─ HelloDotNet/
│  │  └─ HelloDotNet.csproj
│  └─ TodoApi/
│     ├─ Program.cs
│     └─ TodoApi.csproj
└─ README.md
