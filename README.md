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

此项目(Moonglade.Email)及其配套组件均为免费开源的产品，仅用于学习交流，并且不直接向中国提供服务，中国用户请于下载后立即删除。

任何中国境内的组织及个人不得使用此项目(Moonglade.Email)及其配套组件构建任何形式的面向中国境内用户的网站或服务。

不可用于任何违反中华人民共和国(含台湾省)或使用者所在地区法律法规的用途。

因为作者即本人仅完成代码的开发和开源活动(开源即任何人都可以下载使用)，从未参与用户的任何运营和盈利活动。

且不知晓用户后续将程序源代码用于何种用途，故用户使用过程中所带来的任何法律责任即由用户自己承担。

[《开源软件有漏洞，作者需要负责吗？是的！》](https://go.edi.wang/aka/os251)