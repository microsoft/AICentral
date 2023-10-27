using AICentral.Pipelines.Endpoints;

namespace AICentral.Pipelines;

public record AICentralResponse(AICentralUsageInformation AiCentralUsageInformation, IResult ResultHandler);
