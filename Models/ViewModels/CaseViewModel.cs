using System.ComponentModel.DataAnnotations;

namespace better_call_saul.Models.ViewModels;

public class CaseViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;

    public CaseStatus Status { get; set; } = CaseStatus.New;
}