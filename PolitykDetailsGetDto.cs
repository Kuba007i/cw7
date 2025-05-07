namespace APBD_Test1_Example.DTOs;

public class PolitykDetailsGetDto
{
    public int ID { get; set; }
    public string Imie { get; set; }
    public string Nazwisko { get; set; }
    public string Powiedzenie { get; set; }
    public List<PolitykPartiaGetDto> Partie { get; set; }
}