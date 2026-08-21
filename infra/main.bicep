// Azure PaaS footprint for the local directory application.
// Deploy: az deployment group create -g <rg> -f infra/main.bicep -p namePrefix=noidapages jwtSigningKey=<secret>

@description('Short prefix used for all resource names.')
param namePrefix string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('App Service plan SKU. B1 is the smallest tier with Always On support.')
param appServicePlanSku string = 'B1'

@description('JWT signing key. Pass at deploy time or wire Key Vault instead.')
@secure()
param jwtSigningKey string

@description('Seeded administrator email address.')
param adminEmail string = 'admin@example.com'

@description('Seeded administrator password.')
@secure()
param adminPassword string

@description('Database provider. SqlServer provisions Azure SQL; Sqlite keeps the pilot file database on /home.')
@allowed([
  'SqlServer'
  'Sqlite'
])
param databaseProvider string = 'SqlServer'

@description('Azure SQL administrator login.')
param sqlAdminLogin string = 'ypadmin'

@description('Azure SQL administrator password.')
@secure()
param sqlAdminPassword string = ''

@description('Azure SQL database SKU. GP_S_Gen5_1 is serverless and auto-pauses when idle.')
param sqlSkuName string = 'GP_S_Gen5_1'

@description('Maximum database size in bytes.')
param sqlMaxSizeBytes int = 34359738368

var planName = '${namePrefix}-plan'
var webAppName = '${namePrefix}-web'
var storageName = toLower(replace('${namePrefix}stor', '-', ''))
var insightsName = '${namePrefix}-ai'
var sqlServerName = '${namePrefix}-sql'
var sqlDatabaseName = 'yellowpages'
var photoContainer = 'ad-photos'
var keyRingContainer = 'dataprotection-keys'
var useSqlServer = databaseProvider == 'SqlServer'

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

var sqlConnectionString = 'Server=tcp:${sqlServerName}${environment().suffixes.sqlServerHostname},1433;Initial Catalog=${sqlDatabaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=False;Connection Timeout=30;'

var sqliteConnectionString = 'Data Source=/home/data/yellowpages.db;Cache=Shared;Pooling=True'

var effectiveConnectionString = useSqlServer ? sqlConnectionString : sqliteConnectionString

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = if (useSqlServer) {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allows connections from Azure services (the App Service outbound addresses).
// Tighten this to Private Endpoint or VNet integration before handling real user data.
resource sqlFirewall 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = if (useSqlServer) {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = if (useSqlServer) {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: sqlSkuName
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: sqlMaxSizeBytes
    zoneRedundant: false
    // Serverless: pause after an hour idle so a low-traffic pilot costs almost nothing.
    autoPauseDelay: 60
    minCapacity: json('0.5')
  }
}

// Shared Data Protection key ring, required the moment the app runs on 2+ instances.
resource keyRing 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: keyRingContainer
  properties: {
    publicAccess: 'None'
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storage
  name: 'default'
}

resource photos 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: photoContainer
  properties: {
    publicAccess: 'Blob'
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: planName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/healthz'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'Database__Provider', value: databaseProvider }
        { name: 'ConnectionStrings__Default', value: effectiveConnectionString }
        { name: 'Database__UseEnsureCreated', value: 'false' }
        { name: 'Database__CommandTimeoutSeconds', value: '60' }
        { name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE', value: useSqlServer ? 'false' : 'true' }
        { name: 'DataProtection__BlobConnectionString', value: storageConnectionString }
        { name: 'DataProtection__Container', value: keyRingContainer }
        { name: 'Jwt__Issuer', value: 'https://${webAppName}.azurewebsites.net' }
        { name: 'Jwt__Audience', value: 'yellowpages-local-web' }
        { name: 'Jwt__SigningKey', value: jwtSigningKey }
        { name: 'Jwt__AccessTokenMinutes', value: '20' }
        { name: 'Jwt__RefreshTokenDays', value: '14' }
        { name: 'Storage__Provider', value: 'AzureBlob' }
        { name: 'Storage__BlobContainer', value: photoContainer }
        { name: 'Storage__BlobConnectionString', value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'Storage__BlobPublicBaseUrl', value: 'https://${storage.name}.blob.${environment().suffixes.storage}/${photoContainer}' }
        { name: 'Seed__AdminEmail', value: adminEmail }
        { name: 'Seed__AdminPassword', value: adminPassword }
        { name: 'Seed__SeedSampleAds', value: 'false' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
      ]
    }
  }
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output storageAccount string = storage.name
output sqlServerFqdn string = useSqlServer ? '${sqlServerName}${environment().suffixes.sqlServerHostname}' : ''
output databaseProviderInUse string = databaseProvider
