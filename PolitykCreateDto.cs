using System.ComponentModel.DataAnnotations;
namespace APBD_Test1_Example.DTOs;

public class PolitykCreateDto
{
    [MaxLength(50)]
    public required string Imie { get; set; }
    
    [MaxLength(100)]
    public required string Nazwisko { get; set; }
    
    [MaxLength(200)]
    public string? Powiedzenie { get; set; }
    
    public List<int>? PartieAssignments { get; set; }
}