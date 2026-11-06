using System;
using System.Collections.Generic;
using System.Linq;
using DanfeSharp.Blocos;
using DanfeSharp.Blocos.ReformaTributaria;
using DanfeSharp.Modelo;
using org.pdfclown.documents;
using org.pdfclown.documents.contents.fonts;
using org.pdfclown.files;

namespace DanfeSharp
{
    /// <summary>
    /// <para>DANFE modelo 55 no leiaute da NT 2026.010 (Reforma Tributária — IBS/CBS/IS).</para>
    /// <para>Classe independente de <see cref="Danfe"/> (que permanece inalterado): reaproveita
    /// os blocos clássicos que não mudam (canhoto, destinatário, fatura, transportador...) e
    /// usa blocos novos para identificação do emitente, totais, itens e dados adicionais.</para>
    /// <para>Uso: <c>new DanfeReformaTributaria(viewModel, reforma)</c>, onde <c>reforma</c> vem de
    /// <see cref="ReformaTributariaViewModelCreator"/> aplicado ao mesmo XML.</para>
    /// </summary>
    public class DanfeReformaTributaria : IDisposable
    {
        public DanfeViewModel ViewModel { get; private set; }
        public ReformaTributariaViewModel Reforma { get; private set; }
        public File File { get; private set; }
        internal Document PdfDocument { get; private set; }

        internal BlocoCanhoto Canhoto { get; private set; }
        internal BlocoIdentificacaoEmitenteRT IdentificacaoEmitente { get; private set; }
        internal List<BlocoBase> _Blocos;
        internal Estilo EstiloPadrao { get; private set; }
        internal List<DanfePaginaReformaTributaria> Paginas { get; private set; }

        private readonly StandardType1Font _FonteRegular;
        private readonly StandardType1Font _FonteNegrito;
        private readonly StandardType1Font _FonteItalico;

        private bool _FoiGerado;
        private readonly string _creditos;
        private readonly string _metadataCriador;
        private org.pdfclown.documents.contents.xObjects.XObject _LogoObject;

        public DanfeReformaTributaria(DanfeViewModel viewModel, ReformaTributariaViewModel reforma, string creditos = null, string metadataCriador = null)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Reforma = reforma ?? throw new ArgumentNullException(nameof(reforma));

            _creditos = creditos ?? "Impresso com JaguarDFe";
            _metadataCriador = metadataCriador ?? String.Format("{0} {1} - {2}", "JaguarDFe", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version, "https://github.com/SilverCard/DanfeSharp");

            _Blocos = new List<BlocoBase>();
            File = new File();
            PdfDocument = File.Document;

            // Item 7.7 do Manual do DANFE: Times New Roman ou Courier New.
            _FonteRegular = new StandardType1Font(PdfDocument, StandardType1Font.FamilyEnum.Times, false, false);
            _FonteNegrito = new StandardType1Font(PdfDocument, StandardType1Font.FamilyEnum.Times, true, false);
            _FonteItalico = new StandardType1Font(PdfDocument, StandardType1Font.FamilyEnum.Times, false, true);

            EstiloPadrao = CriarEstilo();
            Paginas = new List<DanfePaginaReformaTributaria>();

            Canhoto = new BlocoCanhoto(ViewModel, EstiloPadrao);
            IdentificacaoEmitente = new BlocoIdentificacaoEmitenteRT(ViewModel, Reforma, EstiloPadrao);
            _Blocos.Add(IdentificacaoEmitente);
            _Blocos.Add(new BlocoDestinatarioRemetente(ViewModel, EstiloPadrao));

            if (ViewModel.LocalRetirada != null && ViewModel.ExibirBlocoLocalRetirada)
                _Blocos.Add(new BlocoLocalRetirada(ViewModel, EstiloPadrao));

            if (ViewModel.LocalEntrega != null && ViewModel.ExibirBlocoLocalEntrega)
                _Blocos.Add(new BlocoLocalEntrega(ViewModel, EstiloPadrao));

            if (ViewModel.Duplicatas.Count > 0)
                _Blocos.Add(new BlocoDuplicataFatura(ViewModel, EstiloPadrao));

            if (ViewModel.Pagamento != null && ViewModel.Pagamento.Any(p => p.DetalhePagamento != null && p.DetalhePagamento.Count > 0))
                _Blocos.Add(new BlocoFormaPagamento(ViewModel, EstiloPadrao));

            // Blocos de totais usam cabeçalho de campo menor para caber 4-6 campos por linha.
            var estiloTotais = CriarEstilo(5F);
            _Blocos.Add(new BlocoTotalNotaRT(ViewModel, estiloTotais));
            _Blocos.Add(new BlocoTotalIcmsIpiRT(ViewModel, Reforma, estiloTotais));
            _Blocos.Add(new BlocoTotalIbsCbsIsRT(ViewModel, Reforma, estiloTotais));

            if (ViewModel.CalculoIssqn.Mostrar)
                _Blocos.Add(new BlocoCalculoIssqnRT(ViewModel, EstiloPadrao));

            _Blocos.Add(new BlocoTransportador(ViewModel, EstiloPadrao));
            _Blocos.Add(new BlocoDadosAdicionaisRT(ViewModel, CriarEstilo(tFonteCampoConteudo: 8), PdfDocument, Reforma.QrCode ?? ViewModel.QrCode));

            AdicionarMetadata();
            _FoiGerado = false;
        }

        /// <summary>Cria o DANFE direto de um XML de NF-e processada (nfeProc).</summary>
        public static DanfeReformaTributaria CriarDeStringXml(string xml, string creditos = null, string metadataCriador = null)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));

            return new DanfeReformaTributaria(
                DanfeViewModelCreator.CriarDeStringXml(xml),
                ReformaTributariaViewModelCreator.CriarDeStringXml(xml),
                creditos, metadataCriador);
        }

        public void AdicionarLogoImagem(System.IO.Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var img = org.pdfclown.documents.contents.entities.Image.Get(stream);
            if (img == null) throw new InvalidOperationException("O logotipo não pode ser carregado, certifique-se que a imagem esteja no formato JPEG não progressivo.");
            _LogoObject = img.ToXObject(PdfDocument);
        }

        public void AdicionarLogoPdf(System.IO.Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var pdfFile = new org.pdfclown.files.File(new org.pdfclown.bytes.Stream(stream)))
            {
                _LogoObject = pdfFile.Document.Pages[0].ToXObject(PdfDocument);
            }
        }

        public void AdicionarLogoImagem(String path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                AdicionarLogoImagem(fs);
        }

        public void AdicionarLogoPdf(String path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                AdicionarLogoPdf(fs);
        }

        private void AdicionarMetadata()
        {
            var info = PdfDocument.Information;
            info[new org.pdfclown.objects.PdfName("ChaveAcesso")] = ViewModel.ChaveAcesso;
            info[new org.pdfclown.objects.PdfName("TipoDocumento")] = "DANFE";
            info.CreationDate = DateTime.Now;
            info.Title = "DANFE Reforma Tributária (Documento auxiliar da NFe)";
            info.Creator = _metadataCriador;
        }

        private Estilo CriarEstilo(float tFonteCampoCabecalho = 6, float tFonteCampoConteudo = 10)
        {
            return new Estilo(_FonteRegular, _FonteNegrito, _FonteItalico, tFonteCampoCabecalho, tFonteCampoConteudo);
        }

        public void Gerar()
        {
            if (_FoiGerado) throw new InvalidOperationException("O Danfe já foi gerado.");

            IdentificacaoEmitente.Logo = _LogoObject;
            var tabela = new TabelaProdutosServicosRT(ViewModel, Reforma, EstiloPadrao);

            int paginasSemEspacoConsecutivas = 0;

            while (true)
            {
                var p = CriarPagina();

                // Mesmo tratamento de Danfe.Gerar: se os blocos do topo consumirem toda a
                // altura, a tabela vai para a próxima página.
                if (p.RetanguloCorpo.Size.Height <= 0)
                {
                    if (++paginasSemEspacoConsecutivas > 1)
                    {
                        throw new InvalidOperationException(
                            "Não foi possível renderizar a tabela de produtos: " +
                            "os blocos do cabeçalho consomem toda a altura disponível em duas páginas consecutivas. " +
                            "Reduza blocos opcionais (LocalRetirada/LocalEntrega, infCpl longas) ou use orientação Retrato.");
                    }
                    continue;
                }

                paginasSemEspacoConsecutivas = 0;

                tabela.SetPosition(p.RetanguloCorpo.Location);
                tabela.SetSize(p.RetanguloCorpo.Size);
                tabela.Draw(p.Gfx);

                p.Gfx.Stroke();
                p.Gfx.Flush();

                if (tabela.CompletamenteDesenhada)
                    break;
            }

            for (int i = 0; i < Paginas.Count; i++)
                Paginas[i].DesenhaNumeroPaginas(i + 1, Paginas.Count);

            _FoiGerado = true;
        }

        private DanfePaginaReformaTributaria CriarPagina()
        {
            var p = new DanfePaginaReformaTributaria(this);
            Paginas.Add(p);
            p.DesenharBlocos(Paginas.Count == 1);

            if (!string.IsNullOrWhiteSpace(_creditos))
                p.DesenharCreditos(_creditos);

            if (ViewModel.TipoAmbiente == 2)
                p.DesenharAvisoHomologacao();

            if (ViewModel.IsCancelled || ViewModel.CodigoStatusReposta == 101)
                p.DesenharAvisoCancelamento();

            return p;
        }

        public void Salvar(String path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            File.Save(path, SerializationModeEnum.Incremental);
        }

        public void Salvar(System.IO.Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            File.Save(new org.pdfclown.bytes.Stream(stream), SerializationModeEnum.Incremental);
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) File.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);
        #endregion
    }
}
