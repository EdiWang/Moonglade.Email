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

> This option is in preview and may subject to change.

- Setup Azure CLI and login to your Azure subscription first. 
- Create an Azure Communication Service resource with Email service enabled in Azure Portal.
- Run **"\Deployment\DeployACS.ps1"** to setup the Azure Function App.

Parameters example:

```powershell
-regionName "westus"
-rsgName "moonglade-test-group"
-storageAccountName "moongladeteststorage"
-emailDisplayName "Moonglade Notification Test"
-azureCommunicationConnection "<your connection string>"
-azureCommunicationSenderAddress "<your sender address>"
```

- Build and deploy **Moonglade.Notification.sln** to the Azure Function.

## Send by SMTP

> Please note, only basic authentication is supported in this mode. Microsoft 365 has disabled basic authentication, so this won't work with your Microsoft 365 enterprise or personal account.

- Setup Azure CLI and login to your Azure subscription first. 
- Run **"\Deployment\DeploySMTP.ps1"** to setup the Azure Function App and Azure Key Vault.

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
    "MOONGLADE_EMAIL_STORAGE": "<storage account connection string>",
    "MOONGLADE_EMAIL_SENDER_NAME": "Moonglade Notification Azure Function (local)",
    "MOONGLADE_EMAIL_PROVIDER": "smtp",
    "MOONGLADE_EMAIL_SSL": true,
    "SmtpServerPort": 25,
    "SmtpServer": "smtp.example.com",
    "SmtpUserName": "admin@example.com",
    "EmailAccountPassword": "<smtp password>"
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
