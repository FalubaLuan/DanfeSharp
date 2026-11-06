using System.Collections.Generic;

namespace DanfeSharp.Modelo
{
    /// <summary>
    /// Dados adicionais exigidos pela NT 2026.010 (DANFE Reforma Tributária) que
    /// complementam o <see cref="DanfeViewModel"/>. Os itens em <see cref="Itens"/>
    /// seguem a mesma ordem de <see cref="DanfeViewModel.Produtos"/>.
    /// </summary>
    public class ReformaTributariaViewModel
    {
        /// <summary>Itens, na mesma ordem de DanfeViewModel.Produtos.</summary>
        public List<ItemReformaTributariaViewModel> Itens { get; set; } = new List<ItemReformaTributariaViewModel>();

        /// <summary>Totais IBS/CBS/IS e demais totais condicionais do bloco de totais.</summary>
        public TotaisReformaTributariaViewModel Totais { get; set; } = new TotaisReformaTributariaViewModel();

        /// <summary>Código do Regime Tributário (CRT — C21).</summary>
        public string Crt { get; set; }

        /// <summary>
        /// Tipo de Regime de Apuração do IBS e da CBS. Campo reservado: a NT ainda não
        /// definiu a tag, portanto nada é impresso enquanto for nulo.
        /// </summary>
        public string TipoRegimeApuracaoIbsCbs { get; set; }

        /// <summary>CRT formatado como "3 - REGIME NORMAL".</summary>
        public string CrtDescricao
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Crt)) return null;
                switch (Crt.Trim())
                {
                    case "1": return "1 - SIMPLES NACIONAL";
                    case "2": return "2 - SIMPLES NACIONAL - EXCESSO DE SUBLIMITE";
                    case "3": return "3 - REGIME NORMAL";
                    case "4": return "4 - MEI";
                    default: return Crt.Trim();
                }
            }
        }

        /// <summary>Item correspondente ao produto de índice <paramref name="indice"/>, ou nulo.</summary>
        public ItemReformaTributariaViewModel ItemEm(int indice)
            => indice >= 0 && indice < Itens.Count ? Itens[indice] : null;
    }

    /// <summary>
    /// Campos do item (Grupo UB) exibidos nas colunas Bases / Alíquotas / Valores.
    /// Nulo = informação inexistente no XML (não é impressa).
    /// </summary>
    public class ItemReformaTributariaViewModel
    {
        /// <summary>cClassTrib — UB14</summary>
        public string ClassificacaoTributaria { get; set; }

        /// <summary>vBC IBS/CBS — UB16</summary>
        public double? BaseIbsCbs { get; set; }

        /// <summary>pIBSUF (UB18) ou pAliqEfet (UB28) quando há gRed.</summary>
        public double? AliquotaIbsUf { get; set; }
        public double? ValorIbsUf { get; set; }

        /// <summary>pIBSMun (UB37) ou pAliqEfet (UB47) quando há gRed.</summary>
        public double? AliquotaIbsMun { get; set; }
        public double? ValorIbsMun { get; set; }

        /// <summary>pCBS (UB56) ou pAliqEfet (UB66) quando há gRed.</summary>
        public double? AliquotaCbs { get; set; }
        public double? ValorCbs { get; set; }

        /// <summary>vBCIS — UB05</summary>
        public double? BaseIs { get; set; }
        public double? AliquotaIs { get; set; }
        public double? ValorIs { get; set; }

        /// <summary>vBC do IPI (não existe em ProdutoViewModel).</summary>
        public double? BaseIpi { get; set; }
    }

    /// <summary>Totais do bloco "Total do IBS/CBS/IS" e campos condicionais do "Total do ICMS/IPI".</summary>
    public class TotaisReformaTributariaViewModel
    {
        // Total do IBS / CBS / IS
        public double? ValorCbs { get; set; }            // vCBS — W56
        public double? ValorIbsUf { get; set; }          // vIBSUF — W41
        public double? ValorIbsMun { get; set; }         // vIBSMun — W46
        public double? ValorIs { get; set; }             // vIS — W33
        public double? ValorIbsMono { get; set; }        // vIBSMono — W58
        public double? ValorCbsMono { get; set; }        // vCBSMono — W59
        public double? ValorIbsMonoReten { get; set; }   // vIBSMonoReten — W59a
        public double? ValorCbsMonoReten { get; set; }   // vCBSMonoReten — W59b

        // Total do ICMS / IPI — campos facultativos
        public double? ValorFcpSt { get; set; }          // vFCPST — W06a
        public double? ValorFcpUfDest { get; set; }      // vFCPUFDest — W04c
        public double? QuantidadeBcIcmsMono { get; set; }       // qBCMono
        public double? ValorIcmsMono { get; set; }              // vICMSMono — W06c
        public double? QuantidadeBcIcmsMonoReten { get; set; }  // qBCMonoReten
        public double? ValorIcmsMonoReten { get; set; }         // vICMSMonoReten — W06d

        public bool TemMonofasiaIbsCbs =>
            ValorIbsMono.HasValue || ValorCbsMono.HasValue || ValorIbsMonoReten.HasValue || ValorCbsMonoReten.HasValue;

        public bool TemIcmsMonofasico =>
            QuantidadeBcIcmsMono.HasValue || ValorIcmsMono.HasValue || QuantidadeBcIcmsMonoReten.HasValue || ValorIcmsMonoReten.HasValue;
    }
}
