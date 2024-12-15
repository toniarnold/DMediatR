<#
Runs all [Category("DebugServer")] tests which expect a running DMediatRNode
in the VS debugger.
#>
if ($args.Count -eq 0)
{
    echo "Usage: ./test [test selection]"
}
$selection = [string]::Join(" ", $args)
dotnet test --no-build test/DMediatR.Tests/DMediatR.Tests.csproj -- NUnit.Where="$selection" 
