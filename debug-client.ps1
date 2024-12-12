 <#
The same as DMediatR.Tests.Grpc.StartServer() for debugging
[Category("DebugClient")] tests which expect a running DMediatRNode
 #>
 $profile = 'ClientCertifier'
 cd src\DMediatRNode
 dotnet run --no-build --project DMediatRNode.csproj --launch-profile $profile
 cd ..\..
