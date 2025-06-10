using PlataformaJiujitsu.Models;

namespace PlataformaAPI.Models
{
    public class Luta
    {
        public int Id { get; set; }

        public int Atleta1Id { get; set; }
        public Atleta Atleta1 { get; set; }

        public int Atleta2Id { get; set; }
        public Atleta Atleta2 { get; set; }

        public int? ChaveId { get; set; }
        public Chave Chave { get; set; }
    }
}
