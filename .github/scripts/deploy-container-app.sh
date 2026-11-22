#!/usr/bin/env bash
set -euo pipefail

group=rg-supplysmart
registry_name=supplysmartacr
registry_server="$registry_name.azurecr.io"
target=(--name "${APP_NAME:?}" --resource-group "$group")
: "${IMAGE:?}" "${DATABASE_CONNECTION:?}" "${JWT_SIGNING_KEY:?}"

group_exists=$(az group exists --name "$group" --output tsv)
if [[ "$group_exists" == false ]]; then
    region=$(az acr show --name "$registry_name" --query location --output tsv)
    az group create --name "$group" --location "$region" --output none
fi
existing=$(az containerapp list --resource-group "$group" --query "[?name=='$APP_NAME'].name | [0]" --output tsv)
if [[ -z "$existing" ]]; then
    environment=${CONTAINER_APP_ENVIRONMENT:-}
    if [[ -z "$environment" ]]; then
        environment=$(az containerapp env list --resource-group "$group" --query '[0].name' --output tsv)
    fi
    environment=${environment:-$APP_NAME-env}
    if [[ "$environment" != /* ]]; then
        found=$(az containerapp env list --resource-group "$group" --query "[?name=='$environment'].name | [0]" --output tsv)
        if [[ -z "$found" ]]; then
            region=$(az group show --name "$group" --query location --output tsv)
            az containerapp env create --name "$environment" --resource-group "$group" --location "$region" --output none
        fi
    fi
fi

# New apps use their managed environment's default domain. Existing apps report their own ingress hostname.
frontend_url=${FRONTEND_URL:-}
if [[ -z "$frontend_url" ]]; then
    if [[ -n "$existing" ]]; then
        fqdn=$(az containerapp show "${target[@]}" --query properties.configuration.ingress.fqdn --output tsv)
    else
        env_target=(--name "$environment" --resource-group "$group")
        if [[ "$environment" == /* ]]; then
            env_target=(--ids "$environment")
        fi
        domain=$(az containerapp env show "${env_target[@]}" --query properties.defaultDomain --output tsv)
        if [[ -z "$domain" ]]; then
            echo "Azure did not return a default domain for the Container Apps environment." >&2
            exit 1
        fi
        fqdn="$APP_NAME.$domain"
    fi
    frontend_url="https://$fqdn"
fi
frontend_url=${frontend_url%/}
if [[ ! "$frontend_url" =~ ^https://[a-zA-Z0-9][a-zA-Z0-9.-]*(:[0-9]+)?$ ]]; then
    echo "A public HTTPS origin is required for browser sign-in, billing returns and email links." >&2
    exit 1
fi

# Only secret references go into the container revision. Pass values as individual CLI arguments.
secrets=()
settings=("FrontendUrl=$frontend_url" "EmailSettings__BaseUrl=$frontend_url"
          "EmailSettings__Host=smtp.sendgrid.net" "EmailSettings__Port=587"
          "EmailSettings__SenderEmail=osterfnl@mail.uc.edu" "EmailSettings__SenderName=SupplySmart"
          "EmailSettings__Username=apikey")
bind_secret() {
    local name=$1 setting=$2 value=$3
    if [[ -n "$value" ]]; then
        secrets+=("$name=$value")
        settings+=("$setting=secretref:$name")
    else
        settings+=("$setting=")
    fi
}
bind_secret db-connection 'ConnectionStrings__supplysmart-db' "$DATABASE_CONNECTION"
bind_secret jwt-signing-key JwtSettings__Secret "$JWT_SIGNING_KEY"
bind_secret stripe-key StripeSettings__SecretKey "${STRIPE_SECRET_KEY:-}"
bind_secret stripe-webhook StripeSettings__WebhookSecret "${STRIPE_WEBHOOK_SECRET:-}"
bind_secret stripe-starter StripeSettings__StarterPriceId "${STRIPE_STARTER_PRICE_ID:-}"
bind_secret stripe-professional StripeSettings__ProfessionalPriceId "${STRIPE_PROFESSIONAL_PRICE_ID:-}"
bind_secret stripe-enterprise StripeSettings__EnterprisePriceId "${STRIPE_ENTERPRISE_PRICE_ID:-}"
bind_secret mail-api-key EmailSettings__ApiKey "${SENDGRID_API_KEY:-}"

if [[ -n "$existing" ]]; then
    az containerapp secret set "${target[@]}" --secrets "${secrets[@]}" --output none
    if [[ -n "${ACR_PASSWORD:-}" ]]; then
        az containerapp registry set "${target[@]}" --server "$registry_server" \
            --username "$registry_name" --password "$ACR_PASSWORD" --output none
    fi
    az containerapp update "${target[@]}" --image "$IMAGE" --set-env-vars "${settings[@]}" --output none
else
    registry_args=(--registry-server "$registry_server")
    if [[ -n "${ACR_PASSWORD:-}" ]]; then
        registry_args+=(--registry-username "$registry_name" --registry-password "$ACR_PASSWORD")
    fi
    az containerapp create "${target[@]}" --environment "$environment" --image "$IMAGE" \
        "${registry_args[@]}" --ingress external --target-port 8080 \
        --secrets "${secrets[@]}" --env-vars "${settings[@]}" --output none
fi
