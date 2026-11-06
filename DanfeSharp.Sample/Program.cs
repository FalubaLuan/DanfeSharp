using DanfeSharp;
using DanfeSharp.Modelo;

var xmlPath = @"C:\Users\luan\Downloads\31260905955933000156550010000064141046403276.xml";

var model = DanfeViewModelCreator.CriarDeArquivoXml(xmlPath);
model.IsCancelled = true;

using var danfe = new Danfe(model);
danfe.Gerar();
danfe.Salvar(Path.Combine(Path.GetDirectoryName(xmlPath), "pdf.pdf"));