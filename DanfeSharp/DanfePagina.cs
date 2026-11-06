using System;
using System.Drawing;
using System.Linq;
using DanfeSharp.Blocos;
using DanfeSharp.Graphics;
using org.pdfclown.documents;
using org.pdfclown.documents.contents.composition;

namespace DanfeSharp
{
    internal class DanfePagina
    {
        public Danfe Danfe { get; private set; }
        public Page PdfPage { get; private set; }
        public PrimitiveComposer PrimitiveComposer { get; private set; }
        public Gfx Gfx { get; private set; }
        public RectangleF RetanguloNumeroFolhas { get;  set; }
        public RectangleF RetanguloCorpo { get; private set; }
        public RectangleF RetanguloDesenhavel { get; private set; }
        public RectangleF RetanguloCreditos { get; private set; }
        public RectangleF Retangulo { get; private set; }

        public DanfePagina(Danfe danfe)
        {
            Danfe = danfe ?? throw new ArgumentNullException(nameof(danfe));
            PdfPage = new Page(Danfe.PdfDocument);
            Danfe.PdfDocument.Pages.Add(PdfPage);

            if (Danfe.ViewModel.Orientacao == Orientacao.Retrato)
                Retangulo = new RectangleF(0, 0, Constantes.A4Largura, Constantes.A4Altura);
            else
                Retangulo = new RectangleF(0, 0, Constantes.A4Altura, Constantes.A4Largura);

            // IMPORTANTE: o tamanho da página precisa ser definido ANTES de instanciar o
            // PrimitiveComposer. O pdfclown captura a altura da página (usada no flip do eixo Y
            // para origem no topo-esquerdo) no momento da construção do composer. Se o composer
            // for criado com o tamanho default (A4 retrato, 297mm) e a página só for redimensionada
            // para paisagem (210mm) depois, todo o conteúdo fica deslocado ~87mm para cima e o
            // cabeçalho sai da página. Ver DanfeEventoPagina para o mesmo cuidado.
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
            if (total <= 0) throw new ArgumentOutOfRangeException(nameof(n));
            if (n > total) throw new ArgumentOutOfRangeException("O número da página atual deve ser menor que o total.");

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
        /// Desenha marca d'água "CANCELADA" centralizada na página inteira, em
        /// fonte grande e na diagonal (45°). Padrão visual herdado de
        /// <see cref="DesenharAvisoHomologacao"/> — mesma cor cinza médio
        /// RGB(0.35, 0.35, 0.35). Referência original: PR #8 do @mateuszanini
        /// (2023, branch feature/aviso-cancelamento), commit f658489. Disparo
        /// vem da flag <see cref="DanfeSharp.Modelo.DanfeViewModel.IsCancelled"/>
        /// (ou do cStat=101 do XML) — ver spec danfe-cancelled-watermark.
        /// </summary>
        public void DesenharAvisoCancelamento()
        {
            const string texto = "CANCELADA";
            var fonte = Danfe.EstiloPadrao.CriarFonteRegular(90);
            var largura = fonte.MedirLarguraTexto(texto).ToPoint();

            var pivo = new PointF(Retangulo.X + Retangulo.Width / 2F, Retangulo.Y + Retangulo.Height / 2F);

            // NOTA: o overload PrimitiveComposer.Rotate(angulo, origem) compõe a
            // rotação com uma translação de retorno baseada na ALTURA DA PÁGINA em
            // vez do pivô informado, jogando o texto pra fora da área visível
            // (confirmado inspecionando os operadores "cm" do content stream
            // gerado — a marca ficava presente no PDF, extraível por texto, mas
            // invisível ao renderizar). Por isso a rotação em torno do pivô é
            // montada manualmente aqui: Translate leva a origem local até o pivô e
            // Rotate(angulo) (o overload de 1 argumento, sem esse bug) gira em
            // torno dela.
            //
            // PrimitiveComposer.ShowText(texto, ponto) tem o MESMO tipo de bug: o Y
            // do ponto é sempre tratado como coordenada absoluta de página (aplica
            // "alturaDaPagina - y"), mesmo com o CTM já rotacionado/transladado —
            // então um deslocamento local pequeno (p.ex. -altura/2, pra centralizar
            // verticalmente) vira um valor gigantesco fora da página. Compensa-se
            // pré-invertendo esse flip: passar (alturaDaPagina - yLocalDesejado) faz
            // o flip interno devolver o yLocalDesejado correto.
            Gfx.PrimitiveComposer.BeginLocalState();
            Gfx.PrimitiveComposer.Translate(pivo.X.ToPoint(), pivo.Y.ToPoint());
            Gfx.PrimitiveComposer.Rotate(45);
            Gfx.PrimitiveComposer.SetFillColor(new org.pdfclown.documents.contents.colorSpaces.DeviceRGBColor(0.35, 0.35, 0.35));
            Gfx.SetFont(fonte);
            // yLocalDesejado calibrado empiricamente medindo o bounding box do texto
            // já renderizado (rasterizando o PDF): nem "-altura/2" (metade da altura
            // de linha da fonte, que reserva espaço pra descendentes que "CANCELADA"
            // não tem, por ser só caixa alta) nem 0 batem exatamente com o centro
            // visual da tinta — ambos deixavam sobrar um desvio residual que, a 45°,
            // vaza igualmente pros eixos X e Y da página (por isso a marca parecia
            // "puxada pra direita"). Medindo o desvio em dois pontos de calibração
            // (yLocalDesejado = -altura/2 e = 0) e extrapolando linearmente, o valor
            // abaixo é o que zera o desvio do centro visual em relação ao pivô.
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

                if (bloco is BlocoIdentificacaoEmitente)
                {
                    var rf = (bloco as BlocoIdentificacaoEmitente).RetanguloNumeroFolhas;
                    RetanguloNumeroFolhas = rf;
                }
            }

            RetanguloCorpo = RetanguloDesenhavel;
            Gfx.Flush();
        }
    }
}
