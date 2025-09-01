## Edgamat.Messaging — Copilot / AI agent instructions

Purpose (one line)
-------------------
Small .NET library that exposes a message-consumer contract intended to be wired into a host (e.g., Azure Service Bus) by consumer registration and a transport integration in a separate host project.

Quick facts (what to read first)
--------------------------------
- `src/Edgamat.Messaging/IConsumer.cs` — primary public API: `Task ConsumeAsync(TMessage message, CancellationToken token)`
- `src/Edgamat.Messaging/Edgamat.Messaging.csproj` — targets `net9.0`; references `Azure.Messaging.ServiceBus` and `System.Text.Json`.
- `README.md` (repo root) — design intent and sample registration snippet.

Developer workflows (concrete)
-----------------------------
- Build the solution from repo root:

```powershell
dotnet restore "p:\source\Edgamat.Messaging\Edgamat.Messaging.sln"
dotnet build "p:\source\Edgamat.Messaging\Edgamat.Messaging.sln" -c Debug
```

- There are no tests in the repo yet. Add a `tests/Edgamat.Messaging.Tests` project and run `dotnet test` when present.
- Use the Shouldly and Moq NuGet packages in support of the unit tests.
- This is a class library — to exercise behaviour locally, create a `samples/host` worker/console project that references the library and runs a Service Bus processor wired to `IConsumer<T>` implementations.

Project-specific patterns and conventions
--------------------------------------
- Minimal public surface: focus on a single consumer interface. Keep additions small and well-justified.
- Nullable reference types enabled in the project; avoid nullability warnings.
- Implicit usings are enabled; explicit usings allowed for clarity but not required.
- Messages are JSON serialized (readme indicates JSON). Prefer `System.Text.Json` and keep serialization options compatible with host projects.

Integration points & expected host responsibilities
--------------------------------------------------
- Transport code (creating clients/processors, retry/backoff, dead-letter handling) is expected in the host. This library supplies the contract and small helpers only.
- Hosts will likely use `Azure.Messaging.ServiceBus` to receive messages, deserialize to the message type, then call `IConsumer<T>.ConsumeAsync`.

Concrete examples to copy from here
----------------------------------
- Consumer contract (see `IConsumer.cs`):

	`Task ConsumeAsync(TMessage message, CancellationToken token)`

- Registration (example from README):

```csharp
services.AddAzureServiceBus()
		.WithConfiguration(services.Configuration)
		.AddConsumer<MyMessageConsumer>("queueOrTopicName")
		.Build();
```

AI agent task list (what you can implement next)
----------------------------------------------
- Implement a typed consumer class under `src/` that implements `IConsumer<T>`.
- Add `samples/host/` with a small worker that demonstrates Service Bus wiring and shows a local run path.
- Add unit tests in `tests/Edgamat.Messaging.Tests` that cover consumer behaviour and serialization.

Notes / gotchas
----------------
- Repo is a library; do not add executables to `src/Edgamat.Messaging` — new runnable examples should go under `samples/` or a separate `tools/` folder and be added to the solution.
- Preserve `ImplicitUsings` and `Nullable` settings in the project file unless changing compatibility intentionally.

If any part of this needs more detail (sample host, multi-targeting, testing scaffold), tell me which item to add and I'll implement it and update this file.

