using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Domain;

public class Appeal
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 500 characters long.")]
    public string? Description { get; set; }
    
    [DisplayName("Date")]
    public DateTime EntryTime { get; set; }
    
    [DisplayName("Resolution Deadline")]
    [DisplayFormat(DataFormatString = "HH:mm dd.MM.yyyy", ApplyFormatInEditMode = true)]
    [FutureDate(ErrorMessage = "Resolution deadline must be at least 30 minutes after entry time.")]
    public DateTime ResolutionDeadline { get; set; }
    
    public bool IsResolved { get; set; }
    
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.Now.AddMinutes(30);
            }
            return false;
        }
    }
}