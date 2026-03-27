using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using CupomNaMao.Models;

namespace CupomNaMao.Services
{
    /// <summary>
    /// Serviço de alto nível para operações com PDF: salvar, abrir, imprimir, lote e ZIP.
    /// </summary>
    public class PdfService
    {
        private readonly DanfeGeneratorService _danfeGenerator;

        public PdfService(DanfeGeneratorService danfeGenerator)
        {
            _danfeGenerator = danfeGenerator;
        }

        /// <summary>
        /// Gera e salva o PDF do DANFE para um único modelo.
        /// </summary>
        public string GerarESalvar(NfceModel model, string pastaDestino)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(pastaDestino))
                pastaDestino = Path.GetDirectoryName(model.CaminhoArquivo) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            Directory.CreateDirectory(pastaDestino);

            var nomePdf = model.SugestaoPdfNome;
            var caminhoPdf = Path.Combine(pastaDestino, nomePdf);

            // Evitar sobrescrever arquivo existente
            int contador = 1;
            while (File.Exists(caminhoPdf))
            {
                var semExt = Path.GetFileNameWithoutExtension(nomePdf);
                caminhoPdf = Path.Combine(pastaDestino, $"{semExt}_{contador++}.pdf");
            }

            _danfeGenerator.GerarPdf(model, caminhoPdf);
            return caminhoPdf;
        }

        /// <summary>
        /// Gera e salva o PDF em um caminho específico.
        /// </summary>
        public void GerarESalvarEm(NfceModel model, string caminhoCompleto)
        {
            _danfeGenerator.GerarPdf(model, caminhoCompleto);
        }

        /// <summary>
        /// Abre o PDF no visualizador padrão do sistema.
        /// </summary>
        public void AbrirPdf(string caminhoPdf)
        {
            if (!File.Exists(caminhoPdf))
                throw new FileNotFoundException("Arquivo PDF não encontrado.", caminhoPdf);

            Process.Start(new ProcessStartInfo
            {
                FileName = caminhoPdf,
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Imprime o PDF usando o visualizador padrão do sistema.
        /// </summary>
        public void ImprimirPdf(string caminhoPdf)
        {
            if (!File.Exists(caminhoPdf))
                throw new FileNotFoundException("Arquivo PDF não encontrado para impressão.", caminhoPdf);

            Process.Start(new ProcessStartInfo
            {
                FileName = caminhoPdf,
                UseShellExecute = true,
                Verb = "print"
            });
        }

        /// <summary>
        /// Processa múltiplos modelos em lote, gerando um PDF para cada um.
        /// Retorna lista de caminhos gerados e erros.
        /// </summary>
        public LoteResultado ProcessarLote(IEnumerable<NfceModel> modelos, string pastaDestino,
            Action<int, int, string>? progressCallback = null)
        {
            var resultado = new LoteResultado();
            var lista = modelos.ToList();
            int total = lista.Count;

            for (int i = 0; i < total; i++)
            {
                var model = lista[i];
                progressCallback?.Invoke(i + 1, total, model.NomeArquivo);

                try
                {
                    var caminho = GerarESalvar(model, pastaDestino);
                    resultado.Sucesso.Add(caminho);
                }
                catch (Exception ex)
                {
                    resultado.Erros.Add(new LoteErro
                    {
                        Arquivo = model.CaminhoArquivo,
                        Mensagem = ex.Message
                    });
                }
            }

            return resultado;
        }

        /// <summary>
        /// Gera múltiplos PDFs e os compacta em um arquivo ZIP.
        /// </summary>
        public string GerarZip(IEnumerable<NfceModel> modelos, string caminhoZip,
            Action<int, int, string>? progressCallback = null)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"CupomNaMao_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var resultado = ProcessarLote(modelos, tempDir, progressCallback);

                if (File.Exists(caminhoZip))
                    File.Delete(caminhoZip);

                ZipFile.CreateFromDirectory(tempDir, caminhoZip);
                return caminhoZip;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        /// <summary>
        /// Abre a página oficial da SEFAZ estadual com base na UF da chave de acesso.
        /// </summary>
        public void AbrirConsultaSefaz(NfceModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            string chave = NormalizarChaveAcesso(model.ChaveAcesso);
            string codigoUf = chave.Substring(0, 2);

            if (!PortaisSefazPorCodigoUf.TryGetValue(codigoUf, out var urlBase))
                throw new InvalidOperationException($"UF não suportada para consulta SEFAZ. Código: {codigoUf}.");

            var separador = urlBase.Contains('?') ? "&" : "?";
            string url = $"{urlBase}{separador}chNFe={chave}";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private static string NormalizarChaveAcesso(string chaveAcesso)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso))
                throw new InvalidOperationException("A chave de acesso da nota está vazia.");

            var somenteDigitos = Regex.Replace(chaveAcesso, "[^0-9]", string.Empty);

            if (somenteDigitos.Length != 44)
                throw new InvalidOperationException("A chave de acesso deve conter 44 dígitos.");

            return somenteDigitos;
        }

        private static readonly Dictionary<string, string> PortaisSefazPorCodigoUf = new()
        {
            ["11"] = "https://www.sefin.ro.gov.br/consultanfce/consulta.jsp",
            ["12"] = "https://www.sefaznet.ac.gov.br/nfce/consulta",
            ["13"] = "https://sistemas.sefaz.am.gov.br/nfceweb/consultarNFCe.jsp",
            ["14"] = "https://www.sefaz.rr.gov.br/nfce/servlet/wp_consulta_nfce",
            ["15"] = "https://appnfc.sefa.pa.gov.br/portal/view/consultas/nfce/nfceForm.seam",
            ["16"] = "https://www.sefaz.ap.gov.br/nfce/consulta",
            ["17"] = "https://www.sefaz.to.gov.br/nfce/consulta",
            ["21"] = "https://www.sefaz.ma.gov.br/nfce/consulta",
            ["22"] = "https://www.sefaz.pi.gov.br/nfce/consulta",
            ["23"] = "https://nfce.sefaz.ce.gov.br/pages/consultaNota.jsf",
            ["24"] = "https://nfce.set.rn.gov.br/consultarNFCe.aspx",
            ["25"] = "https://www.sefaz.pb.gov.br/nfce/consulta",
            ["26"] = "https://www.sefaz.pe.gov.br/nfce/consulta",
            ["27"] = "https://www.sefaz.al.gov.br/nfce/consulta",
            ["28"] = "https://www.sefaz.se.gov.br/nfce/consulta",
            ["29"] = "https://nfe.sefaz.ba.gov.br/servicos/nfce/default.aspx",
            ["31"] = "https://portalsped.fazenda.mg.gov.br/spedmg/nfce",
            ["32"] = "https://app.sefaz.es.gov.br/ConsultaNFCe",
            ["33"] = "https://www4.fazenda.rj.gov.br/consultaNFCe/QRCode",
            ["35"] = "https://www.nfce.fazenda.sp.gov.br/consulta",
            ["41"] = "https://www.fazenda.pr.gov.br/nfce/qrcode",
            ["42"] = "https://sat.sef.sc.gov.br/tax.NET/Sat.Dfe.Nfe.Web/Consultas",
            ["43"] = "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx",
            ["50"] = "https://www.dfe.ms.gov.br/nfce",
            ["51"] = "https://www.sefaz.mt.gov.br/nfce/consultanfce",
            ["52"] = "https://www.sefaz.go.gov.br/nfce/consulta",
            ["53"] = "https://www.fazenda.df.gov.br/aplicacoes/nfce/consulta"
        };
    }

    public class LoteResultado
    {
        public List<string> Sucesso { get; } = new();
        public List<LoteErro> Erros { get; } = new();
        public int Total => Sucesso.Count + Erros.Count;
        public bool TodosSucesso => Erros.Count == 0;
    }

    public class LoteErro
    {
        public string Arquivo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
    }
}
