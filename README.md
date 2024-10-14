# Moonglade.Email

The Azure Function used by my blog (https://edi.wang) to send notifications.

This Function sets HTML template and send email notifications to blog administrator or users.

Tools | Alternative
--- | ---
[.NET 8.0 SDK](http://dot.net) | N/A
[Visual Studio 2022](https://visualstudio.microsoft.com/) with Azure Development payload| [Visual Studio Code](https://code.visualstudio.com/)
[Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) | N/A
[Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest) | N/A

You can choose to send email by SMTP or Azure Communication Service. 

## Send by Azure Communication Service

// TODO

## Send by SMTP

> Please note, only basic authentication is supported in this mode. Microsoft 365 has disabled basic authentication, so this won't work with your Microsoft 365 enterprise or personal account.

### 1. Setup Azure Key Vault and Azure Function App

- Setup Azure CLI and login to your Azure subscription first. 
- Run **"\Azure-Deployment\Deploy.ps1"** to setup the Azure Function App and Azure Key Vault.

Parameters example:

```powershell
-regionName "westus"
-rsgName "moonglade-test-group"
-storageAccountName "moongladeteststorage"
-emailDisplayName "Moonglade Notification Test"
-smtpServer "smtp.example.com"
-smtpUserName "admin@example.com"
-pwdValue "P@ssw0rd"
```

- Build and deploy **Moonglade.Notification.sln** to the Azure Function.

## Configure Moonglade

Open **appsettings.json** under your Moonglade instance, add the following settings:

```json
"Email": {
  "ApiEndpoint": "https://yourfunctionappurl",
  "ApiKey": "<your function key>"
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
    "moongladestorage": "<storage account connection string>",
    "EmailDisplayName": "Moonglade Notification Azure Function (local)",
    "Sender": "smtp",
    "EnableSsl": true,
    "SmtpServerPort": 25,
    "SmtpServer": "smtp.example.com",
    "SmtpUserName": "admin@example.com",
    "EmailAccountPassword": "<smtp password>"
  }
}
```

## 免责申明

对于中国访客，我们有一份特定的免责申明。请确保你已经阅读并理解其内容：

- [免责申明（仅限中国访客）](./DISCLAIMER_CN.md)
