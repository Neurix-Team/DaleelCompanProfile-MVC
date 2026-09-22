namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Which CRM record an activity or task hangs off. Activities and tasks attach to
    /// any of the four record types, and no single table can be the FK target, so the
    /// link is stored as a (type, id) pair and validated in the business layer.
    /// </summary>
    public enum CrmEntityType
    {
        Lead,
        Contact,
        Company,
        Deal
    }
}
