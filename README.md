# Moonglade.Notification (Preview)

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Moonglade.Notification-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=58)

.NET Core 3.0 Notification API used by my blog (https://edi.wang), runs on Microsoft Azure.

This API sets HTML template and send Email notifications to Blog administrator or users.

## Build and Run

> The following tools are required for development.

Tools | Alternative
--- | ---
[.NET Core 3.0 SDK](http://dot.net) | N/A
[Visual Studio 2019](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/)
[Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) | N/A

### Setup Azure Key Vault

This API needs a pre-configured email account to send emaills. The account name and server infomations can be configured in appsettings.json or from environment variables. However, the password must be stored in Azure Key Vault to ensure security. Thus, you have to create an Azure Key Vault first.

To be cool, please use Azure Cloud Shell. 

You can also do it in [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest)

```bash
az keyvault create --name "<YourKeyVaultName>" --resource-group "<YourResourceGroupName>" --location "<Region Name>"
```

Set the email account password which the API is going to use to send emails.

```bash
az keyvault secret set --vault-name "<YourKeyVaultName>" --name "<YourEmailAccountPasswordKeyName>" --value "<YourEmailAccountPasswordValue>"
```

Open your **appsettings.[env].json** and set the values.

```json
"AzureKeyVault": {
  "Name": "<YourKeyVaultName>",
  "SmtpPasswordKey": "<YourEmailAccountPasswordKeyName>"
}
```

#### For Production

```bash
az webapp identity assign --name "<YourAPIAppName>" --resource-group "<YourResourceGroupName>"
```

Make a note of the output when you publish the application to Azure. It should be of the format:

```bash
{
  "principalId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "type": "SystemAssigned"
}
```

Then, run this command by using the name of your key vault and the value of **PrincipalId**:

```bash
az keyvault set-policy --name '<YourKeyVaultName>' --object-id <PrincipalId> --secret-permissions get list
```

### Build Source

1. Create an "**appsettings.Development.json**" under "**src\Moonglade.Notification.API**", this file defines development time settings. It is by default ignored by git, so you will need to manange it on your own.

2. Build and run **Moonglade.Notification.sln**

## Configuration

> Below section discuss system settings in **appsettings.[env].json**.

### Email Server

AppSettings section defines the email server configuration. Change the values as you need.

```json
"EnableEmailSending": true,
"EnableSsl": true,
"SmtpServerPort": 587,
"AdminEmail": "{AZURE-APPSVC-ENV}",
"EmailDisplayName": "{AZURE-APPSVC-ENV}",
"SmtpServer": "{AZURE-APPSVC-ENV}",
"SmtpUserName": "{AZURE-APPSVC-ENV}",
```

You can use Azure CLI to setup these values for App Service:

For example:

```bash
az webapp config appsettings set -g <Resource Group Name> -n <API App Name> --settings AppSettings:AdminEmail=<Admin Email>
```

### ApiKeys

You must assign owners and their API keys before you can call the API.

Define owners, roles, and API keys in AppSettings:ApiKeys

*Roles are currently not used, in the future, Administrators will have management APIs while users can only make notification requests.*

```json
"ApiKeys": [
  {
    "Id": 1,
    "Owner": "https-edi.wang",
    "Key": "{AZURE-APPSVC-ENV}",
    "Roles": [ "Administrators" ]
  }
```