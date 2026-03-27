using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CupomNaMao.Models;

namespace CupomNaMao.Services
{
    /// <summary>
    /// Serviço responsável por ler, validar e extrair dados de arquivos XML de NFC-e.
    /// </summary>
    public class XmlReaderService
    {
        /// <summary>
        /// Lê um arquivo XML de NFC-e e retorna o modelo preenchido.
        /// </summary>
        public NfceModel LerXml(string caminhoArquivo)
        {
            var model = new NfceModel
            {
                CaminhoArquivo = caminhoArquivo
            };

            try
            {
                if (!File.Exists(caminhoArquivo))
                    throw new FileNotFoundException("Arquivo XML não encontrado.", caminhoArquivo);

                var doc = XDocument.Load(caminhoArquivo);
                
                // Buscar infNFe de forma robusta ignorando namespaces (LocalName)
                var infNfe = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe");

                if (infNfe == null)
                    throw new InvalidDataException("Não é um XML de NF-e/NFC-e válido. Elemento 'infNFe' não encontrado.");

                // Identificar o namespace atual do elemento infNFe para as buscas seguintes
                XNamespace ns = infNfe.Name.Namespace;

                // ── Chave de acesso ───────────────────────────────────────────
                var idAttr = infNfe.Attribute("Id")?.Value ?? string.Empty;
                model.ChaveAcesso = idAttr.StartsWith("NFe") ? idAttr.Substring(3) : idAttr;

                // ── Identificação (ide) ────────────────────────────────────────
                var ide = ObterElemento(infNfe, "ide");
                if (ide != null)
                {
                    model.NumeroNota = ObterTexto(ide, "nNF");
                    model.SerieNota = ObterTexto(ide, "serie");
                    model.NaturezaOperacao = ObterTexto(ide, "natOp");
                    model.TipoEmissao = int.TryParse(ObterTexto(ide, "tpEmis"), out var te) ? te : 1;
                    var tpAmb = ObterTexto(ide, "tpAmb");
                    model.Ambiente = tpAmb == "1" ? "Produção" : "Homologação";

                    var dhEmi = ObterTexto(ide, "dhEmi");
                    if (DateTime.TryParse(dhEmi, out var dtEmi))
                        model.DataEmissao = dtEmi;

                    var mod = ObterTexto(ide, "mod");
                    if (mod != "65" && !string.IsNullOrEmpty(mod))
                    {
                        // Apenas aviso, mas continua o processamento
                        model.ErroValidacao = $"Aviso: Modelo {mod} detectado (esperado 65).";
                    }
                }

                // ── Emitente (emit) ───────────────────────────────────────────
                var emit = ObterElemento(infNfe, "emit");
                if (emit != null)
                {
                    model.NomeEmitente = ObterTexto(emit, "xNome");
                    model.CnpjEmitente = ObterTexto(emit, "CNPJ");
                    model.IeEmitente = ObterTexto(emit, "IE");

                    var enderEmit = ObterElemento(emit, "enderEmit");
                    if (enderEmit != null)
                    {
                        var logradouro = ObterTexto(enderEmit, "xLgr");
                        var numero = ObterTexto(enderEmit, "nro");
                        var complemento = ObterTexto(enderEmit, "xCpl");
                        var bairro = ObterTexto(enderEmit, "xBairro");
                        model.MunicipioEmitente = ObterTexto(enderEmit, "xMun");
                        model.UfEmitente = ObterTexto(enderEmit, "UF");
                        model.CepEmitente = ObterTexto(enderEmit, "CEP");
                        model.TelefoneEmitente = ObterTexto(emit, "fone");

                        model.EnderecoEmitente = $"{logradouro}, {numero}" +
                            (string.IsNullOrWhiteSpace(complemento) ? "" : $" - {complemento}") +
                            $" - {bairro}";
                    }
                }

                // ── Produtos (det) ────────────────────────────────────────────
                var dets = infNfe.Elements().Where(x => x.Name.LocalName == "det");
                foreach (var det in dets)
                {
                    var prod = ObterElemento(det, "prod");
                    if (prod == null) continue;

                    var item = new ProdutoModel
                    {
                        NumeroItem = int.TryParse(det.Attribute("nItem")?.Value, out var ni) ? ni : 0,
                        Codigo = ObterTexto(prod, "cProd"),
                        Descricao = ObterTexto(prod, "xProd"),
                        Ncm = ObterTexto(prod, "NCM"),
                        Cfop = ObterTexto(prod, "CFOP"),
                        Unidade = ObterTexto(prod, "uCom"),
                        Quantidade = ParseDecimal(ObterTexto(prod, "qCom")),
                        ValorUnitario = ParseDecimal(ObterTexto(prod, "vUnCom")),
                        ValorTotal = ParseDecimal(ObterTexto(prod, "vProd")),
                        Desconto = ParseDecimal(ObterTexto(prod, "vDesc")),
                        Acrescimo = ParseDecimal(ObterTexto(prod, "vOutro"))
                    };

                    model.Produtos.Add(item);
                }

                // ── Totais (total) ────────────────────────────────────────────
                var total = ObterElemento(infNfe, "total");
                if (total != null)
                {
                    var icmsTot = ObterElemento(total, "ICMSTot");
                    if (icmsTot != null)
                    {
                        model.TotalProdutos = ParseDecimal(ObterTexto(icmsTot, "vProd"));
                        model.TotalDesconto = ParseDecimal(ObterTexto(icmsTot, "vDesc"));
                        model.TotalNota = ParseDecimal(ObterTexto(icmsTot, "vNF"));
                        model.TotalIcms = ParseDecimal(ObterTexto(icmsTot, "vICMS"));
                        model.TotalPis = ParseDecimal(ObterTexto(icmsTot, "vPIS"));
                        model.TotalCofins = ParseDecimal(ObterTexto(icmsTot, "vCOFINS"));
                        model.TotalTributos = ParseDecimal(ObterTexto(icmsTot, "vTotTrib"));
                    }
                }

                // ── Pagamentos (pag) ──────────────────────────────────────────
                var pag = ObterElemento(infNfe, "pag");
                if (pag != null)
                {
                    var detPags = pag.Elements().Where(x => x.Name.LocalName == "detPag");
                    foreach (var detPag in detPags)
                    {
                        model.Pagamentos.Add(new PagamentoModel
                        {
                            TipoPagamento = ObterTexto(detPag, "tPag"),
                            Valor = ParseDecimal(ObterTexto(detPag, "vPag"))
                        });
                    }
                    model.TotalPago = model.Pagamentos.Sum(p => p.Valor);
                    model.Troco = ParseDecimal(ObterTexto(pag, "vTroco"));
                    if (model.Troco == 0) model.Troco = Math.Max(0, model.TotalPago - model.TotalNota);
                }

                // ── QR Code e URL de consulta (infNFeSupl) ─────────────────────
                // infNFeSupl costuma ser irmão de infNFe dentro de NFe
                var infNfeSupl = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFeSupl");
                if (infNfeSupl != null)
                {
                    model.UrlQrCode = ObterTexto(infNfeSupl, "qrCode");
                    model.UrlConsulta = ObterTexto(infNfeSupl, "urlChave");
                }

                // ── Protocolo de autorização (protNFe) ─────────────────────────
                var infProt = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infProt");
                if (infProt != null)
                {
                    model.NumeroProtocolo = ObterTexto(infProt, "nProt");
                    var dhRecbto = ObterTexto(infProt, "dhRecbto");
                    if (DateTime.TryParse(dhRecbto, out var dtAut))
                        model.DataAutorizacao = dtAut;
                    
                    if (string.IsNullOrEmpty(model.ChaveAcesso))
                        model.ChaveAcesso = ObterTexto(infProt, "chNFe");
                }

                model.Valida = true;
            }
            catch (Exception ex)
            {
                model.Valida = false;
                model.ErroValidacao = ex.Message;
            }

            return model;
        }

        public List<NfceModel> LerMultiplosXml(IEnumerable<string> caminhos)
        {
            var resultado = new List<NfceModel>();
            foreach (var caminho in caminhos)
                resultado.Add(LerXml(caminho));
            return resultado;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static XElement? ObterElemento(XElement parent, string localName)
        {
            return parent.Elements().FirstOrDefault(x => x.Name.LocalName == localName);
        }

        private static string ObterTexto(XElement parent, string localName)
        {
            return ObterElemento(parent, localName)?.Value ?? string.Empty;
        }

        private static decimal ParseDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;
            return decimal.TryParse(valor,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result) ? result : 0m;
        }
    }
}
