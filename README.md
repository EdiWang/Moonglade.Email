# Moonglade.Email

The Azure Function used by my blog (https://edi.wang) to send notifications.

This Function sets HTML template and send email notifications to blog administrator or users.

## Get Started

Tools | Alternative
--- | ---
[.NET 6.0 SDK](http://dot.net) | N/A
[Visual Studio 2022](https://visualstudio.microsoft.com/) with Azure Development payload| [Visual Studio Code](https://code.visualstudio.com/)
[Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) | N/A
[Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest) | N/A

### 1. Setup Azure Key Vault and Azure Function App

> This Function needs a pre-configured email account to send emaills. The account name and server infomations can be configured from environment variables. However, the password must be stored in Azure Key Vault to ensure security. Thus, you have to create an Azure Key Vault first.

You need to setup Azure CLI and login to your Azure subscription first. 

Open **"\Azure-Deployment\Deploy.ps1"**

Parameters example:

```powershell
-regionName "eastasia"
-rsgName "Moonglade-Test-RSG"
-storageAccountName "moongladeteststorage"
-adminEmail "edi.wang@outlook.com"
-emailDisplayName "Moonglade Notification Test"
-smtpServer "smtp.whatever.com"
-smtpUserName "admin@whatever.com"
-pwdValue "P@ssw0rd"
```

### 2. Build and Deploy

Build and deploy **Moonglade.Notification.sln** to the Azure Function that created via ```Deploy.ps1```

### 3. Configure Moonglade

Open **appsettings.json** under your Moonglade instance, add the following settings:

```json
"Email": {
  "ApiEndpoint": "https://yourfunctionappurl",
  "ApiKey": "<your function key>"
}
```

### 3. Local Development and Debugging

For development, create ```local.settings.json``` under "**./src/Moonglade.Function.Email**", this file defines development time settings. It is by default ignored by git, so you will need to manange it on your own.

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

## 免责申明

对于中国用户，我们有一份特定的免责申明。请确保你已经阅读并理解其内容：

- [免责申明（仅限中国用户）](./DISCLAIMER_CN.md)
