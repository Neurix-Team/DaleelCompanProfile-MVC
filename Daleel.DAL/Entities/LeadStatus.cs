namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Where a lead sits in the qualification funnel.
    /// </summary>
    public enum LeadStatus
    {
        New,
        Contacted,
        Qualified,
        Unqualified,
        Converted
    }
}
