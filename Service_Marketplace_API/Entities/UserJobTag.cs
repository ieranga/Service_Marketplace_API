namespace Service_Marketplace_API.Entities;

public class UserJobTag
{
    public Guid UserJobId { get; set; }
    public UserJob? UserJob { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
