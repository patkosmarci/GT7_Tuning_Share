using System.ComponentModel.DataAnnotations;

namespace GT7TuningShare.Module.ViewModels;

public class CreateSetupViewModel : CarSetupPartViewModel
{
    [Required(ErrorMessage = "Please give your setup a title.")]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;
}
