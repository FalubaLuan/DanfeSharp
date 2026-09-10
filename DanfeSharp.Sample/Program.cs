using DanfeSharp;
using DanfeSharp.Modelo;

var xmlPath = @"C:\Users\luan\Documents\1 - Desenvolvimento Aplicativos\4 - Bibliotecas\DanfeSharp\DanfeSharp.Sample\XmlPath\35260854651716001150550000084651681155254327.xml";

var model = DanfeViewModelCreator.CriarDeArquivoXml(xmlPath);
using var danfe = new Danfe(model);
danfe.Gerar();
danfe.Salvar(Path.Combine(Path.GetDirectoryName(xmlPath), "pdf.pdf"));