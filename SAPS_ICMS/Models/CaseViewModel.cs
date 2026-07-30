namespace SAPS_ICMS.Models
{
    public class CaseViewModel
    {
        public Case Case { get; set; } = new Case();
        public List<Witness> Witnesses { get; set; } = new List<Witness>();
    }
}
