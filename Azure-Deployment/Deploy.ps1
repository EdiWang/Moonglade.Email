# ---------------------------------------------------------------------------------------------------------
# Quick Start deployment script for running Moonglade.Notification Azure Function
# Author: Edi Wang
# ---------------------------------------------------------------------------------------------------------

param(
    $regionName = "eastasia",
    $adminEmail = "edi.wang@outlook.com",
    $rsgName = "moongladegroup",
    $storageAccountName = "moongladestorage",
    $emailDisplayName = "Moonglade Notification",
    $smtpServer = "smtp.whatever.com",
    $smtpUserName = "admin@whatever.com",
    $smtpPassword = "P@ssw0rd"
)

[Console]::ResetColor()
# az login --use-device-code
$output = az account show -o json | ConvertFrom-Json
$subscriptionList = az account list -o json | ConvertFrom-Json 
$subscriptionList | Format-Table name, id, tenantId -AutoSize
$subscriptionName = $output.name
Write-Host "Currently logged in to subscription """$output.name.Trim()""" in tenant " $output.tenantId
$subscriptionName = Read-Host "Enter subscription Id ("$output.id")"
$subscriptionName = $subscriptionName.Trim()
if ([string]::IsNullOrWhiteSpace($subscriptionName)) {
    $subscriptionName = $output.id
}
else {
    # az account set --subscription $subscriptionName
    Write-Host "Changed to subscription ("$subscriptionName")"
}

# Confirmation
Clear-Host
Write-Host "Your Moonglade.Notification will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continue." -ForegroundColor Green
Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

# Select Subscription
$echo = az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Create Resource Group
Write-Host ""
Write-Host "Preparing Resource Group" -ForegroundColor Green
$rsgExists = az group exists -n $rsgName
if ($rsgExists -eq 'false') {
    $echo = az group create -l $regionName -n $rsgName
}

# Create Storage Account
Write-Host ""
Write-Host "Preparing Storage Account" -ForegroundColor Green
$storageAccountCheck = az storage account list --query "[?name=='$storageAccountName']" | ConvertFrom-Json
$storageAccountExists = $storageAccountCheck.Length -gt 0
if (!$storageAccountExists) {
    Write-Host "Creating Storage Account"
    $echo = az storage account create --name $storageAccountName --resource-group $rsgName --location $regionName --sku Standard_LRS --kind StorageV2
}

# Create Key Vault and set secret
Write-Host ""
Write-Host "Preparing Key Vault" -ForegroundColor Green
$keyVaultName = "moonglade-kvt" + (Get-Random -Maximum 1000)
$pwdKey = "Notification-Email-Password"
$echo = az keyvault create --name $keyVaultName --resource-group $rsgName --location $regionName
$echo = az keyvault secret set --vault-name $keyVaultName --name $pwdKey --value $smtpPassword
$pwdSecret = az keyvault secret show --vault-name $keyVaultName --name $pwdKey | ConvertFrom-Json
$pwdSecretId = $pwdSecret.id

# Create Function App
Write-Host ""
Write-Host "Preparing Azure Function App" -ForegroundColor Green
$funcAppName = "moonglade-ntfunc" + (Get-Random -Maximum 1000)
$appCheck = az functionapp list --query "[?name=='$funcAppName']" | ConvertFrom-Json
$appExists = $appCheck.Length -gt 0
if (!$appExists) {
    $echo = az functionapp create --functions-version 4 --consumption-plan-location $regionName --name $funcAppName --os-type Windows --resource-group $rsgName --runtime dotnet --storage-account $storageAccountName
    $echo = az functionapp config set -g $rsgName -n $funcAppName --use-32bit-worker-process false --http20-enabled true
}
$echo = az functionapp config appsettings set -g $rsgName -n $funcAppName --settings AdminEmail=$adminEmail
$echo = az functionapp config appsettings set -g $rsgName -n $funcAppName --settings EmailDisplayName=$emailDisplayName
$echo = az functionapp config appsettings set -g $rsgName -n $funcAppName --settings SmtpServer=$smtpServer
$echo = az functionapp config appsettings set -g $rsgName -n $funcAppName --settings SmtpUserName=$smtpUserName

# Azure CLI get buggy and I have to work around this truncating values issue, very 996
# https://github.com/Azure/azure-cli/issues/10066
$tempSettings = "EmailAccountPassword=@Microsoft.KeyVault(SecretUri=$pwdSecretId)"
$echo = az functionapp config appsettings set -g $rsgName -n $funcAppName --settings `"$tempSettings`"

Write-Host ""
Write-Host "Setting Identity for Function App to access Azure Key Vault" -ForegroundColor Green
$json = az functionapp identity assign --name $funcAppName --resource-group $rsgName | ConvertFrom-Json
$principalId = $json.principalId
$echo = az keyvault set-policy --name $keyVaultName --object-id $principalId --secret-permissions get list --key-permissions get list --certificate-permissions get list

Read-Host -Prompt "Setup is done, you can now deploy the code and assign function keys, press [ENTER] to exit."