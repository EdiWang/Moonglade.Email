# Copilot Instructions for Moonglade.Email

## Project Context

Moonglade.Email is a .NET 10 Azure Functions isolated worker app that provides asynchronous email notifications for the Moonglade blogging platform.

The main function app lives in `src/Moonglade.Function.Email`. Unit tests live in `src/Moonglade.Function.Email.Tests`. `src/TestClient` is only for manual/local testing.

Primary runtime flow:

1. `Enqueue` receives HTTP POST requests, validates the request contract, and writes an `EmailNotification` to Azure Queue Storage.
2. `QueueProcessor` reads queue messages, validates the persisted contract again, builds template-based messages, sends one email per recipient, and decides whether failures should retry.
3. `EmailDispatcher` routes messages to provider senders registered through `IEmailProviderSender`.
4. `SmtpEmailSender` sends through MailKit/Edi.TemplateEmail SMTP support.
5. `AzureCommunicationSender` sends through Azure Communication Services and logs the operation ID.

## Technology and Dependencies

- Target framework: `net10.0`.
- Azure Functions runtime: v4 isolated worker.
- Queue transport: Azure Storage Queue, queue name `moongladeemailqueue`.
- Email templates: `Edi.TemplateEmail`, configured by `mailConfiguration.xml`.
- Email providers: SMTP and Azure Communication Services.
- Tests: xUnit v3 and Moq.
- Main project has implicit usings enabled. The test project has nullable enabled; the main function app currently does not.

## Build and Test Commands

Use these commands from the repository root unless a task specifically requires another directory:

```powershell
dotnet build .\src\Moonglade.Email.slnx
dotnet test .\src\Moonglade.Function.Email.Tests\Moonglade.Function.Email.Tests.csproj
dotnet publish .\src\Moonglade.Function.Email\Moonglade.Function.Email.csproj
```

For local function execution, use Azure Functions Core Tools from `src/Moonglade.Function.Email`:

```powershell
func start
```

Do not rely on `.vscode/tasks.json` paths without checking them first; they may still reference old `Moonglade.Notification.API` project names.

## Coding Conventions

- Keep changes small and focused on the function app or tests relevant to the request.
- Prefer existing abstractions over new ones: `IEmailNotificationQueue`, `IEmailDispatcher`, `IEmailProviderSender`, and `IAzureCommunicationEmailClient` exist to keep functions and providers testable.
- Use constructor injection for Azure Functions classes and services.
- Keep provider-specific behavior behind `IEmailProviderSender`; avoid adding provider conditionals to function entry points.
- Keep supported message type names centralized in `MessageTypes` and contract validation centralized in `EmailNotificationContract`.
- Use `MoongladeJsonSerializerOptions.Default` for queue/payload JSON unless intentionally changing serialization behavior across the app.
- Preserve case-insensitive provider handling through `EmailServiceOptions.NormalizedProvider`.
- Avoid broad refactors, central package management, or nullable enablement in the main project unless the user asks for that explicitly.
- Add comments only for non-obvious behavior, especially retry decisions or provider SDK behavior.

## Contracts and Validation

- Validate all inbound HTTP request data before enqueueing.
- Validate queue messages again before sending. Queue messages can be malformed, stale, or produced by older clients.
- Use `EmailNotificationContract.ValidateMessageType`, `ValidateRecipients`, `ValidateNotification`, and `ValidatePayload` rather than duplicating contract rules.
- Keep recipient limits and email-address validation in `EmailNotificationContract`.
- When adding a message type, update all related places together: `MessageTypes`, payload model, `EmailNotificationContract.ValidatePayload`, `MessageBuilder`, `QueueProcessor.GetMessage`, `mailConfiguration.xml`, README/API docs, and tests.
- Treat invalid JSON, unsupported message types, invalid recipients, and invalid typed payloads as non-retryable queue messages unless requirements change.

## Retry and Failure Handling

- `QueueProcessor` intentionally sends one email per recipient so one permanent recipient failure does not block others.
- Use `EmailDeliveryFailureClassifier` to distinguish permanent and transient provider failures.
- Preserve current queue retry semantics:
  - partial recipient failure does not throw;
  - all recipients fail with at least one transient failure throws to let Azure Functions retry;
  - all recipients fail permanently logs and does not retry;
  - unknown/unclassified exceptions bubble for retry visibility.
- Do not log full payloads, message bodies, connection strings, passwords, function keys, or other secrets.

## Configuration

Configuration is environment-variable driven. Important names include:

- `MOONGLADE_EMAIL_STORAGE`
- `MOONGLADE_EMAIL_PROVIDER`
- `MOONGLADE_EMAIL_SENDER_NAME`
- SMTP: `MOONGLADE_EMAIL_SMTP_SERVER`, `MOONGLADE_EMAIL_SMTP_USER`, `MOONGLADE_EMAIL_SMTP_PASS`, `MOONGLADE_EMAIL_SMTP_PORT`, `MOONGLADE_EMAIL_SSL`
- Azure Communication Services: `MOONGLADE_EMAIL_ACS_CONN`, `MOONGLADE_EMAIL_ACS_ADDR`

Use `EmailServiceOptionsValidator` and `EmailQueueOptionsValidator` for startup validation. Do not introduce late runtime configuration failures when validation can catch them at startup.

`local.settings.json` is ignored by Git and must stay untracked. Never add real connection strings, function keys, SMTP passwords, or ACS secrets to tracked files, tests, logs, or documentation.

## Testing Guidance

- Add or update focused unit tests for behavior changes.
- Prefer mocks and adapter interfaces over real network calls.
- Use Moq verification for dispatcher/sender interactions.
- Use `Record.ExceptionAsync` or `Assert.ThrowsAsync<T>` to document retry vs non-retry behavior.
- For queue processor tests, create `QueueMessage` instances with `QueuesModelFactory.QueueMessage`.
- Keep tests deterministic; do not require live Azure Storage, SMTP, ACS, Azurite, or local secrets.
- Run the full test project after changes that affect contracts, providers, queue processing, options validation, or message building.

## Documentation and CI

- Update README examples when public API contracts, message types, environment variables, or local development commands change.
- Check GitHub Actions before changing build/deploy assumptions. Existing workflows target .NET 10, Azure Functions deployment, and Docker image builds.
- Keep Docker and workflow naming consistent with the current `Moonglade.Email`/`Moonglade.Function.Email` project unless intentionally fixing legacy names.