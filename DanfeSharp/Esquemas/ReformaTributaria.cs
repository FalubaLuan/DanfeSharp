using System;
using System.Xml.Serialization;

namespace DanfeSharp.Esquemas.NFe
{
    // NT 2026.010 — DANFE Reforma Tributária.
    // Extensões do schema da NF-e (grupos UB/IS/W do leiaute com IBS/CBS/IS).
    // Tudo aqui é aditivo: as classes existentes (ProdutoImposto, Total, ICMSTotal)
    // são estendidas via "partial" para não alterar ProcNFe.cs.

    #region Item (det/imposto)

    public partial class ProdutoImposto
    {
        /// <summary>Grupo UB — IBS/CBS do item.</summary>
        public RtIbsCbsItem IBSCBS { get; set; }

        /// <summary>Grupo UB — Imposto Seletivo do item.</summary>
        [XmlElement("IS")]
        public RtIsItem ImpostoSeletivo { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtIbsCbsItem
    {
        public string CST { get; set; }
        /// <summary>cClassTrib — UB14</summary>
        public string cClassTrib { get; set; }
        public RtGIbsCbsItem gIBSCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGIbsCbsItem
    {
        /// <summary>vBC — UB16</summary>
        public double? vBC { get; set; }
        public RtGIbsUfItem gIBSUF { get; set; }
        public RtGIbsMunItem gIBSMun { get; set; }
        public RtGCbsItem gCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGRed
    {
        public double? pRedAliq { get; set; }
        /// <summary>Alíquota efetiva (pAliqEfet) — impressa quando o grupo gRed existe.</summary>
        public double? pAliqEfet { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGIbsUfItem
    {
        public double? pIBSUF { get; set; }
        public RtGRed gRed { get; set; }
        public double? vIBSUF { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGIbsMunItem
    {
        public double? pIBSMun { get; set; }
        public RtGRed gRed { get; set; }
        public double? vIBSMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGCbsItem
    {
        public double? pCBS { get; set; }
        public RtGRed gRed { get; set; }
        public double? vCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtIsItem
    {
        public string CSTIS { get; set; }
        public string cClassTribIS { get; set; }
        /// <summary>vBCIS — UB05</summary>
        public double? vBCIS { get; set; }
        /// <summary>pIS — UB06</summary>
        public double? pIS { get; set; }
        /// <summary>vIS — UB11</summary>
        public double? vIS { get; set; }
    }

    #endregion

    #region Totais (total/IBSCBSTot, total/ISTot, total/ICMSTot)

    public partial class Total
    {
        public RtIbsCbsTot IBSCBSTot { get; set; }
        public RtIsTot ISTot { get; set; }
    }

    public partial class ICMSTotal
    {
        public double? vFCPUFDest { get; set; }
        public double? vFCPST { get; set; }
        public double? qBCMono { get; set; }
        public double? vICMSMono { get; set; }
        public double? qBCMonoReten { get; set; }
        public double? vICMSMonoReten { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtIbsCbsTot
    {
        public double? vBCIBSCBS { get; set; }
        public RtGIbsTot gIBS { get; set; }
        public RtGCbsTot gCBS { get; set; }
        public RtGMonoTot gMono { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGIbsTot
    {
        public RtGIbsUfTot gIBSUF { get; set; }
        public RtGIbsMunTot gIBSMun { get; set; }
        public double? vIBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGIbsUfTot
    {
        public double? vIBSUF { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGIbsMunTot
    {
        public double? vIBSMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGCbsTot
    {
        public double? vCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtGMonoTot
    {
        public double? vIBSMono { get; set; }
        public double? vCBSMono { get; set; }
        public double? vIBSMonoReten { get; set; }
        public double? vCBSMonoReten { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFe)]
    public class RtIsTot
    {
        public double? vIS { get; set; }
    }

    #endregion
}
