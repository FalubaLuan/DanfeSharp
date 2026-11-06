using System;
using System.IO;
using System.Text;
using DanfeSharp.Modelo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.pdfclown.tools;
using PdfFile = org.pdfclown.files.File;

namespace DanfeSharp.Test
{
    /// <summary>
    /// Testes do DANFE no leiaute da NT 2026.010 (Reforma Tributária — IBS/CBS/IS),
    /// gerado por <see cref="DanfeReformaTributaria"/>.
    /// </summary>
    [TestClass]
    public class DanfeReformaTributariaTests
    {
        private const string XmlBase = "Xml/NFe/v4.00/v4_DanfeCompleto.xml";
        private readonly string OutputDirectory = Path.Combine("Output", "ReformaTributaria");

        public DanfeReformaTributariaTests()
        {
            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }

        // Injeta grupos IBSCBS/IS no primeiro item e IBSCBSTot/ISTot no total do XML de exemplo.
        private static string XmlComReforma(bool comGRed = false)
        {
            var xml = File.ReadAllText(XmlBase);

            var gred = comGRed ? "<gRed><pRedAliq>50</pRedAliq><pAliqEfet>0.0500</pAliqEfet></gRed>" : "";
            var item =
                "<IBSCBS><CST>000</CST><cClassTrib>000001</cClassTrib><gIBSCBS><vBC>100.00</vBC>" +
                "<gIBSUF><pIBSUF>0.1000</pIBSUF>" + gred + "<vIBSUF>0.10</vIBSUF></gIBSUF>" +
                "<gIBSMun><pIBSMun>0.0000</pIBSMun><vIBSMun>0.00</vIBSMun></gIBSMun>" +
                "<vIBS>0.10</vIBS><gCBS><pCBS>0.9000</pCBS><vCBS>0.90</vCBS></gCBS></gIBSCBS></IBSCBS>" +
                "<IS><CSTIS>000</CSTIS><cClassTribIS>000001</cClassTribIS><vBCIS>100.00</vBCIS><pIS>2.0000</pIS><vIS>2.00</vIS></IS>";

            var i = xml.IndexOf("</imposto>", StringComparison.Ordinal);
            xml = xml.Insert(i, item);

            var totais =
                "<ISTot><vIS>2.00</vIS></ISTot>" +
                "<IBSCBSTot><vBCIBSCBS>100.00</vBCIBSCBS><gIBS><gIBSUF><vIBSUF>0.10</vIBSUF></gIBSUF>" +
                "<gIBSMun><vIBSMun>0.00</vIBSMun></gIBSMun><vIBS>0.10</vIBS></gIBS><gCBS><vCBS>0.90</vCBS></gCBS></IBSCBSTot>";
            var t = xml.IndexOf("</ICMSTot>", StringComparison.Ordinal) + "</ICMSTot>".Length;
            return xml.Insert(t, totais);
        }

        private static string ExtrairTexto(string pdfPath)
        {
            var sb = new StringBuilder();
            using (var file = new PdfFile(pdfPath))
            {
                var extractor = new TextExtractor();
                foreach (var page in file.Document.Pages)
                    foreach (var pair in extractor.Extract(page))
                        foreach (var s in pair.Value)
                            sb.AppendLine(s.Text);
            }
            return sb.ToString();
        }

        private string Gerar(string xml, string nome, Action<DanfeViewModel> ajustar = null)
        {
            var model = DanfeViewModelCreator.CriarDeStringXml(xml);
            ajustar?.Invoke(model);
            var reforma = ReformaTributariaViewModelCreator.CriarDeStringXml(xml);
            var path = Path.Combine(OutputDirectory, nome);

            using (var danfe = new DanfeReformaTributaria(model, reforma))
            {
                danfe.Gerar();
                danfe.Salvar(path);
            }

            return path;
        }

        [TestMethod]
        public void Creator_LeCamposIbsCbsIs()
        {
            var r = ReformaTributariaViewModelCreator.CriarDeStringXml(XmlComReforma());
            var item = r.Itens[0];

            Assert.AreEqual("000001", item.ClassificacaoTributaria);
            Assert.AreEqual(100.0, item.BaseIbsCbs);
            Assert.AreEqual(0.1, item.AliquotaIbsUf);
            Assert.AreEqual(0.9, item.AliquotaCbs);
            Assert.AreEqual(2.0, item.ValorIs);
            Assert.AreEqual(0.9, r.Totais.ValorCbs);
            Assert.AreEqual(0.1, r.Totais.ValorIbsUf);
            Assert.AreEqual(2.0, r.Totais.ValorIs);
            Assert.IsNull(r.Itens[1].BaseIbsCbs, "Itens sem o grupo não devem ter dados inventados.");
        }

        [TestMethod]
        public void Creator_UsaAliquotaEfetivaQuandoHaGRed()
        {
            var r = ReformaTributariaViewModelCreator.CriarDeStringXml(XmlComReforma(comGRed: true));
            Assert.AreEqual(0.05, r.Itens[0].AliquotaIbsUf);
        }

        [TestMethod]
        public void Gerar_ContemBlocosETitulosDaNT()
        {
            var texto = ExtrairTexto(Gerar(XmlComReforma(), "ReformaTributaria.pdf"));

            StringAssert.Contains(texto, "TOTAL DO IBS / CBS / IS");
            StringAssert.Contains(texto, "TOTAL DO ICMS / IPI");
            StringAssert.Contains(texto, "CÓDIGO DO REGIME TRIBUTÁRIO");
            StringAssert.Contains(texto, "TIPO DE REGIME DE APURAÇÃO DO IBS E DA CBS");
            StringAssert.Contains(texto, "BASES DE CÁLCULO");
            StringAssert.Contains(texto, "VALOR DOS TRIBUTOS");
            StringAssert.Contains(texto, "cClassTrib 000001");
        }

        [TestMethod]
        public void Gerar_Homologacao_ContemMarcaDagua()
        {
            var texto = ExtrairTexto(Gerar(XmlComReforma(), "ReformaTributaria_Homologacao.pdf", m => m.TipoAmbiente = 2));
            StringAssert.Contains(texto, "SEM VALOR FISCAL");
        }

        [TestMethod]
        public void Gerar_Paisagem_NaoLanca()
        {
            Gerar(XmlComReforma(), "ReformaTributaria_Paisagem.pdf", m => m.Orientacao = Orientacao.Paisagem);
        }

        [TestMethod]
        public void Gerar_ComQrCode_NaoLanca()
        {
            Gerar(XmlComReforma(), "ReformaTributaria_QrCode.pdf",
                m => m.QrCode = "https://www.nfe.fazenda.gov.br/portal/consultaRecaptcha.aspx?tipoConteudo=XbSeqxE8pl8=&nfe=35260607952851000109550010001234561000123459");
        }

        [TestMethod]
        public void Gerar_SemGruposDaReforma_NaoLanca()
        {
            Gerar(File.ReadAllText(XmlBase), "ReformaTributaria_SemGrupos.pdf");
        }
    }
}
