using System.ComponentModel.DataAnnotations;
using ApexPost.Domain;

namespace ApexPost.Web.Models;

public sealed class IdentityVerificationViewModel
{
    [Required] public string TransactionId { get; set; } = "";
    [Required] public IdentityDocumentType DocumentType { get; set; }
    [Required, StringLength(100, MinimumLength = 4)] public string DocumentNumber { get; set; } = "";
    [Required] public VerificationResult Result { get; set; } = VerificationResult.Successful;
}
