using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using DanfeSharp.Graphics;
using DanfeSharp.Modelo;
using org.pdfclown.documents;
using org.pdfclown.documents.contents.xObjects;

namespace DanfeSharp.Blocos.ReformaTributaria
{
    /// <summary>
    /// NT 2026.010 — "Dados Adicionais" com o QR Code (ZX02) ao lado das informações
    /// complementares. O QR Code só é desenhado quando <see cref="DanfeViewModel.QrCode"/>
    /// está preenchido; sem ele o bloco fica igual ao clássico.
    /// </summary>
    internal class BlocoDadosAdicionaisRT : BlocoBase
    {
        public const float AlturaMinima = 25;
        public const float InfComplementaresLarguraPorcentagem = 55;
        public const float ReservadoFiscoLarguraPorcentagem = 20;
        public const float QrCodeLarguraPorcentagem = 25;

        private readonly CampoMultilinha _cInfComplementares;
        private readonly FlexibleLine _linha;

        public BlocoDadosAdicionaisRT(DanfeViewModel viewModel, Estilo estilo, Document documento) : base(viewModel, estilo)
        {
            _cInfComplementares = new CampoMultilinha("Informações Complementares", ViewModel.TextoAdicional(), estilo);
            var cReservadoFisco = new CampoMultilinha("Reservado ao fisco", ViewModel.TextoReservadoFisco(), estilo);

            _linha = new FlexibleLine() { Height = _cInfComplementares.Height }
                .ComElemento(_cInfComplementares)
                .ComElemento(cReservadoFisco);

            if (string.IsNullOrWhiteSpace(viewModel.QrCode))
            {
                _linha.ComLarguras(75, 0);
            }
            else
            {
                _linha.ComElemento(new ElementoQrCodeRT(estilo, CriarXObject(viewModel.QrCode, documento)));
                _linha.ComLarguras(InfComplementaresLarguraPorcentagem, ReservadoFiscoLarguraPorcentagem, QrCodeLarguraPorcentagem);
            }

            MainVerticalStack.Add(_linha);
        }

        private static XObject CriarXObject(string conteudo, Document documento)
        {
            using (var ms = new MemoryStream())
            {
                using (var bitmap = GerarQrCode.GerarQRCode(conteudo))
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                }

                ms.Position = 0;
                return org.pdfclown.documents.contents.entities.Image.Get(ms).ToXObject(documento);
            }
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                // Mesmo ajuste do bloco clássico: força a altura mínima do InfComplementares.
                if (_cInfComplementares != null && _linha != null)
                {
                    _linha.Width = value;
                    _linha.Posicionar();
                    _cInfComplementares.Height = AlturaMinima;
                    _linha.Height = _cInfComplementares.Height;
                }
            }
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Base;
        public override string Cabecalho => "Dados adicionais";
    }

    /// <summary>Célula com borda que desenha o QR Code quadrado e centralizado.</summary>
    internal class ElementoQrCodeRT : ElementoBase
    {
        private readonly XObject _qrCode;

        public ElementoQrCodeRT(Estilo estilo, XObject qrCode) : base(estilo)
        {
            _qrCode = qrCode ?? throw new ArgumentNullException(nameof(qrCode));
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            var area = BoundingBox.InflatedRetangle(1F);
            float lado = Math.Min(area.Width, area.Height);
            var r = new RectangleF(area.X + (area.Width - lado) / 2F, area.Y + (area.Height - lado) / 2F, lado, lado);
            gfx.ShowXObject(_qrCode, r);
        }
    }
}
