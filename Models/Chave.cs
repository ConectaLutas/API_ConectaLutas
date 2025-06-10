using PlataformaJiujitsu.Models;
using System.Collections.Generic;

namespace PlataformaAPI.Models
{
    public class Chave
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string Nome { get; set; }

        public List<Inscricao> Inscricoes { get; set; } = new();
        public List<Luta> Lutas { get; set; } = new();
    }
}
