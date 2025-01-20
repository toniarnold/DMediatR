# Misc

## Lock: async reentrant locking

DMEdiatR's certificate generation operates on a single external resource: the
`cert` folder on the filesystem. During an operation, concurring certificate
requests are blocked from access with a `SemaphoreSlim.WaitAsync()`.

As certificate requests of different kind are implemented recursively in
DMediatR, a recursive request hitting the lock would cause a deadlock. To avoid
that, the locking messages carry with them a `HasLocked` set of locked
`SemaphoreSlim` instances to avoid attempting to lock it twice..

The `ILock` extension encapsulates that logic. Its simplest form e.g. in the
`RootCertificateRequest` handler of the `RootCertificateProvider` acquires the
lock and releases it only if it was acquired before:

[!code-csharp[RootCertificateProvider.cs](../../src/DMediatR/RootCertificateProvider.cs?name=lock&highlight=1,6)]

But the `RenewRootCertificateNotification` causes the dependent certificates to
be renewed, too. This entails recursively publishing subsequent messages
through `MediatR`. The `HasLocked` property has to be carried along manually, as
in the `RenewRootCertificateNotification` handler:

[!code-csharp[RootCertificateProvider.cs](../../src/DMediatR/RootCertificateProvider.cs?name=notificationlock&highlight=7,13)]

Unexpected deadlocks are often caused by not handling down the `HasLocked`
property.


## Remote certificate renewal firewall

There is an optional `RenewFirewallEnabled` setting in the `Certificate` section
of the DMediatR configuration which defaults to `True`. If the firewall is
explicitly disabled, any node on the network can publish two
RenewRootCertificateNotification messages in a row to bring down the entire
network, which may not be desirable. Chicago-style functional unit testing is one
use case for disabling the firewall. Another one might be, in a highly trusted
IoT scenario, some kind of intrusion detection on a remote node  that declares
the entire network to be potentially compromised and thus requiring manual
recertification.

It would have been security by obscurity to just hide the recertification
messages as internal: Any malicious client can construct a serialized
(supposedly) internal-only object which the target node will happily deserialize
and execute. This was the reason for not hiding these messages, but making them
public along with the aforementioned firewall.
