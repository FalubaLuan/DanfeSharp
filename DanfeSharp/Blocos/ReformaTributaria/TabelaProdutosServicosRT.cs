using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using DanfeSharp.Graphics;
using DanfeSharp.Modelo;

namespace DanfeSharp.Blocos.ReformaTributaria
{
    /// <summary>
    /// NT 2026.010 item 4.3 — "Dados dos Produtos / Serviços" com as colunas
    /// Descrição | CST/CFOP | Qtd/UN | Vlr unit | Vlr total | Bases de cálculo |
    /// Alíquotas | Valor dos tributos. As três últimas colunas empilham os tributos
    /// (ICMS, IBS UF, IBS Mun, CBS, IS, IPI) com preenchimento por pontos, como no modelo.
    /// Reaproveita <see cref="Tabela"/> sem alterá-la; as linhas são montadas no primeiro
    /// Draw porque o preenchimento por pontos depende da largura real da coluna.
    /// </summary>
    internal class TabelaProdutosServicosRT : ElementoBase
    {
        private const string Vazio = "--";
        private const string Separador = " / ";
        // Margem de segurança para o texto não quebrar por arredondamento de medida.
        private const float Folga = 0.5F;

        // Percentuais de largura (descrição ocupa o restante).
        private const float PCstCfop = 6F, PQtdUn = 5.5F, PUnit = 6.5F, PTotal = 6.5F,
                            PBases = 13.5F, PAliquotas = 17F, PValores = 21F;

        public CabecalhoBloco CabecalhoBloco { get; private set; }
        public Tabela Tabela { get; private set; }
        public DanfeViewModel ViewModel { get; private set; }
        public ReformaTributariaViewModel Reforma { get; private set; }

        private bool _linhasMontadas;

        public TabelaProdutosServicosRT(DanfeViewModel viewModel, ReformaTributariaViewModel reforma, Estilo estilo) : base(estilo)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Reforma = reforma ?? throw new ArgumentNullException(nameof(reforma));
            CabecalhoBloco = new CabecalhoBloco(estilo, "DADOS DOS PRODUTOS / SERVIÇOS");

            var ad = AlinhamentoHorizontal.Direita;
            var ac = AlinhamentoHorizontal.Centro;
            var ae = AlinhamentoHorizontal.Esquerda;

            Tabela = new Tabela(Estilo)
                .ComColuna(0, ae, "DESCRIÇÃO DO PRODUTO / SERVIÇO")
                .ComColuna(PCstCfop, ae, "CST /", "CFOP")
                .ComColuna(PQtdUn, ac, "QTD / UN")
                .ComColuna(PUnit, ad, "VLR UNIT")
                .ComColuna(PTotal, ad, "VLR TOTAL")
                .ComColuna(PBases, ae, "BASES DE CÁLCULO")
                .ComColuna(PAliquotas, ae, "ALÍQUOTAS")
                .ComColuna(PValores, ae, "VALOR DOS TRIBUTOS");

            Tabela.AjustarLarguraColunas();
        }

        private float LarguraUtil(float percentual) => Width * percentual / 100F - 2F * Estilo.PaddingHorizontal - Folga;

        private void MontarLinhas()
        {
            var f = Tabela.FonteCorpo;

            for (int i = 0; i < ViewModel.Produtos.Count; i++)
            {
                var p = ViewModel.Produtos[i];
                var rt = Reforma.ItemEm(i) ?? new ItemReformaTributariaViewModel();

                Tabela.AdicionarLinha(new List<string>
                {
                    Descricao(p, rt),
                    CstCfop(f, p, LarguraUtil(PCstCfop)),
                    Quantidade(p),
                    p.ValorUnitario.Formatar(),
                    p.ValorTotal.Formatar(),
                    Bases(f, p, rt, LarguraUtil(PBases)),
                    Duplas(f, p, rt, LarguraUtil(PAliquotas), true),
                    Duplas(f, p, rt, LarguraUtil(PValores), false)
                });
            }

            _linhasMontadas = true;
        }

        private static string Descricao(ProdutoViewModel p, ItemReformaTributariaViewModel rt)
        {
            var sb = new StringBuilder(p.DescricaoCompleta);

            var tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.Ncm)) tags.Add("[NCM " + p.Ncm + "]");
            if (!string.IsNullOrWhiteSpace(rt.ClassificacaoTributaria)) tags.Add("[cClassTrib " + rt.ClassificacaoTributaria + "]");

            if (tags.Count > 0)
                sb.Append("\r\n").Append(string.Join(" ", tags));

            return sb.ToString();
        }

        private static string Quantidade(ProdutoViewModel p)
        {
            var un = string.IsNullOrWhiteSpace(p.Unidade) ? null : "[" + p.Unidade + "]";
            return p.Quantidade.Formatar() + (un != null ? "\r\n" + un : "");
        }

        // "CST" é origem + CST/CSOSN (3 dígitos no modelo da NT).
        private static string CstCfop(Fonte f, ProdutoViewModel p, float largura)
        {
            var codigo = !string.IsNullOrEmpty(p.Cst) ? p.Cst : p.Csosn;
            var rotulo = !string.IsNullOrEmpty(p.Cst) || string.IsNullOrEmpty(p.Csosn) ? "CST" : "CSOSN";
            var valor = (p.Origem ?? "") + (codigo ?? "");

            var linhas = new List<string>();
            if (valor.Length > 0) linhas.Add(Lider(f, rotulo, valor, largura));
            linhas.Add(Lider(f, "CFOP", p.Cfop.ToString(), largura));
            return string.Join("\r\n", linhas);
        }

        // Coluna "Bases de cálculo": ICMS, IBS / CBS, IS, IPI (uma por linha, só as informadas).
        private static string Bases(Fonte f, ProdutoViewModel p, ItemReformaTributariaViewModel rt, float largura)
        {
            var linhas = new List<string> { Lider(f, "ICMS", p.BaseIcms.Formatar(), largura) };
            if (rt.BaseIbsCbs.HasValue) linhas.Add(Lider(f, "IBS / CBS", rt.BaseIbsCbs.Formatar(), largura));
            if (rt.BaseIs.HasValue) linhas.Add(Lider(f, "IS", rt.BaseIs.Formatar(), largura));
            if (rt.BaseIpi.HasValue) linhas.Add(Lider(f, "IPI", rt.BaseIpi.Formatar(), largura));
            return string.Join("\r\n", linhas);
        }

        // Colunas "Alíquotas" e "Valor dos tributos": duas metades por linha.
        //   ICMS    / CBS
        //   IBS UF  / IPI
        //   IBS MUN / IS
        private static string Duplas(Fonte f, ProdutoViewModel p, ItemReformaTributariaViewModel rt, float largura, bool aliquotas)
        {
            Func<double?, string> fmt = v => !v.HasValue ? null : (aliquotas ? v.Formatar() + "%" : v.Formatar());

            var esq = new[]
            {
                new KeyValuePair<string, string>("ICMS", fmt(aliquotas ? p.AliquotaIcms : p.ValorIcms)),
                new KeyValuePair<string, string>("IBS UF", fmt(aliquotas ? rt.AliquotaIbsUf : rt.ValorIbsUf)),
                new KeyValuePair<string, string>("IBS MUN", fmt(aliquotas ? rt.AliquotaIbsMun : rt.ValorIbsMun))
            };
            var dir = new[]
            {
                new KeyValuePair<string, string>("CBS", fmt(aliquotas ? rt.AliquotaCbs : rt.ValorCbs)),
                new KeyValuePair<string, string>("IPI", fmt(aliquotas ? p.AliquotaIpi : p.ValorIpi)),
                new KeyValuePair<string, string>("IS", fmt(aliquotas ? rt.AliquotaIs : rt.ValorIs))
            };

            float metade = (largura - f.MedirLarguraTexto(Separador)) / 2F;
            var linhas = new List<string>();

            for (int i = 0; i < 3; i++)
            {
                bool temEsq = esq[i].Value != null, temDir = dir[i].Value != null;
                if (!temEsq && !temDir) continue;

                var s = Lider(f, esq[i].Key, temEsq ? esq[i].Value : Vazio, metade);
                if (temDir) s += Separador + Lider(f, dir[i].Key, dir[i].Value, metade);
                linhas.Add(s);
            }

            return string.Join("\r\n", linhas);
        }

        /// <summary>Rótulo + pontos + valor, ocupando a largura (mm) informada.</summary>
        internal static string Lider(Fonte f, string rotulo, string valor, float larguraMm)
        {
            float wPonto = f.MedirLarguraTexto(".");
            float livre = larguraMm - f.MedirLarguraTexto(rotulo) - f.MedirLarguraTexto(valor);
            int n = wPonto > 0 ? Math.Max(1, (int)Math.Floor(livre / wPonto)) : 1;
            return rotulo + new string('.', n) + valor;
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            if (!_linhasMontadas) MontarLinhas();

            Tabela.SetPosition(RetanguloTabela.Location);
            Tabela.SetSize(RetanguloTabela.Size);
            Tabela.Draw(gfx);

            CabecalhoBloco.SetPosition(X, Y);
            CabecalhoBloco.Width = Width;
            CabecalhoBloco.Draw(gfx);
        }

        public RectangleF RetanguloTabela => BoundingBox.CutTop(CabecalhoBloco.Height);
        public bool CompletamenteDesenhada => Tabela.LinhaAtual == ViewModel.Produtos.Count;
        public override bool PossuiContono => false;
    }
}
