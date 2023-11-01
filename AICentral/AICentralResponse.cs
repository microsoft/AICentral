using AICentral.PipelineComponents.Endpoints;

namespace AICentral;

public record AICentralResponse(AICentralUsageInformation AiCentralUsageInformation, IResult ResultHandler);
