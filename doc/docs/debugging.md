﻿# Debugging

Since DMediatR is bootstrapped in the sense that it is implemented by passing
certificates around as MediatR `IResponse` objects, the tools used to debug it can
also be applied to projects that just use DMediatR as a library.

The VS debugger can only be attached to one process at a time, so debugging a
distributed system presents some challenges. To make debugging easier, there are
two scripts in the root directory:

Script | Arguments | Usage Example
: -- | : - | : -
`start.ps1` | `profile1 [profile2 ...]` | `./start Monolith`
`test.ps1` |  `[test selection]` | `./test =~ GetRemoteTemp`


## start.ps1

Tests of category `Integration` expect one or more running `DMediatRNode`
instances to talk to. They start these by themselves with `SetUp.StartServer()`. But
these instances are automatically terminated by the `[OneTimeTearDown]` method,
and its output disappears - at least unless there’s a breakpoint before its
`SetUp.StopAllServers();`.

The `./start.ps1` script starts nodes with the specified `--launch-profile`
separately and leaves them running until Ctrl+C is pressed. 
After commenting out `StartServer` in e.g. ServerTest.cs:

[!code-csharp[ServerTest.cs](../../test/DMediatR.Tests/Grpc/ServerTest.cs?name=startserver&highlight=1)]

and recompiling, the client tests can be debugged from within VS after running
`./start Monolith`.


## test.ps1

To debug a `DMediatRNode` instance itself in the VS debugger, the tests must be
run from another process. The `./test.ps1` script executes NUnit tests with the
specified `NUnit.Where` test selection[^selection]. If no left-hand side is
given in the selection, `test` is assumed and prepended. It is assumed that the
selected test executed by the script expects a `DMediatRNode` with a matching
configuration profile running in the VS debugger.

Running e.g. `./test =~ Test1` in the VS Developer-PowerShell eliminates the
need to attempt to teach tools like Postman to talk binary gRPC MediatR.

[^selection]: [NUnit Test Selection Language](https://docs.nunit.org/articles/nunit/running-tests/Test-Selection-Language.html)
