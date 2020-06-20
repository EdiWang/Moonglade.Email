# ---------------------------------------------------------------------------------------------------------
# Quick Start deployment script for running Moonglade.Notification Azure Function
# Author: Edi Wang
# ---------------------------------------------------------------------------------------------------------

# Replace with your own values
$subscriptionName = "Microsoft MVP"
$rsgName = "Moonglade-Test-RSG"
$regionName = "eastasia"
$storageAccountName = "moongladeteststorage"
$adminEmail = "edi.wang@outlook.com"
$emailDisplayName = "Moonglade Notification Test"
$smtpServer = "smtp.whatever.com"
$smtpUserName = "admin@whatever.com"
$pwdValue = "P@ssw0rd"

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

if(Check-Command -cmdname 'az') {
    Write-Host "Azure CLI is found on your machine. If something blow up, please check update for Azure CLI." -ForegroundColor Yellow
}
else {
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    az login
}

# Confirmation
Write-Host "Your Moonglade.Notification will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continue."
Read-Host -Prompt "Press [ENTER] to continue"

# Select Subscription
az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Create Resource Group
Write-Host ""
Write-Host "Preparing Resource Group" -ForegroundColor Green
$rsgExists = az group exists -n $rsgName
if ($rsgExists -eq 'false') {
    az group create -l $regionName -n $rsgName
}

# Create Storage Account
Write-Host ""
Write-Host "Preparing Storage Account" -ForegroundColor Green
$storageAccountCheck = az storage account list --query "[?name=='$storageAccountName']" | ConvertFrom-Json
$storageAccountExists = $storageAccountCheck.Length -gt 0
if (!$storageAccountExists) {
    Write-Host "Creating Storage Account"
    az storage account create --name $storageAccountName --resource-group $rsgName --location $regionName --sku Standard_LRS --kind StorageV2
}

# Create Key Vault and set secret
Write-Host ""
Write-Host "Preparing Key Vault" -ForegroundColor Green
$keyVaultName = "moonglade-kvt" + (Get-Random -Maximum 1000)
$pwdKey = "Notification-Email-Password"
az keyvault create --name $keyVaultName --resource-group $rsgName --location $regionName
az keyvault secret set --vault-name $keyVaultName --name $pwdKey --value $pwdValue
$pwdSecret = az keyvault secret show --vault-name $keyVaultName --name $pwdKey | ConvertFrom-Json
$pwdSecretId = $pwdSecret.id

# Create Function App
Write-Host ""
Write-Host "Preparing Azure Function App" -ForegroundColor Green
$funcAppName = "moonglade-ntfunc" + (Get-Random -Maximum 1000)
$appCheck = az functionapp list --query "[?name=='$funcAppName']" | ConvertFrom-Json
$appExists = $appCheck.Length -gt 0
if (!$appExists) {
    az functionapp create --functions-version 3 --consumption-plan-location $regionName --name $funcAppName --os-type Windows --resource-group $rsgName --runtime dotnet --storage-account $storageAccountName
    az functionapp config set -g $rsgName -n $funcAppName --use-32bit-worker-process false --http20-enabled true
}
az functionapp config appsettings set -g $rsgName -n $funcAppName --settings AdminEmail=$adminEmail
az functionapp config appsettings set -g $rsgName -n $funcAppName --settings EmailDisplayName=$emailDisplayName
az functionapp config appsettings set -g $rsgName -n $funcAppName --settings SmtpServer=$smtpServer
az functionapp config appsettings set -g $rsgName -n $funcAppName --settings SmtpUserName=$smtpUserName

# Azure CLI get buggy and I have to work around this truncating values issue, very 996
# https://github.com/Azure/azure-cli/issues/10066
$tempSettings = "EmailAccountPassword=@Microsoft.KeyVault(SecretUri=$pwdSecretId)"
az functionapp config appsettings set -g $rsgName -n $funcAppName --settings `"$tempSettings`"

Write-Host ""
Write-Host "Setting Identity for Function App to access Azure Key Vault" -ForegroundColor Green
$json = az functionapp identity assign --name $funcAppName --resource-group $rsgName | ConvertFrom-Json
$principalId = $json.principalId
az keyvault set-policy --name $keyVaultName --object-id $principalId --secret-permissions get list --key-permissions get list --certificate-permissions get list

Read-Host -Prompt "Setup is done, you can now deploy the code and assign function keys, press [ENTER] to exit."