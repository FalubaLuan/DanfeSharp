using System;
using System.Drawing;
using System.Linq;
using DanfeSharp.Blocos;
using DanfeSharp.Graphics;
using org.pdfclown.documents;
using org.pdfclown.documents.contents.composition;

namespace DanfeSharp
{
    /// <summary>
    /// Página do <see cref="DanfeReformaTributaria"/> (NT 2026.010). Espelha
    /// <see cref="DanfePagina"/> (que depende do tipo <see cref="Danfe"/>) para não
    /// alterá-la; as marcas d'água repetem o mesmo visual.
    /// </summary>
    internal class DanfePaginaReformaTributaria
    {
        public DanfeReformaTributaria Danfe { get; private set; }
        public Page PdfPage { get; private set; }
        public PrimitiveComposer PrimitiveComposer { get; private set; }
        public Gfx Gfx { get; private set; }
        public RectangleF RetanguloNumeroFolhas { get; set; }
        public RectangleF RetanguloCorpo { get; private set; }
        public RectangleF RetanguloDesenhavel { get; private set; }
        public RectangleF RetanguloCreditos { get; private set; }
        public RectangleF Retangulo { get; private set; }

        public DanfePaginaReformaTributaria(DanfeReformaTributaria danfe)
        {
            Danfe = danfe ?? throw new ArgumentNullException(nameof(danfe));
            PdfPage = new Page(Danfe.PdfDocument);
            Danfe.PdfDocument.Pages.Add(PdfPage);

            Retangulo = Danfe.ViewModel.Orientacao == Orientacao.Retrato
                ? new RectangleF(0, 0, Constantes.A4Largura, Constantes.A4Altura)
                : new RectangleF(0, 0, Constantes.A4Altura, Constantes.A4Largura);

            // O tamanho da página precisa ser definido ANTES do PrimitiveComposer (ver DanfePagina).
            PdfPage.Size = new SizeF(Retangulo.Width.ToPoint(), Retangulo.Height.ToPoint());

            PrimitiveComposer = new PrimitiveComposer(PdfPage);
            Gfx = new Gfx(PrimitiveComposer);

            RetanguloDesenhavel = Retangulo.InflatedRetangle(Danfe.ViewModel.Margem);
            RetanguloCreditos = new RectangleF(RetanguloDesenhavel.X, RetanguloDesenhavel.Bottom + Danfe.EstiloPadrao.PaddingSuperior, RetanguloDesenhavel.Width, Retangulo.Height - RetanguloDesenhavel.Height - Danfe.EstiloPadrao.PaddingSuperior);
        }

        public void DesenharCreditos(string creditos)
        {
            Gfx.DrawString(creditos, RetanguloCreditos, Danfe.EstiloPadrao.CriarFonteItalico(6), AlinhamentoHorizontal.Direita);
        }

        private void DesenharCanhoto()
        {
            if (Danfe.ViewModel.QuantidadeCanhotos == 0) return;

            var canhoto = Danfe.Canhoto;
            canhoto.SetPosition(RetanguloDesenhavel.Location);

            if (Danfe.ViewModel.Orientacao == Orientacao.Retrato)
            {
                canhoto.Width = RetanguloDesenhavel.Width;

                for (int i = 0; i < Danfe.ViewModel.QuantidadeCanhotos; i++)
                {
                    canhoto.Draw(Gfx);
                    canhoto.Y += canhoto.Height;
                }

                RetanguloDesenhavel = RetanguloDesenhavel.CutTop(canhoto.Height * Danfe.ViewModel.QuantidadeCanhotos);
            }
            else
            {
                canhoto.Width = RetanguloDesenhavel.Height;
                Gfx.PrimitiveComposer.BeginLocalState();
                Gfx.PrimitiveComposer.Rotate(90, new PointF(0, canhoto.Width + canhoto.X + canhoto.Y).ToPointMeasure());

                for (int i = 0; i < Danfe.ViewModel.QuantidadeCanhotos; i++)
                {
                    canhoto.Draw(Gfx);
                    canhoto.Y += canhoto.Height;
                }

                Gfx.PrimitiveComposer.End();
                RetanguloDesenhavel = RetanguloDesenhavel.CutLeft(canhoto.Height * Danfe.ViewModel.QuantidadeCanhotos);
            }
        }

        public void DesenhaNumeroPaginas(int n, int total)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (total <= 0) throw new ArgumentOutOfRangeException(nameof(total));
            if (n > total) throw new ArgumentOutOfRangeException(nameof(n), "O número da página atual deve ser menor que o total.");

            Gfx.DrawString($"Folha {n}/{total}", RetanguloNumeroFolhas, Danfe.EstiloPadrao.FonteNumeroFolhas, AlinhamentoHorizontal.Centro);
            Gfx.Flush();
        }

        public void DesenharAvisoHomologacao()
        {
            TextStack ts = new TextStack(RetanguloCorpo) { AlinhamentoVertical = AlinhamentoVertical.Centro, AlinhamentoHorizontal = AlinhamentoHorizontal.Centro, LineHeightScale = 0.9F }
                .AddLine("SEM VALOR FISCAL", Danfe.EstiloPadrao.CriarFonteRegular(48))
                .AddLine("AMBIENTE DE HOMOLOGAÇÃO", Danfe.EstiloPadrao.CriarFonteRegular(30));

            Gfx.PrimitiveComposer.BeginLocalState();
            Gfx.PrimitiveComposer.SetFillColor(new org.pdfclown.documents.contents.colorSpaces.DeviceRGBColor(0.35, 0.35, 0.35));
            ts.Draw(Gfx);
            Gfx.PrimitiveComposer.End();
        }

        /// <summary>
        /// Marca d'água "CANCELADA". Mesma técnica e calibração de
        /// <see cref="DanfePagina.DesenharAvisoCancelamento"/> (ver comentários lá sobre os
        /// bugs de Rotate(angulo, origem) e ShowText do PDFClown).
        /// </summary>
        public void DesenharAvisoCancelamento()
        {
            const string texto = "CANCELADA";
            var fonte = Danfe.EstiloPadrao.CriarFonteRegular(90);
            var largura = fonte.MedirLarguraTexto(texto).ToPoint();
            var pivo = new PointF(Retangulo.X + Retangulo.Width / 2F, Retangulo.Y + Retangulo.Height / 2F);

            Gfx.PrimitiveComposer.BeginLocalState();
            Gfx.PrimitiveComposer.Translate(pivo.X.ToPoint(), pivo.Y.ToPoint());
            Gfx.PrimitiveComposer.Rotate(45);
            Gfx.PrimitiveComposer.SetFillColor(new org.pdfclown.documents.contents.colorSpaces.DeviceRGBColor(0.35, 0.35, 0.35));
            Gfx.SetFont(fonte);
            var alturaPaginaPt = Retangulo.Height.ToPoint();
            const float yLocalDesejado = 26.73F;
            Gfx.PrimitiveComposer.ShowText(texto, new PointF(-largura / 2F, alturaPaginaPt - yLocalDesejado));
            Gfx.PrimitiveComposer.End();
        }

        public void DesenharBlocos(bool isPrimeirapagina = false)
        {
            if (isPrimeirapagina && Danfe.ViewModel.QuantidadeCanhotos > 0) DesenharCanhoto();

            var blocos = isPrimeirapagina ? Danfe._Blocos : Danfe._Blocos.Where(x => x.VisivelSomentePrimeiraPagina == false);

            foreach (var bloco in blocos)
            {
                bloco.Width = RetanguloDesenhavel.Width;

                if (bloco.Posicao == PosicaoBloco.Topo)
                {
                    bloco.SetPosition(RetanguloDesenhavel.Location);
                    RetanguloDesenhavel = RetanguloDesenhavel.CutTop(bloco.Height);
                }
                else
                {
                    bloco.SetPosition(RetanguloDesenhavel.X, RetanguloDesenhavel.Bottom - bloco.Height);
                    RetanguloDesenhavel = RetanguloDesenhavel.CutBottom(bloco.Height);
                }

                bloco.Draw(Gfx);

                if (bloco is Blocos.ReformaTributaria.BlocoIdentificacaoEmitenteRT emitente)
                    RetanguloNumeroFolhas = emitente.RetanguloNumeroFolhas;
            }

            RetanguloCorpo = RetanguloDesenhavel;
            Gfx.Flush();
        }
    }
}
