# dotnet workload install wasm-tools --source https://api.nuget.org/v3/index.json
cd ../test/project
dotnet publish -c Release -r browser-wasm --self-contained
read -r -p "Press Enter key to exit..."
