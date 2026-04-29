// Parameter values for NokiaHome Azure deployment
// Reference: infra/main.bicep
//
// Secrets (linearApiKey, linearTeamId, openAiApiKey) are NOT stored here.
// - GitHub Actions: stored as repository secrets, passed via --parameters in the workflow
// - Local deploy:   passed via LINEAR_API_KEY / LINEAR_TEAM_ID / OPENAI_API_KEY env vars in deploy.sh

using './main.bicep'

param location = 'norwayeast'
param appServicePlanName = 'asp-nokiahome'
param webAppName = 'nokiahome'
param enturClientName = 'hilmarelverhoy-nokiajourneyplanner'
