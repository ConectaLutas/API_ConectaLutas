using PlataformaAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace PlataformaAPI.Data.Dtos
{
    public class ReadCampeonatoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string SobreEvento { get; set; }
        public string LocalEvento { get; set; }
        public decimal TaxaInscricao { get; set; }
        public string Premiacoes { get; set; }
        public int MaxInscritos { get; set; }
        public int IdadeMinima { get; set; }
        public int IdadeMaxima { get; set; }
        public string LinkRegulamento { get; set; }
        public string LinkInscricao { get; set; }
        public StatusCampeonato Status { get; set; }
        // Novo campo para a foto
        public string FotoUrl { get; set; }
    }
}
