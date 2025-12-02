# Moonglade.Email

The Azure Function used by my blog (https://edi.wang) to send notifications.

This Function sets HTML template and send email notifications to blog administrator or users.

Tools | Alternative
--- | ---
[.NET 10.0 SDK](http://dot.net) | N/A
[Visual Studio 2026](https://visualstudio.microsoft.com/) with Azure Development payload| [Visual Studio Code](https://code.visualstudio.com/)

## Send by Azure Communication Service

- Create a Storage Account
- Create an Azure Communication Service resource with Email service enabled.
- Deploy `Moonglade.Function.Email` to Azure Function App.
- Set Environment Variables like this:

```
"MOONGLADE_EMAIL_STORAGE": "<storage account connection string>"
"MOONGLADE_EMAIL_SENDER_NAME": "Moonglade Notification Azure Function"
"MOONGLADE_EMAIL_PROVIDER": "AzureCommunication"
"MOONGLADE_EMAIL_ACS_CONN": "<your connection string>"
"MOONGLADE_EMAIL_ACS_ADDR": "<your sender address>"
```

## Send by SMTP

> Please note, only basic authentication is supported in this mode. Microsoft 365 has disabled basic authentication, so this won't work with your Microsoft 365 enterprise or personal account.

- Create a Storage Account
- Deploy `Moonglade.Function.Email` to Azure Function App.
- Set Environment Variables like this:

```
"MOONGLADE_EMAIL_STORAGE": "<storage account connection string>",
"MOONGLADE_EMAIL_SENDER_NAME": "Moonglade Notification Azure Function"
"MOONGLADE_EMAIL_PROVIDER": "smtp"
"MOONGLADE_EMAIL_SSL": true
"MOONGLADE_EMAIL_SMTP_PORT": 25
"MOONGLADE_EMAIL_SMTP_SERVER": "smtp.example.com"
"MOONGLADE_EMAIL_SMTP_USER": "admin@example.com"
"MOONGLADE_EMAIL_SMTP_PASS": "<smtp password>"
```

## Configure Moonglade

Open **appsettings.json** under your Moonglade instance, add the following settings:

```json
"Email": {
  "ApiEndpoint": "https://yourfunctionappurl",
  "ApiKey": "<your function key>",
  "ApiKeyHeader": "x-functions-key"
}
```

## Local Development and Debugging

For development, create ```local.settings.json``` under "**./src/Moonglade.Function.Email**", this file defines development time settings. It is by default ignored by git, so you will need to manange it on your own.

Sample ```local.settings.json``` file (SMTP)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "MOONGLADE_EMAIL_STORAGE": "<storage account connection string>",
    "MOONGLADE_EMAIL_SENDER_NAME": "Moonglade Notification Azure Function (local)",
    "MOONGLADE_EMAIL_PROVIDER": "smtp",
    "MOONGLADE_EMAIL_SSL": true,
    "MOONGLADE_EMAIL_SMTP_PORT": 25,
    "MOONGLADE_EMAIL_SMTP_SERVER": "smtp.example.com",
    "MOONGLADE_EMAIL_SMTP_USER": "admin@example.com",
    "MOONGLADE_EMAIL_SMTP_PASS": "<smtp password>"
  }
}
```

Sample ```local.settings.json``` file (Azure Communication)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "MOONGLADE_EMAIL_STORAGE": "<storage account connection string>",
    "MOONGLADE_EMAIL_SENDER_NAME": "Moonglade Notification Azure Function (local)",
    "MOONGLADE_EMAIL_PROVIDER": "AzureCommunication",
    "MOONGLADE_EMAIL_ACS_CONN": "<your connection string>",
    "MOONGLADE_EMAIL_ACS_ADDR": "<your sender address>"
  }
}
```
