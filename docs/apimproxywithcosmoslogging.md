# Quickstart

## APIm Proxy with Cosmos Logging

### Use Case

- You are running an AI Platform with Azure API Management leveraging AI Gateway for load balancing, circuit breaking, quota, etc.
- You are using APIm products to reduce blast radius of changing API Management when onboarding new consumers.
- You want to restrict AOAI from accessing URLs provided in chat completion requests.

### AI Central Features

- AI Central will provide simple PII Stripped logging to a secure Cosmos Database.
- It will filter AOAI requests removing any URLs that could cause AOAI to fetch URLs and potentially leak data.

### Configuration

``` docker graemefoster/aicentral:latest ``` 

Set the following environment variables

>> ClaimsToKeys will have multiple values. Typically, we will map the appid of Managed Identities to a subscription key.

>> This quickstart expects consumers to provide tokens scoped for Azure Open AI. You don't need to setup RBAC for your consumers to Azure Open AI. Libraries like PromptFlow hardcode this scope, so that's why we look for it.

| Environment Variable                 | Definition                                                                |
|--------------------------------------|---------------------------------------------------------------------------|
| ApimEndpointUri                      | Base Uri of API Management AI Gateway API                                 |
| TenantId                             | Tenant Id linked to your JWTs                                             |
| IncomingClaimName                    | Claim to use to target a subscription key                                 |
| CosmosConnectionString               | Cosmos connection string for prompt and response logging                  |
| StorageConnectionString              | Storage connection string used to enqueue prompts / responses for logging |
| TextAnalyticsEndpoint                | Uri of text analytics service for PII stripping                           |
| TextAnalyticsKey                     | Key for text analytics service                                            |
| ClaimsToKeys__{idx}__ClaimValue      | Value of claim                                                            |
| ClaimsToKeys__{idx}__SubscriptionKey | APIm subscription key                                                     |

### Architecture Diagram

TODO