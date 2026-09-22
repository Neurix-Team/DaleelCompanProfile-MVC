namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Where a lead came from. The three form values map 1:1 to the public
    /// intake forms so channel reporting is meaningful.
    /// </summary>
    public enum LeadSource
    {
        Website,
        ContactForm,
        ScheduleDemo,
        PartnerInquiry,
        Referral,
        ColdCall,
        Event,
        SocialMedia,
        Other
    }
}
