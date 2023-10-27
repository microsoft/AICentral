// using System.Text;
// using AICentral.Pipelines;
//
// namespace AICentral.EndpointSelectors;
//
// public class EndpointStepWrapper : IAICentralPipelineStep
// {
//     private readonly IAICentralEndpointSelector _selector;
//
//     public EndpointStepWrapper(IAICentralEndpointSelector selector)
//     {
//         _selector = selector;
//     }
//     public Task<CentralCommandResponse> Handle(HttpContext context, CentralCommandPipelineExecutor pipeline, CancellationToken cancellationToken)
//     {
//         return _selector.Handle(context, pipeline, cancellationToken);
//     }
//
//     public object WriteDebug()
//     {
//         return _selector.WriteDebug();
//     }
// }