#!/usr/bin/env bash
# infra/deploy.sh — Provision NokiaHome infrastructure on Azure
#
# Usage:
#   ./infra/deploy.sh                          # uses defaults
#   ./infra/deploy.sh --resource-group my-rg   # override resource group
#
# Secrets must be supplied as environment variables:
#   LINEAR_API_KEY   — Linear API key
#   LINEAR_TEAM_ID   — Linear team ID
#   OPENAI_API_KEY   — Azure OpenAI API key
#
# Example:
#   LINEAR_API_KEY=lin_... LINEAR_TEAM_ID=abc123 OPENAI_API_KEY=xyz... ./infra/deploy.sh
#
# Prerequisites:
#   - Azure CLI installed  (https://aka.ms/install-azure-cli)
#   - Bicep CLI installed  (az bicep install)
#   - Logged in           (az login)

set -euo pipefail

# ---------------------------------------------------------------------------
# Defaults — override via env vars or flags
# ---------------------------------------------------------------------------
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-nokiahome}"
LOCATION="${LOCATION:-norwayeast}"
DEPLOYMENT_NAME="nokiahome-$(date +%Y%m%d%H%M%S)"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Parse optional flags
while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group|-g) RESOURCE_GROUP="$2"; shift 2 ;;
    --location|-l)       LOCATION="$2";       shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

# ---------------------------------------------------------------------------
# Validate secrets
# ---------------------------------------------------------------------------
MISSING=()
[[ -z "${LINEAR_API_KEY:-}"  ]] && MISSING+=("LINEAR_API_KEY")
[[ -z "${LINEAR_TEAM_ID:-}"  ]] && MISSING+=("LINEAR_TEAM_ID")
[[ -z "${OPENAI_API_KEY:-}"  ]] && MISSING+=("OPENAI_API_KEY")

if [[ ${#MISSING[@]} -gt 0 ]]; then
  echo "ERROR: Missing required environment variables:"
  for v in "${MISSING[@]}"; do echo "  - $v"; done
  echo ""
  echo "Run with: LINEAR_API_KEY=... LINEAR_TEAM_ID=... OPENAI_API_KEY=... $0"
  exit 1
fi

echo "========================================"
echo " NokiaHome — Azure infrastructure deploy"
echo "========================================"
echo "  Resource group : $RESOURCE_GROUP"
echo "  Location       : $LOCATION"
echo "  Deployment     : $DEPLOYMENT_NAME"
echo "========================================"

# ---------------------------------------------------------------------------
# 1. Ensure logged in
# ---------------------------------------------------------------------------
echo ""
echo "[1/4] Checking Azure CLI login..."
az account show --output none 2>/dev/null || {
  echo "Not logged in. Running 'az login'..."
  az login
}
echo "  Logged in as: $(az account show --query user.name -o tsv)"
echo "  Subscription: $(az account show --query name -o tsv)"

# ---------------------------------------------------------------------------
# 2. Create resource group (idempotent)
# ---------------------------------------------------------------------------
echo ""
echo "[2/4] Creating resource group '$RESOURCE_GROUP' in '$LOCATION'..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output table

# ---------------------------------------------------------------------------
# 3. Deploy Bicep template
# ---------------------------------------------------------------------------
echo ""
echo "[3/4] Deploying Bicep template..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters "$SCRIPT_DIR/main.bicepparam" \
  --output table

# Capture outputs
WEB_APP_NAME=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query properties.outputs.webAppName.value \
  --output tsv)

WEB_APP_URL=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query properties.outputs.webAppUrl.value \
  --output tsv)

# ---------------------------------------------------------------------------
# 4. Print publish profile for GitHub Actions
# ---------------------------------------------------------------------------
echo ""
echo "[4/4] Fetching publish profile for GitHub Actions secret..."
echo ""
echo "  Copy the XML below and store it as a GitHub Actions secret named:"
echo "  AZUREAPPSERVICE_PUBLISHPROFILE_<your-identifier>"
echo ""
echo "--- PUBLISH PROFILE (start) ---"
az webapp deployment list-publishing-profiles \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEB_APP_NAME" \
  --xml
echo "--- PUBLISH PROFILE (end) ---"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo ""
echo "========================================"
echo " Deployment complete"
echo "========================================"
echo "  Web App     : $WEB_APP_NAME"
echo "  URL         : $WEB_APP_URL"
echo "  Resource grp: $RESOURCE_GROUP"
echo ""
echo "Next steps:"
echo "  1. Copy the publish profile XML above into a GitHub secret"
echo "     (Settings → Secrets → Actions → New secret)"
echo "  2. Update the secret name in .github/workflows/main_nokiahome.yml"
echo "     if it differs from the current one"
echo "  3. Push to 'main' (or trigger workflow_dispatch) to deploy the app"
echo "========================================"
# infra/deploy.sh — Provision NokiaHome infrastructure on Azure
#
# Usage:
#   ./infra/deploy.sh                          # uses defaults
#   ./infra/deploy.sh --resource-group my-rg   # override resource group
#
# Prerequisites:
#   - Azure CLI installed  (https://aka.ms/install-azure-cli)
#   - Bicep CLI installed  (az bicep install)
#   - Logged in           (az login)

set -euo pipefail

# ---------------------------------------------------------------------------
# Defaults — override via env vars or flags
# ---------------------------------------------------------------------------
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-nokiahome}"
LOCATION="${LOCATION:-norwayeast}"
DEPLOYMENT_NAME="nokiahome-$(date +%Y%m%d%H%M%S)"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Parse optional --resource-group flag
while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group|-g) RESOURCE_GROUP="$2"; shift 2 ;;
    --location|-l)       LOCATION="$2";       shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

echo "========================================"
echo " NokiaHome — Azure infrastructure deploy"
echo "========================================"
echo "  Resource group : $RESOURCE_GROUP"
echo "  Location       : $LOCATION"
echo "  Deployment     : $DEPLOYMENT_NAME"
echo "========================================"

# ---------------------------------------------------------------------------
# 1. Ensure logged in
# ---------------------------------------------------------------------------
echo ""
echo "[1/4] Checking Azure CLI login..."
az account show --output none 2>/dev/null || {
  echo "Not logged in. Running 'az login'..."
  az login
}
echo "  Logged in as: $(az account show --query user.name -o tsv)"
echo "  Subscription: $(az account show --query name -o tsv)"

# ---------------------------------------------------------------------------
# 2. Create resource group (idempotent)
# ---------------------------------------------------------------------------
echo ""
echo "[2/4] Creating resource group '$RESOURCE_GROUP' in '$LOCATION'..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output table

# ---------------------------------------------------------------------------
# 3. Deploy Bicep template
# ---------------------------------------------------------------------------
echo ""
echo "[3/4] Deploying Bicep template..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters "$SCRIPT_DIR/main.bicepparam" \
  --output table

# Capture outputs
WEB_APP_NAME=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query properties.outputs.webAppName.value \
  --output tsv)

WEB_APP_URL=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query properties.outputs.webAppUrl.value \
  --output tsv)

# ---------------------------------------------------------------------------
# 4. Print publish profile for GitHub Actions
# ---------------------------------------------------------------------------
echo ""
echo "[4/4] Fetching publish profile for GitHub Actions secret..."
echo ""
echo "  Copy the XML below and store it as a GitHub Actions secret named:"
echo "  AZUREAPPSERVICE_PUBLISHPROFILE_<your-identifier>"
echo ""
echo "--- PUBLISH PROFILE (start) ---"
az webapp deployment list-publishing-profiles \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEB_APP_NAME" \
  --xml
echo "--- PUBLISH PROFILE (end) ---"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo ""
echo "========================================"
echo " Deployment complete"
echo "========================================"
echo "  Web App     : $WEB_APP_NAME"
echo "  URL         : $WEB_APP_URL"
echo "  Resource grp: $RESOURCE_GROUP"
echo ""
echo "Next steps:"
echo "  1. Copy the publish profile XML above into a GitHub secret"
echo "     (Settings → Secrets → Actions → New secret)"
echo "  2. Update the secret name in .github/workflows/main_nokiahome.yml"
echo "     if it differs from the current one"
echo "  3. Push to 'main' (or trigger workflow_dispatch) to deploy the app"
echo "========================================"
