using System.ComponentModel.DataAnnotations;

public class CoverImageUuid
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Uuid { get; set; }
}
