# DMediatR

DMediatR gRPC server template with a `Request1` and a `Notification1` class to
be handled either locally in `Handler1`  or in `Handler1Remote` when configured
so.

There is an appsettings.Red.json and an appsettings.Blue.json for two server
instances. These can be started in the Developer PowerShell with `./start Red`
or `./start Blue`.

A `Request1` message can be sent to such a host e.g. with `./test test =~
PingRedTest.RequestResponseTest`, as described in
[Debugging](https://toniarnold.github.io/DMediatR/docs/debugging.html).
