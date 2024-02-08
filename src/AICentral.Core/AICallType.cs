namespace AICentral.Core;

/// <summary>
/// Represents the type of call a consumer is making. This gives us info about the Javascript structure of the call (be careful to check api versions as-well). 
/// </summary>
public enum AICallType
{
    /// <summary>
    /// A chat call
    /// </summary>
    Chat,
    
    /// <summary>
    /// A Chat Completions call
    /// </summary>
    Completions,
    
    /// <summary>
    /// An embeddings request
    /// </summary>
    Embeddings,
    
    /// <summary>
    /// DALLE-2 call (Azure Open AI uses an asynchronous pattern) 
    /// </summary>
    DALLE2,
    
    /// <summary>
    /// DALLE-2 status call (Azure Open AI uses an asynchronous pattern) 
    /// </summary>
    Operations,
    
    /// <summary>
    /// DALLE-3 call
    /// </summary>
    DALLE3,
    
    /// <summary>
    /// Whisper style request to translate audio to text
    /// </summary>
    Transcription,
    
    /// <summary>
    /// Translate between languages
    /// </summary>
    Translation,
    
    Assistants,
    
    Threads,
    
    Files,

    //supported when we do a direct pass through. This is only allowed if we have the 
    //same service type on both ends, e.g. Listen for Azure Open AI requests, and proxy to Azure Open AI
    //This may throw runtime errors for example when we have image requests to Azure Open AI
    //which works in a asynchronous 'poll for completion' style.
    Other
}
