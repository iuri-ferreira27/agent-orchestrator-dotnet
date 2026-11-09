namespace AgentOrchestrator.Core.Orchestration;

public interface IModelRouter
{
    string ModelFor(ModelTier tier, RoutingMode mode);
}
