<#
Runs all [Category("DebugServer")] tests which expect a running DMediatRNode
in the VS debugger.
#>
cd test\DMediatR.Tests
dotnet test DMediatR.Tests.csproj --configuration=Debug --no-build --verbosity normal -- NUnit.Where="cat == DebugServer"
cd ..\..