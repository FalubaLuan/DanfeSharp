using DanfeSharp.Esquemas.NFe;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DanfeSharp.Modelo
{
    /// <summary>
    /// Extrai de um XML de NF-e processada os campos da NT 2026.010 que não existem
    /// em <see cref="DanfeViewModelCreator"/>. Use junto com ele sobre o MESMO xml.
    /// </summary>
    public static class ReformaTributariaViewModelCreator
    {
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ProcNFe));

        public static ReformaTributariaViewModel CriarDeStringXml(string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));

            using (var reader = new StringReader(xml))
            {
                return CriarDeProcNFe((ProcNFe)Serializer.Deserialize(reader));
            }
        }

        public static ReformaTributariaViewModel CriarDeArquivoXml(string caminho)
        {
            using (var sr = new StreamReader(caminho, true))
            {
                return CriarDeProcNFe((ProcNFe)Serializer.Deserialize(sr));
            }
        }

        public static ReformaTributariaViewModel CriarDeProcNFe(ProcNFe procNfe)
        {
            if (procNfe == null) throw new ArgumentNullException(nameof(procNfe));

            var infNfe = procNfe.NFe.infNFe;
            var model = new ReformaTributariaViewModel { Crt = infNfe.emit?.CRT, QrCode = procNfe.NFe.infNFeSupl?.qrCode };

            foreach (var det in infNfe.det)
                model.Itens.Add(CriarItem(det.imposto, det.imposto?.IPI?.IPITrib));

            var total = infNfe.total;
            var t = model.Totais;

            var icms = total?.ICMSTot;
            if (icms != null)
            {
                t.ValorFcpSt = icms.vFCPST;
                t.ValorFcpUfDest = icms.vFCPUFDest;
                t.QuantidadeBcIcmsMono = icms.qBCMono;
                t.ValorIcmsMono = icms.vICMSMono;
                t.QuantidadeBcIcmsMonoReten = icms.qBCMonoReten;
                t.ValorIcmsMonoReten = icms.vICMSMonoReten;
            }

            var ibsCbs = total?.IBSCBSTot;
            if (ibsCbs != null)
            {
                t.ValorCbs = ibsCbs.gCBS?.vCBS;
                t.ValorIbsUf = ibsCbs.gIBS?.gIBSUF?.vIBSUF;
                t.ValorIbsMun = ibsCbs.gIBS?.gIBSMun?.vIBSMun;
                t.ValorIbsMono = ibsCbs.gMono?.vIBSMono;
                t.ValorCbsMono = ibsCbs.gMono?.vCBSMono;
                t.ValorIbsMonoReten = ibsCbs.gMono?.vIBSMonoReten;
                t.ValorCbsMonoReten = ibsCbs.gMono?.vCBSMonoReten;
            }

            t.ValorIs = total?.ISTot?.vIS;

            return model;
        }

        private static ItemReformaTributariaViewModel CriarItem(ProdutoImposto imposto, IPITrib ipi)
        {
            var item = new ItemReformaTributariaViewModel { BaseIpi = ipi?.vBC };
            if (imposto == null) return item;

            var ibscbs = imposto.IBSCBS;
            if (ibscbs != null)
            {
                item.ClassificacaoTributaria = ibscbs.cClassTrib;

                var g = ibscbs.gIBSCBS;
                if (g != null)
                {
                    item.BaseIbsCbs = g.vBC;

                    // Regra da NT (4.3): com gRed informado exibe-se a alíquota efetiva;
                    // sem ele, a alíquota vigente.
                    item.AliquotaIbsUf = g.gIBSUF?.gRed?.pAliqEfet ?? g.gIBSUF?.pIBSUF;
                    item.ValorIbsUf = g.gIBSUF?.vIBSUF;
                    item.AliquotaIbsMun = g.gIBSMun?.gRed?.pAliqEfet ?? g.gIBSMun?.pIBSMun;
                    item.ValorIbsMun = g.gIBSMun?.vIBSMun;
                    item.AliquotaCbs = g.gCBS?.gRed?.pAliqEfet ?? g.gCBS?.pCBS;
                    item.ValorCbs = g.gCBS?.vCBS;
                }
            }

            var seletivo = imposto.ImpostoSeletivo;
            if (seletivo != null)
            {
                item.BaseIs = seletivo.vBCIS;
                item.AliquotaIs = seletivo.pIS;
                item.ValorIs = seletivo.vIS;

                // IS sem IBS/CBS: ainda assim identifica a classificação do item.
                if (string.IsNullOrWhiteSpace(item.ClassificacaoTributaria))
                    item.ClassificacaoTributaria = seletivo.cClassTribIS;
            }

            return item;
        }
    }
}
