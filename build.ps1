# a dev file to build api ocally

# dotnet workload install wasm-tools
dotnet restore visiowebtools-wasm
# dotnet test tests --logger trx --results-directory "TestResults"
dotnet publish --configuration Release visiowebtools-wasm
# dotnet publish --configuration Release azure_function
Remove-Item ./public/AppBundle -Force -Recurse -ErrorAction SilentlyContinue
Copy-Item visiowebtools-wasm/bin/Release/net8.0/browser-wasm/AppBundle ./public -Recurse
