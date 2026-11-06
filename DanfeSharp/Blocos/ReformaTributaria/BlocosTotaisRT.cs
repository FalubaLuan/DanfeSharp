using DanfeSharp.Modelo;

namespace DanfeSharp.Blocos.ReformaTributaria
{
    /// <summary>NT 2026.010 — "Total dos Produtos e Total da Nota".</summary>
    internal class BlocoTotalNotaRT : BlocoBase
    {
        public BlocoTotalNotaRT(DanfeViewModel viewModel, Estilo estilo) : base(viewModel, estilo)
        {
            var m = viewModel.CalculoImposto;

            AdicionarLinhaCampos()
                .ComCampoNumerico("Valor total dos produtos", m.ValorTotalProdutos)
                .ComCampoNumerico("Valor do frete", m.ValorFrete)
                .ComCampoNumerico("Valor do seguro", m.ValorSeguro)
                .ComCampoNumerico("Desconto", m.Desconto)
                .ComCampoNumerico("Outras despesas acessórias", m.OutrasDespesas)
                .ComCampoNumerico("Valor total da nota", m.ValorTotalNota)
                .ComLargurasIguais();
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
        public override string Cabecalho => "Total dos Produtos e Total da Nota";
    }

    /// <summary>
    /// NT 2026.010 — "Total do ICMS / IPI". As linhas de FCP/DIFAL e de ICMS
    /// monofásico são facultativas: só aparecem quando há dado no XML (item 4.4).
    /// </summary>
    internal class BlocoTotalIcmsIpiRT : BlocoBase
    {
        public BlocoTotalIcmsIpiRT(DanfeViewModel viewModel, ReformaTributariaViewModel reforma, Estilo estilo) : base(viewModel, estilo)
        {
            var m = viewModel.CalculoImposto;
            var t = reforma.Totais;

            AdicionarLinhaCampos()
                .ComCampoNumerico("Base de cálculo do ICMS", m.BaseCalculoIcms)
                .ComCampoNumerico("Valor do ICMS", m.ValorIcms)
                .ComCampoNumerico("Base de cálculo do ICMS ST", m.BaseCalculoIcmsSt)
                .ComCampoNumerico("Valor do ICMS ST", m.ValorIcmsSt)
                .ComCampoNumerico("Valor do IPI", m.ValorIpi)
                .ComLargurasIguais();

            if (m.ValorFcp.HasValue || m.vICMSUFDest.HasValue || t.ValorFcpSt.HasValue || t.ValorFcpUfDest.HasValue)
            {
                AdicionarLinhaCampos()
                    .ComCampoNumerico("Valor do FCP", m.ValorFcp)
                    .ComCampoNumerico("Valor do FCP retido por ST", t.ValorFcpSt)
                    .ComCampoNumerico("Valor do DIFAL na UF de destino", m.vICMSUFDest)
                    .ComCampoNumerico("Valor do FCP na UF de destino", t.ValorFcpUfDest)
                    .ComLargurasIguais();
            }

            if (t.TemIcmsMonofasico)
            {
                AdicionarLinhaCampos()
                    .ComCampoNumerico("BC do ICMS monofásico", t.QuantidadeBcIcmsMono, 4)
                    .ComCampoNumerico("Valor do ICMS monofásico", t.ValorIcmsMono)
                    .ComCampoNumerico("BC do ICMS monofásico por retenção", t.QuantidadeBcIcmsMonoReten, 4)
                    .ComCampoNumerico("Valor do ICMS monofásico por retenção", t.ValorIcmsMonoReten)
                    .ComLargurasIguais();
            }
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
        public override string Cabecalho => "Total do ICMS / IPI";
    }

    /// <summary>NT 2026.010 item 4.1 — novo bloco "Total do IBS / CBS / IS".</summary>
    internal class BlocoTotalIbsCbsIsRT : BlocoBase
    {
        public BlocoTotalIbsCbsIsRT(DanfeViewModel viewModel, ReformaTributariaViewModel reforma, Estilo estilo) : base(viewModel, estilo)
        {
            var t = reforma.Totais;

            AdicionarLinhaCampos()
                .ComCampoNumerico("Valor da CBS", t.ValorCbs)
                .ComCampoNumerico("Valor do IBS UF", t.ValorIbsUf)
                .ComCampoNumerico("Valor do IBS Município", t.ValorIbsMun)
                .ComCampoNumerico("Valor do Imposto Seletivo", t.ValorIs)
                .ComLargurasIguais();

            // Monofasia: exibida quando o grupo gMono foi informado.
            if (t.TemMonofasiaIbsCbs)
            {
                AdicionarLinhaCampos()
                    .ComCampoNumerico("Valor do IBS monofásico", t.ValorIbsMono)
                    .ComCampoNumerico("Valor da CBS monofásica", t.ValorCbsMono)
                    .ComCampoNumerico("Valor do IBS monofásico por retenção", t.ValorIbsMonoReten)
                    .ComCampoNumerico("Valor da CBS monofásica por retenção", t.ValorCbsMonoReten)
                    .ComLargurasIguais();
            }
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
        public override string Cabecalho => "Total do IBS / CBS / IS";
    }

    /// <summary>
    /// ISSQN logo após os totais (posição do modelo da NT 2026.010), reaproveitando
    /// o bloco clássico sem alterá-lo.
    /// </summary>
    internal class BlocoCalculoIssqnRT : BlocoCalculoIssqn
    {
        public BlocoCalculoIssqnRT(DanfeViewModel viewModel, Estilo estilo) : base(viewModel, estilo) { }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
    }
}
