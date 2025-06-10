using PlataformaAPI.Models;

namespace PlataformaJiujitsu.Models;

public class Inscricao
{
    public int Id { get; set; }

    public int AtletaId { get; set; }
    public Atleta Atleta { get; set; }

    public int CampeonatoId { get; set; }
    public Campeonato Campeonato { get; set; }

    public int? CategoriaId { get; set; }
    public Categoria Categoria { get; set; }

   
    public int? ChaveId { get; set; }  // Pode ser nullable se a inscrição puder existir sem chave
    public Chave? Chave { get; set; }  // Propriedade de navegação (opcional se já existir)
    public DateTime DataInscricao { get; set; }

}


