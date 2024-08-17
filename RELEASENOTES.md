Release Notes:

0.20.0 (17/aug/2024)
- Support running in Azure Fuctions
- PII Stripping Logger now able to use Managed Identities (User / System assigned)
- Updated the PII Stripping Quickstart to use Managed Identities over access keys.

0.19.4 (01/aug/2024)
- Embeddings don't expect the array to be of strings anymore, when passed as an array.

0.19.1/2/3 (29/jul/2024)
- Fixed a bug stopping PII Stripped embeddings being logged
- Added AI Search Vectorization nuget publish

0.19.0 (29/jul/2024)
- Added 'route proxies' enabling new routes to proxy to downstream Open AI services
- Published a AI Search Vectorization route so you can use AI Central over a Private Endpoint to handle AI Search Vectorization request.

0.18.5 (15/jul/2024)
- Stopped retrying 400's against different servers. 400's are badly formed requests, content-filter triggers, etc, and should not be retried.

0.18.4 (15/jul/2024)
- Better partition key on Cosmos database logger.

0.18.3 (12/jul/2024)
- HealthCheck bundled into docker container
- Improved resiliency and error handling of PII Stripping consumer loop

0.18.1 (10/jul/2024)
- Capacity Based Endpoint selector. Reduces chance of 429 providing better overall latency.

0.18.0 (10/jul/2024)
- PII Stripping logger that logs to Cosmos DB for prompt / response audit
- Request filtering to block sending URLs to chat-completion that Open AI will reach-out for.

0.17.0 (06/jul/2024)
- New backend authenticator that can add a header, as-well as pass through a Bearer token. Helps with Azure APIm AI Gateway that has the concept of Subscriptions and products, but also can authorise using a JWT.

0.16.18 (13/may/2024)
- Support the Streaming Token counts if returned

0.16.13 (23/mar/2024)
- Allowed JWT tokens to be restricted to specific Deployments as-well as Pipelines
- Allowed enforcement of Deployments for AOAI allowing a Pipeline to restrict the available Deployments

0.16.12 (21/mar/2024)
- Added a inbuilt JWT Token Provider that can produce time-bound JWTs for use in pipelines. Perfect for Hackathons!

0.16.11 (20/mar/2024)
- Allow Model Mappings for Azure Open AI deployments

0.16.8 (15/mar/2024)
- Fixed a bug when array is passed into Embeddings.

0.16.7 (10/mar/2024)
- Fixed a bug when complex types are passed into Chat Completions.

0.15.1 (23/feb/2024)
- Fixed missing token counts for streaming completions requests

0.15.0 (22/feb/2024)
- Adding a flag to show Streaming requests

0.9.0 (29/jan/2024)
- Added Pipeline Name to the IncomingRequestDetails, so it can be used by steps. 

0.7.10 (16/jan/2024)
- Improved Open Telemetry metrics

0.7 (12/jan/2024)
- Added Role Authorisation to Entra Consumer Auth Provider
