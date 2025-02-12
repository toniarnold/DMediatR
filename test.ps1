<#
Runs all [Category("DebugServer")] tests which expect a running DMediatRNode
in the VS debugger.
If no left-hand side is given in the test selection, "test" is prepended.
#>
if ($args.Count -eq 0)
{
    echo "Usage: ./test [test selection]"
}
$selection = [string]::Join(" ", $args)
if (!($selection -match "^\s*[a-zA-Z]+"))
{
    $selection = "test " + $selection;
}
dotnet test --no-build test/DMediatR.Tests/DMediatR.Tests.csproj -- NUnit.Where="$selection" 
