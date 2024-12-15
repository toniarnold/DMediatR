 <#
The same as DMediatR.Tests.Grpc.StartServer() for debugging
 #>
if ($args.Count -eq 0)
{
    echo "Usage: ./start-node profile1 [profile2 ...]"
}
foreach ($arg in $args)
{
    Start-Process "dotnet" -ArgumentList "run --no-build --project src/DMediatRNode/DMediatRNode.csproj --launch-profile $arg"
}