# Moonglade.Notification

[![Build Status](https://dev.azure.com/ediwang/Edi-GitHub/_apis/build/status/EdiWang.Moonglade.Notification?branchName=master)](https://dev.azure.com/ediwang/Edi-GitHub/_build/latest?definitionId=69&branchName=master)

The Azure Function used by my blog (https://edi.wang) to send notifications.

This Function sets HTML template and send email notifications to blog administrator or users.

> Note: This notification API is just a simple toy. It doesn't use message queues or a database storage to ensure notification delivery, neither can this handle high amount of concurrent traffic. If you are looking for an enterprise level notification service, this is NOT what you should use.

## Build and Run

Tools | Alternative
--- | ---
[.NET Core 3.1 SDK](http://dot.net) | N/A
[Visual Studio 2019](https://visualstudio.microsoft.com/) with Azure Development payload| [Visual Studio Code](https://code.visualstudio.com/)
[Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) | N/A

### Setup Azure Key Vault and Azure Function App

> This Function needs a pre-configured email account to send emaills. The account name and server infomations can be configured from environment variables. However, the password must be stored in Azure Key Vault to ensure security. Thus, you have to create an Azure Key Vault first.

You need to install Azure CLI and login to your Azure subscription first. See [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest)

Open **"\Azure-Deployment-Script\Deploy.ps1"**

Replace these with your own values, and run the PowerShell script:

// TODO: Outdated

```powershell
$subscriptionName = "Microsoft MVP"
$rsgName = "Moonglade-Test-RSG"
$regionName = "East Asia"
$keyVaultName = "Edi-Test-KV"
$pwdKey = "Notification-Email-Password"
$pwdValue = "P@ssw0rd"
$apiAppName = "moonglade-notification-test"
$aspName = "moonglade-test-plan"
$adminEmail = "edi.test@outlook.com"
$emailDisplayName = "Moonglade Notification Test"
$smtpServer = "smtp.whatever.com"
$smtpUserName = "admin"
```

### Build Source

Build and run **Moonglade.Notification.sln**

For development, create ```local.settings.json``` under "**./src/Moonglade.Notification.AzFunc**", this file defines development time settings. It is by default ignored by git, so you will need to manange it on your own.

Sample ```local.settings.json``` file

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "EnableSsl": true,
    "SmtpServerPort": 587,
    "AdminEmail": "edi.wang@outlook.com",
    "EmailDisplayName": "Moonglade Notification Azure Function (local)",
    "SmtpServer": "smtp-mail.outlook.com",
    "SmtpUserName": "edi.wang@outlook.com",
    "EmailAccountPassword": "**********" 
  }
}
```