Release Notes:

0.16.13 (23/mar/2024)
Allowed JWT tokens to be restricted to specific Deployments as-well as Pipelines
Allowed enforcement of Deployments for AOAI allowing a Pipeline to restrict the available Deployments

0.16.12 (21/mar/2024)
Added a inbuilt JWT Token Provider that can produce time-bound JWTs for use in pipelines. Perfect for Hackathons!

0.16.11 (20/mar/2024)
Allow Model Mappings for Azure Open AI deployments

0.16.8 (15/mar/2024)
Fixed a bug when array is passed into Embeddings.

0.16.7 (10/mar/2024)
Fixed a bug when complex types are passed into Chat Completions.

0.15.1 (23/feb/2024)
Fixed missing token counts for streaming completions requests

0.15.0 (22/feb/2024)
Adding a flag to show Streaming requests

0.9.0 (29/jan/2024)
Added Pipeline Name to the IncomingRequestDetails so it can be used by steps. 

0.7.10 (16/jan/2024)
Improved Open Telemetry metrics

0.7 (12/jan/2024)
Added Role Authorisation to Entra Consumer Auth Provider
