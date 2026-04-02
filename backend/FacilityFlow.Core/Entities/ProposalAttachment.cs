namespace FacilityFlow.Core.Entities;

public class ProposalAttachment
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid AttachmentId { get; set; }
    public Proposal Proposal { get; set; } = null!;
    public Attachment Attachment { get; set; } = null!;
}
