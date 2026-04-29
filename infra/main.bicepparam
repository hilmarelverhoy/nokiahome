// Parameter values for NokiaHome Azure deployment
// Reference: infra/main.bicep
//
// Secrets are read from environment variables at deploy time via readEnvironmentVariable().
// - GitHub Actions: set as repository secrets and exposed as env vars in the workflow
// - Local deploy:   export LINEAR_API_KEY / LINEAR_TEAM_ID / OPENAI_API_KEY before running deploy.sh

using './main.bicep'

param location = 'norwayeast'
param appServicePlanName = 'asp-nokiahome'
param webAppName = 'nokiahome'
param enturClientName = 'hilmarelverhoy-nokiajourneyplanner'
param linearApiKey = readEnvironmentVariable('LINEAR_API_KEY')
param linearTeamId = readEnvironmentVariable('LINEAR_TEAM_ID')
param openAiApiKey = readEnvironmentVariable('OPENAI_API_KEY')

