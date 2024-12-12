# Debugging

The VS debugger can only be attached to one process at a time, so debugging a distributed system
presents some challenges. To make debugging easier, there are two scripts in the root directory:

Script | NUnit Category
: -- | : -
`.\debug-client.ps1` | `[Category("DebugClient")]`
`.\debug-server.ps1` | `[Category("DebugServer")]`

As debugging tests are a kind of "scaffolding tests", they are not intended to be run 
regularly to verify the code, but only upon request. So it is recommended to group them 
by "Traits" in the VS Test-Explorer with the "Group By" button[^group] to be able to 
run them discretely.


## DebugClient

Category `DebugClient` tests expect a running `DMediatRNode` to talk to. They are similar to
those in the `Integration` category, but these are intended to be run automatically
and start the `DMediatRNode` by themselves with `SetUp.StartServer()`. But these instances
are automatically terminated by the `[OneTimeTearDown]` method.

The `.\debug-client.ps1` script starts the server separately and leaves it running until
Ctrl+C is pressed. While it is running, the DebugClient tests can be run in debug mode
from within VS.

## DebugServer

Category `DebugServer` tests have no equivalent in the tests run automatically and are intended to
debug the `DMediatRNode` server itself.

The `.\debug-server.ps1` executes the NUnit tests with `[Category("DebugServer")]`
from the shell. It expects the DMediatRNode to be running in VS debug mode and a matching
configuration. These tests can send messages to that instance to inspect
the reaction of the server within the debugger.


[^group]: [Group Tests in the Test Explorer](https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer?view=vs-2022#group-tests-in-the-test-list)
