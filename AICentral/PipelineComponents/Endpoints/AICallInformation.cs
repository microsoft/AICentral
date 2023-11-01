namespace AICentral.PipelineComponents.Endpoints;

public record AICallInformation(AICallType AICallType, string IncomingModelName, string PromptText, string RemainingUrl);