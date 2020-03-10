# ---------------------------------------------------------------------------------------------------------
# Quick Start deployment script for running Moonglade.Notification API on Microsoft Azure
# Author: Edi Wang
# ---------------------------------------------------------------------------------------------------------

# Replace with your own values
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

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

if(Check-Command -cmdname 'az') {
    Write-Host "Azure CLI is found on your machine. If something blow up, please check update for Azure CLI." -ForegroundColor Yellow
    az --version
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

# Create Key Vault and set secret
Write-Host ""
Write-Host "Preparing Key Vault" -ForegroundColor Green
az keyvault create --name $keyVaultName --resource-group $rsgName --location $regionName
az keyvault secret set --vault-name $keyVaultName --name $pwdKey --value $pwdValue

# Create App Service Plan
Write-Host ""
Write-Host "Preparing App Service Plan" -ForegroundColor Green
$planCheck = az appservice plan list --query "[?name=='$aspName']" | ConvertFrom-Json
$planExists = $planCheck.Length -gt 0
if (!$planExists) {
    az appservice plan create -n $aspName -g $rsgName --sku S1 --location $regionName
}

# Create API App
Write-Host ""
Write-Host "Preparing API App" -ForegroundColor Green
$appCheck = az webapp list --query "[?name=='$apiAppName']" | ConvertFrom-Json
$appExists = $appCheck.Length -gt 0
if (!$appExists) {
    az webapp create -g $rsgName -p $aspName -n $apiAppName
    az webapp config set -g $rsgName -n $apiAppName --always-on true --use-32bit-worker-process false --http20-enabled true
}
az webapp config appsettings set -g $rsgName -n $apiAppName --settings AppSettings:AdminEmail=$adminEmail
az webapp config appsettings set -g $rsgName -n $apiAppName --settings AppSettings:EmailDisplayName=$emailDisplayName
az webapp config appsettings set -g $rsgName -n $apiAppName --settings AppSettings:SmtpServer=$smtpServer
az webapp config appsettings set -g $rsgName -n $apiAppName --settings AppSettings:SmtpUserName=$smtpUserName

Write-Host ""
Write-Host "Setting Identity for API App to access Azure Key Vault" -ForegroundColor Green
$json = az webapp identity assign --name $apiAppName --resource-group $rsgName | ConvertFrom-Json
$principalId = $json.principalId
az keyvault set-policy --name $keyVaultName --object-id $principalId --secret-permissions get list

Read-Host -Prompt "Setup is done, you can now deploy the code and assign API keys, press [ENTER] to exit."