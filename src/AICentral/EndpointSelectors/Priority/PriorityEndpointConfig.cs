﻿namespace AICentral.EndpointSelectors.Priority;

public class PriorityEndpointConfig
{
    public string[]? PriorityEndpoints { get; init; }
    public string[]? FallbackEndpoints { get; init; }
}