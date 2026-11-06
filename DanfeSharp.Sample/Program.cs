using DanfeSharp;
using DanfeSharp.Modelo;

var xmlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "XmlPath", "NFe_NT2026010_Facultativos_Ficticia.xml");;
var xml = File.ReadAllText(xmlPath);
var model = DanfeViewModelCreator.CriarDeStringXml(xml);
var reforma = ReformaTributariaViewModelCreator.CriarDeStringXml(xml);

using var danfe = new DanfeReformaTributaria(model, reforma);
danfe.Gerar();
danfe.Salvar("saida.pdf");