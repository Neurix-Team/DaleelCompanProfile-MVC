namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Sales pipeline stages, in pipeline order. <see cref="Won"/> and <see cref="Lost"/>
    /// are the closed stages.
    /// </summary>
    public enum DealStage
    {
        New,
        Qualified,
        Proposal,
        Negotiation,
        Won,
        Lost
    }
}
