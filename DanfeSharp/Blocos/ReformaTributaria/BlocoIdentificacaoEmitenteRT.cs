using System.Drawing;
using DanfeSharp.Modelo;
using org.pdfclown.documents.contents.xObjects;

namespace DanfeSharp.Blocos.ReformaTributaria
{
    /// <summary>
    /// NT 2026.010 item 4.2 — identificação do emitente com o Código do Regime
    /// Tributário (CRT) e o campo reservado "Tipo de Regime de Apuração do IBS e da CBS".
    /// </summary>
    internal class BlocoIdentificacaoEmitenteRT : BlocoBase
    {
        public const float LarguraCampoChaveNFe = 93F;
        public const float AlturaLinha1 = 30;

        private readonly NumeroNfSerie2 _ifdNfe;
        private readonly IdentificacaoEmitente _idEmitente;

        public BlocoIdentificacaoEmitenteRT(DanfeViewModel viewModel, ReformaTributariaViewModel reforma, Estilo estilo) : base(viewModel, estilo)
        {
            var textoConsulta = new TextoSimples(Estilo, Strings.TextoConsulta)
            {
                Height = 8,
                AlinhamentoHorizontal = AlinhamentoHorizontal.Centro,
                AlinhamentoVertical = AlinhamentoVertical.Centro,
                TamanhoFonte = 9
            };

            var campoChaveAcesso = new Campo("Chave de Acesso", Formatador.FormatarChaveAcesso(ViewModel.ChaveAcesso), estilo, AlinhamentoHorizontal.Centro) { Height = Constantes.CampoAltura };
            var codigoBarras = new Barcode128C(viewModel.ChaveAcesso, Estilo) { Height = AlturaLinha1 - textoConsulta.Height - campoChaveAcesso.Height };

            var coluna3 = new VerticalStack();
            coluna3.Add(codigoBarras, campoChaveAcesso, textoConsulta);

            _ifdNfe = new NumeroNfSerie2(estilo, ViewModel);
            _idEmitente = new IdentificacaoEmitente(Estilo, ViewModel);

            var fl = new FlexibleLine() { Height = coluna3.Height }
                .ComElemento(_idEmitente)
                .ComElemento(_ifdNfe)
                .ComElemento(coluna3)
                .ComLarguras(0, 15, 46.5F);

            MainVerticalStack.Add(fl);

            AdicionarLinhaCampos()
                .ComCampo("Natureza da operação", ViewModel.NaturezaOperacao)
                .ComCampo("Inscrição Estadual", ViewModel.Emitente.Ie, AlinhamentoHorizontal.Centro)
                .ComCampo("Protocolo de autorização", ViewModel.ProtocoloAutorizacao, AlinhamentoHorizontal.Centro)
                .ComLarguras(0, 22F, 30F);

            AdicionarLinhaCampos()
                .ComCampo("Código do Regime Tributário", reforma.CrtDescricao)
                .ComCampo("Inscrição Estadual do Subst. Tributário", ViewModel.Emitente.IeSt, AlinhamentoHorizontal.Centro)
                .ComCampo("Cnpj / Cpf", Formatador.FormatarCpfCnpj(ViewModel.Emitente.CnpjCpf), AlinhamentoHorizontal.Centro)
                .ComLargurasIguais();

            // Campo reservado: a tag ainda não foi publicada, nada é impresso enquanto
            // TipoRegimeApuracaoIbsCbs for nulo.
            AdicionarLinhaCampos()
                .ComCampo("Tipo de Regime de Apuração do IBS e da CBS", reforma.TipoRegimeApuracaoIbsCbs)
                .ComLargurasIguais();
        }

        public XObject Logo
        {
            get => _idEmitente.Logo;
            set => _idEmitente.Logo = value;
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
        public override bool VisivelSomentePrimeiraPagina => false;

        public RectangleF RetanguloNumeroFolhas => _ifdNfe.RetanguloNumeroFolhas;
    }
}
