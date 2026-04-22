// NokiaHome — Azure infrastructure
// App Service Plan (F1 Free, Windows) + Web App (.NET 9)

@description('Azure region for all resources.')
param location string = 'norwayeast'

@description('Name of the App Service Plan.')
param appServicePlanName string = 'asp-nokiahome'

@description('Name of the Web App. Must be globally unique — becomes <name>.azurewebsites.net.')
param webAppName string = 'nokiahome'

@description('Value sent as ET-Client-Name header to the Entur APIs.')
param enturClientName string = 'hilmarelverhoy-nokiajourneyplanner'

// ---------------------------------------------------------------------------
// App Service Plan — F1 Free tier (Windows, shared compute)
// ---------------------------------------------------------------------------
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'windows'
  properties: {
    reserved: false // false = Windows host OS
  }
}

// ---------------------------------------------------------------------------
// Web App — ASP.NET Core MVC (.NET 9), framework-dependent deployment
// ---------------------------------------------------------------------------
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app' // Windows web app
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      // Signals to the portal that this is a .NET (Core) app
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      // App settings surfaced as environment variables at runtime
      appSettings: [
        {
          name: 'Entur__ClientName'
          value: enturClientName
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
      // F1 does not support Always On — must be false
      alwaysOn: false
      // Deploy published output as a run-from-package zip
      use32BitWorkerProcess: true // F1 only supports 32-bit worker
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
output resourceGroupName string = resourceGroup().name
