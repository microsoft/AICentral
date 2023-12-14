namespace AICentral.Core;

public enum AICallType
{
    Chat,
    Completions,
    Embeddings,
    DALLE3,

    //supported when we do a direct pass through. This is only allowed if we have the 
    //same service type on both ends, e.g. Listen for Azure Open AI requests, and proxy to Azure Open AI
    //This may throw runtime errors for example when we have image requests to Azure Open AI
    //which works in a asynchronous 'poll for completion' style.
    Other
}
