// using System.Text;
// using AICentral.Pipelines;
// using AICentral.Pipelines.EndpointSelectors;
//
// namespace AICentral.EndpointSelectors;
//
// public class EndpointStepWrapper : IAICentralPipelineStep
// {
//     private readonly IAICentralEndpointSelectorRuntime _selector;
//
//     public EndpointStepWrapper(IAICentralEndpointSelectorRuntime selector)
//     {
//         _selector = selector;
//     }
//     public Task<AICentralResponse> Handle(HttpContext context, AICentralPipelineExecutor pipeline, CancellationToken cancellationToken)
//     {
//         return _selector.Handle(context, pipeline, cancellationToken);
//     }
//
//     public object WriteDebug()
//     {
//         return _selector.WriteDebug();
//     }
// }