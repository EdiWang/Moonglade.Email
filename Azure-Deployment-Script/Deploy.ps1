# Replace with your own values
$rsgName = "Moonglade-Test-RSG"
$regionName = "East Asia"
$keyVaultName = "Edi-Test-KV"
$pwdKey = "Notification-Email-Password"
$pwdValue = "P@ssw0rd"
$apiAppName = "moonglade-notification-test"

# Select Subscription
az account set --subscription "Microsoft MVP"

# Create Resource Group
# TODO: Check if exist
az group create -l $regionName -n $rsgName

# Create Key Vault and Secret
az keyvault create --name $keyVaultName --resource-group $rsgName --location $regionName

az keyvault secret set --vault-name $keyVaultName --name $pwdKey --value $pwdValue

# Create API App
