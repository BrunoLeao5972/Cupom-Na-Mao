using System;
using System.IO;
using CupomNaMao.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CupomNaMao.Services
{
    /// <summary>
    /// Serviço responsável pela geração do DANFE NFC-e em PDF no formato cupom 80mm.
    /// </summary>
    public class DanfeGeneratorService
    {
        private readonly QrCodeService _qrCodeService;

        // Dimensões do cupom 80mm em pontos (1 pt = 1/72 polegada)
        // Largura: 80mm = 226.77pt, usamos 226
        private const double LarguraPt = 226.0;
        private const double MargemLateral = 8.0;
        private const double LarguraUtil = LarguraPt - (MargemLateral * 2);

        // Fontes
        private const string FonteNome = "Arial";

        public DanfeGeneratorService(QrCodeService qrCodeService)
        {
            _qrCodeService = qrCodeService;
        }

        /// <summary>
        /// Gera o DANFE NFC-e em PDF e salva no caminho especificado.
        /// </summary>
        public void GerarPdf(NfceModel model, string caminhoDestino)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(caminhoDestino))
                throw new ArgumentException("Caminho de destino inválido.", nameof(caminhoDestino));

            // Garantir que o diretório existe
            var dir = Path.GetDirectoryName(caminhoDestino);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            // Calcular altura dinâmica do documento
            double alturaTotal = CalcularAltura(model);

            using var document = new PdfDocument();
            document.Info.Title = $"DANFE NFC-e - {model.NomeEmitente}";
            document.Info.Subject = $"NFC-e {model.NumeroNota} - {model.DataEmissao:dd/MM/yyyy}";
            document.Info.Author = "Cupom Na Mão";

            // Criar página com dimensões de cupom
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(LarguraPt);
            page.Height = XUnit.FromPoint(alturaTotal);

            using var gfx = XGraphics.FromPdfPage(page);

            double y = MargemLateral;

            // ── Cabeçalho do emitente ─────────────────────────────────────────
            y = DesenharCabecalho(gfx, model, y);

            // ── Linha separadora ──────────────────────────────────────────────
            y = DesenharLinha(gfx, y);

            // ── Título DANFE ──────────────────────────────────────────────────
            y = DesenharTitulo(gfx, y);

            // ── Linha separadora ──────────────────────────────────────────────
            y = DesenharLinha(gfx, y);

            // ── Cabeçalho da tabela de produtos ───────────────────────────────
            y = DesenharCabecalhoProdutos(gfx, y);

            // ── Lista de produtos ─────────────────────────────────────────────
            y = DesenharProdutos(gfx, model, y);

            // ── Linha separadora ──────────────────────────────────────────────
            y = DesenharLinha(gfx, y);

            // ── Totais ────────────────────────────────────────────────────────
            y = DesenharTotais(gfx, model, y);

            // ── Linha separadora ──────────────────────────────────────────────
            y = DesenharLinha(gfx, y);

            // ── Pagamentos ────────────────────────────────────────────────────
            y = DesenharPagamentos(gfx, model, y);

            // ── Linha separadora ──────────────────────────────────────────────
            y = DesenharLinha(gfx, y);

            // ── Informações da NFC-e ──────────────────────────────────────────
            y = DesenharInfoNfce(gfx, model, y);

            // ── QR Code ───────────────────────────────────────────────────────
            y = DesenharQrCode(gfx, model, y);

            // ── Chave de acesso ───────────────────────────────────────────────
            y = DesenharChaveAcesso(gfx, model, y);

            // ── Protocolo ─────────────────────────────────────────────────────
            y = DesenharProtocolo(gfx, model, y);

            // ── Informações adicionais ────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(model.InformacoesAdicionais))
                y = DesenharInformacoesAdicionais(gfx, model, y);

            // ── Ambiente (se homologação) ─────────────────────────────────────
            if (model.Ambiente == "Homologação")
                DesenharMarcaHomologacao(gfx, page);

            document.Save(caminhoDestino);
        }

        /// <summary>
        /// Gera o DANFE e retorna como MemoryStream.
        /// </summary>
        public MemoryStream GerarPdfStream(NfceModel model)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"danfe_{Guid.NewGuid():N}.pdf");
            GerarPdf(model, tempPath);
            var bytes = File.ReadAllBytes(tempPath);
            File.Delete(tempPath);
            return new MemoryStream(bytes);
        }

        // ── Seções do DANFE ───────────────────────────────────────────────────

        private double DesenharCabecalho(XGraphics gfx, NfceModel model, double y)
        {
            var fonteTitulo = new XFont(FonteNome, 9, XFontStyleEx.Bold);
            var fonteNormal = new XFont(FonteNome, 7.5, XFontStyleEx.Regular);
            var fontePequena = new XFont(FonteNome, 7, XFontStyleEx.Regular);

            // Nome do emitente (centralizado, negrito)
            gfx.DrawString(model.NomeEmitente.ToUpper(), fonteTitulo, XBrushes.Black,
                new XRect(MargemLateral, y, LarguraUtil, 14), XStringFormats.TopCenter);
            y += 14;

            // CNPJ
            gfx.DrawString($"CNPJ: {FormatarCnpj(model.CnpjEmitente)}", fonteNormal, XBrushes.Black,
                new XRect(MargemLateral, y, LarguraUtil, 11), XStringFormats.TopCenter);
            y += 11;

            // Endereço
            if (!string.IsNullOrWhiteSpace(model.EnderecoEmitente))
            {
                gfx.DrawString(model.EnderecoEmitente, fontePequena, XBrushes.Black,
                    new XRect(MargemLateral, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 10;
            }

            // Município/UF/CEP
            if (!string.IsNullOrWhiteSpace(model.MunicipioEmitente))
            {
                gfx.DrawString($"{model.MunicipioEmitente} - {model.UfEmitente}  CEP: {FormatarCep(model.CepEmitente)}",
                    fontePequena, XBrushes.Black,
                    new XRect(MargemLateral, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 10;
            }

            // Telefone
            if (!string.IsNullOrWhiteSpace(model.TelefoneEmitente))
            {
                gfx.DrawString($"Tel: {model.TelefoneEmitente}", fontePequena, XBrushes.Black,
                    new XRect(MargemLateral, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 10;
            }

            y += 3;
            return y;
        }

        private double DesenharTitulo(XGraphics gfx, double y)
        {
            var fonte = new XFont(FonteNome, 8.5, XFontStyleEx.Bold);
            gfx.DrawString("DANFE NFC-e", fonte, XBrushes.Black,
                new XRect(MargemLateral, y, LarguraUtil, 13), XStringFormats.TopCenter);
            y += 13;
            gfx.DrawString("Documento Auxiliar da Nota Fiscal de Consumidor Eletrônica", 
                new XFont(FonteNome, 6.5, XFontStyleEx.Regular), XBrushes.Black,
                new XRect(MargemLateral, y, LarguraUtil, 10), XStringFormats.TopCenter);
            y += 12;
            return y;
        }

        private double DesenharCabecalhoProdutos(XGraphics gfx, double y)
        {
            var fonte = new XFont(FonteNome, 7, XFontStyleEx.Bold);
            var bg = new XSolidBrush(XColor.FromArgb(230, 230, 230));
            gfx.DrawRectangle(bg, MargemLateral, y, LarguraUtil, 11);

            double x = MargemLateral + 1;
            gfx.DrawString("#", fonte, XBrushes.Black, new XRect(x, y + 1, 12, 10), XStringFormats.TopLeft);
            gfx.DrawString("DESCRIÇÃO", fonte, XBrushes.Black, new XRect(x + 13, y + 1, 90, 10), XStringFormats.TopLeft);
            gfx.DrawString("QTD", fonte, XBrushes.Black, new XRect(x + 104, y + 1, 28, 10), XStringFormats.TopCenter);
            gfx.DrawString("VL.UN", fonte, XBrushes.Black, new XRect(x + 133, y + 1, 32, 10), XStringFormats.TopCenter);
            gfx.DrawString("TOTAL", fonte, XBrushes.Black, new XRect(x + 166, y + 1, 38, 10), XStringFormats.TopRight);
            y += 12;
            return y;
        }

        private double DesenharProdutos(XGraphics gfx, NfceModel model, double y)
        {
            var fonteDesc = new XFont(FonteNome, 7.5, XFontStyleEx.Regular);
            var fonteValor = new XFont(FonteNome, 7.5, XFontStyleEx.Regular);
            var fontePequena = new XFont(FonteNome, 6.5, XFontStyleEx.Regular);

            foreach (var prod in model.Produtos)
            {
                double x = MargemLateral + 1;

                // Número do item
                gfx.DrawString(prod.NumeroItem.ToString(), fontePequena, XBrushes.Black,
                    new XRect(x, y, 12, 10), XStringFormats.TopLeft);

                // Descrição (pode quebrar linha)
                var desc = prod.Descricao;
                if (desc.Length > 22)
                {
                    gfx.DrawString(desc.Substring(0, 22), fonteDesc, XBrushes.Black,
                        new XRect(x + 13, y, 90, 10), XStringFormats.TopLeft);
                    y += 10;
                    gfx.DrawString(desc.Substring(22), fontePequena, XBrushes.Black,
                        new XRect(x + 13, y, 90, 10), XStringFormats.TopLeft);
                }
                else
                {
                    gfx.DrawString(desc, fonteDesc, XBrushes.Black,
                        new XRect(x + 13, y, 90, 10), XStringFormats.TopLeft);
                }

                // Quantidade
                gfx.DrawString(prod.Quantidade.ToString("G29"), fonteValor, XBrushes.Black,
                    new XRect(x + 104, y, 28, 10), XStringFormats.TopCenter);

                // Valor unitário
                gfx.DrawString(prod.ValorUnitario.ToString("N2"), fonteValor, XBrushes.Black,
                    new XRect(x + 133, y, 32, 10), XStringFormats.TopCenter);

                // Valor total
                gfx.DrawString(prod.ValorTotal.ToString("N2"), fonteValor, XBrushes.Black,
                    new XRect(x + 166, y, 38, 10), XStringFormats.TopRight);

                y += 11;

                // Desconto por item
                if (prod.Desconto > 0)
                {
                    gfx.DrawString($"  Desconto: -R$ {prod.Desconto:N2}", fontePequena,
                        new XSolidBrush(XColor.FromArgb(180, 0, 0)),
                        new XRect(x + 13, y, LarguraUtil - 14, 9), XStringFormats.TopLeft);
                    y += 9;
                }
            }

            y += 2;
            return y;
        }

        private double DesenharTotais(XGraphics gfx, NfceModel model, double y)
        {
            var fonteLabel = new XFont(FonteNome, 7.5, XFontStyleEx.Regular);
            var fonteValor = new XFont(FonteNome, 7.5, XFontStyleEx.Bold);
            var fonteTotalGrande = new XFont(FonteNome, 9.5, XFontStyleEx.Bold);

            double x = MargemLateral;

            // Subtotal
            if (model.TotalDesconto > 0 || model.TotalAcrescimo > 0)
            {
                DesenharLinhaValor(gfx, fonteLabel, fonteValor, x, y, "Subtotal:", $"R$ {model.TotalProdutos:N2}");
                y += 11;

                if (model.TotalDesconto > 0)
                {
                    DesenharLinhaValor(gfx, fonteLabel, new XFont(FonteNome, 7.5, XFontStyleEx.Bold),
                        x, y, "Desconto:", $"-R$ {model.TotalDesconto:N2}",
                        labelBrush: XBrushes.Black,
                        valorBrush: new XSolidBrush(XColor.FromArgb(180, 0, 0)));
                    y += 11;
                }
            }

            // Total da nota (destaque)
            gfx.DrawString("TOTAL:", fonteTotalGrande, XBrushes.Black,
                new XRect(x, y, LarguraUtil / 2, 14), XStringFormats.TopLeft);
            gfx.DrawString($"R$ {model.TotalNota:N2}", fonteTotalGrande, XBrushes.Black,
                new XRect(x, y, LarguraUtil, 14), XStringFormats.TopRight);
            y += 15;

            // Tributos aproximados
            if (model.TotalTributos > 0)
            {
                gfx.DrawString($"Tributos aprox.: R$ {model.TotalTributos:N2}",
                    new XFont(FonteNome, 6.5, XFontStyleEx.Italic), XBrushes.Gray,
                    new XRect(x, y, LarguraUtil, 10), XStringFormats.TopLeft);
                y += 10;
            }

            y += 2;
            return y;
        }

        private double DesenharPagamentos(XGraphics gfx, NfceModel model, double y)
        {
            var fonteLabel = new XFont(FonteNome, 7.5, XFontStyleEx.Regular);
            var fonteValor = new XFont(FonteNome, 7.5, XFontStyleEx.Bold);
            var fonteTitulo = new XFont(FonteNome, 7.5, XFontStyleEx.Bold);

            double x = MargemLateral;

            gfx.DrawString("FORMA DE PAGAMENTO", fonteTitulo, XBrushes.Black,
                new XRect(x, y, LarguraUtil, 11), XStringFormats.TopLeft);
            y += 12;

            foreach (var pag in model.Pagamentos)
            {
                DesenharLinhaValor(gfx, fonteLabel, fonteValor, x, y,
                    pag.TipoPagamentoDescricao + ":", $"R$ {pag.Valor:N2}");
                y += 11;
            }

            if (model.Troco > 0)
            {
                DesenharLinhaValor(gfx, fonteLabel, fonteValor, x, y, "Troco:", $"R$ {model.Troco:N2}");
                y += 11;
            }

            y += 2;
            return y;
        }

        private double DesenharInfoNfce(XGraphics gfx, NfceModel model, double y)
        {
            var fonteLabel = new XFont(FonteNome, 7, XFontStyleEx.Bold);
            var fonteValor = new XFont(FonteNome, 7, XFontStyleEx.Regular);

            double x = MargemLateral;

            // Número, série, data
            gfx.DrawString($"NFC-e Nº {model.NumeroNota}  Série {model.SerieNota}",
                fonteLabel, XBrushes.Black,
                new XRect(x, y, LarguraUtil, 10), XStringFormats.TopCenter);
            y += 10;

            gfx.DrawString($"Emissão: {model.DataEmissao:dd/MM/yyyy HH:mm:ss}",
                fonteValor, XBrushes.Black,
                new XRect(x, y, LarguraUtil, 10), XStringFormats.TopCenter);
            y += 12;

            return y;
        }

        private double DesenharQrCode(XGraphics gfx, NfceModel model, double y)
        {
            if (string.IsNullOrWhiteSpace(model.UrlQrCode)) return y;

            try
            {
                var qrStream = _qrCodeService.GerarQrCodeStream(model.UrlQrCode, 150);
                var qrImage = XImage.FromStream(qrStream);

                double qrSize = 120;
                double qrX = (LarguraPt - qrSize) / 2;
                gfx.DrawImage(qrImage, qrX, y, qrSize, qrSize);
                y += qrSize + 4;

                gfx.DrawString("Consulte pela chave de acesso ou QR Code",
                    new XFont(FonteNome, 6.5, XFontStyleEx.Italic), XBrushes.Gray,
                    new XRect(MargemLateral, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 11;
            }
            catch
            {
                gfx.DrawString("[QR Code indisponível]",
                    new XFont(FonteNome, 7, XFontStyleEx.Italic), XBrushes.Gray,
                    new XRect(MargemLateral, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 12;
            }

            return y;
        }

        private double DesenharChaveAcesso(XGraphics gfx, NfceModel model, double y)
        {
            var fonteLabel = new XFont(FonteNome, 7, XFontStyleEx.Bold);
            var fonteChave = new XFont(FonteNome, 6.5, XFontStyleEx.Regular);

            double x = MargemLateral;

            gfx.DrawString("CHAVE DE ACESSO", fonteLabel, XBrushes.Black,
                new XRect(x, y, LarguraUtil, 10), XStringFormats.TopCenter);
            y += 10;

            // Quebrar a chave em 3 linhas de ~15 caracteres cada
            var chave = model.ChaveAcessoFormatada;
            var partes = QuebrarTexto(chave, 44);
            foreach (var parte in partes)
            {
                gfx.DrawString(parte, fonteChave, XBrushes.Black,
                    new XRect(x, y, LarguraUtil, 9), XStringFormats.TopCenter);
                y += 9;
            }

            y += 3;
            return y;
        }

        private double DesenharProtocolo(XGraphics gfx, NfceModel model, double y)
        {
            var fonteLabel = new XFont(FonteNome, 7, XFontStyleEx.Bold);
            var fonteValor = new XFont(FonteNome, 7, XFontStyleEx.Regular);

            double x = MargemLateral;

            if (!string.IsNullOrWhiteSpace(model.NumeroProtocolo))
            {
                gfx.DrawString($"Protocolo: {model.NumeroProtocolo}", fonteLabel, XBrushes.Black,
                    new XRect(x, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 10;
            }

            if (model.DataAutorizacao.HasValue)
            {
                gfx.DrawString($"Autorizado em: {model.DataAutorizacao:dd/MM/yyyy HH:mm:ss}",
                    fonteValor, XBrushes.Black,
                    new XRect(x, y, LarguraUtil, 10), XStringFormats.TopCenter);
                y += 10;
            }

            y += 4;
            return y;
        }

        private double DesenharInformacoesAdicionais(XGraphics gfx, NfceModel model, double y)
        {
            var fonteLabel = new XFont(FonteNome, 7, XFontStyleEx.Bold);
            var fonteValor = new XFont(FonteNome, 6.5, XFontStyleEx.Regular);

            double x = MargemLateral;

            gfx.DrawString("INFORMAÇÕES ADICIONAIS", fonteLabel, XBrushes.Black,
                new XRect(x, y, LarguraUtil, 10), XStringFormats.TopLeft);
            y += 10;

            var linhas = QuebrarTexto(model.InformacoesAdicionais, 50);
            foreach (var linha in linhas)
            {
                gfx.DrawString(linha, fonteValor, XBrushes.Black,
                    new XRect(x, y, LarguraUtil, 9), XStringFormats.TopLeft);
                y += 9;
            }

            y += 4;
            return y;
        }

        private void DesenharMarcaHomologacao(XGraphics gfx, PdfPage page)
        {
            var fonte = new XFont(FonteNome, 28, XFontStyleEx.Bold);
            var brush = new XSolidBrush(XColor.FromArgb(40, 255, 0, 0));

            gfx.Save();
            gfx.RotateAtTransform(-45,
                new XPoint(page.Width.Point / 2, page.Height.Point / 2));
            gfx.DrawString("HOMOLOGAÇÃO", fonte, brush,
                new XRect(0, page.Height.Point / 2 - 20, page.Width.Point, 40),
                XStringFormats.Center);
            gfx.Restore();
        }

        private double DesenharLinha(XGraphics gfx, double y)
        {
            gfx.DrawLine(XPens.LightGray, MargemLateral, y, LarguraPt - MargemLateral, y);
            return y + 5;
        }

        private void DesenharLinhaValor(XGraphics gfx, XFont fonteLabel, XFont fonteValor,
            double x, double y, string label, string valor,
            XBrush? labelBrush = null, XBrush? valorBrush = null)
        {
            labelBrush ??= XBrushes.Black;
            valorBrush ??= XBrushes.Black;

            gfx.DrawString(label, fonteLabel, labelBrush,
                new XRect(x, y, LarguraUtil / 2, 10), XStringFormats.TopLeft);
            gfx.DrawString(valor, fonteValor, valorBrush,
                new XRect(x, y, LarguraUtil, 10), XStringFormats.TopRight);
        }

        // ── Cálculo de altura dinâmica ────────────────────────────────────────

        private double CalcularAltura(NfceModel model)
        {
            double altura = MargemLateral * 2;

            // Cabeçalho emitente
            altura += 14 + 11 + 10 + 10 + 10 + 3; // nome + cnpj + endereço + municipio + tel + espaço

            // Separador + título DANFE
            altura += 5 + 13 + 12 + 5;

            // Separador + cabeçalho produtos
            altura += 5 + 12;

            // Produtos
            foreach (var prod in model.Produtos)
            {
                altura += prod.Descricao.Length > 22 ? 21 : 11;
                if (prod.Desconto > 0) altura += 9;
            }
            altura += 2;

            // Separador + totais
            altura += 5;
            if (model.TotalDesconto > 0) altura += 11 + 11;
            altura += 15;
            if (model.TotalTributos > 0) altura += 10;
            altura += 2;

            // Separador + pagamentos
            altura += 5 + 12;
            altura += model.Pagamentos.Count * 11;
            if (model.Troco > 0) altura += 11;
            altura += 2;

            // Separador + info NFC-e
            altura += 5 + 10 + 10 + 12;

            // QR Code
            if (!string.IsNullOrWhiteSpace(model.UrlQrCode))
                altura += 120 + 4 + 11;

            // Chave de acesso
            altura += 10 + 27 + 3;

            // Protocolo
            if (!string.IsNullOrWhiteSpace(model.NumeroProtocolo)) altura += 10;
            if (model.DataAutorizacao.HasValue) altura += 10;
            altura += 4;

            // Informações adicionais
            if (!string.IsNullOrWhiteSpace(model.InformacoesAdicionais))
            {
                var linhas = QuebrarTexto(model.InformacoesAdicionais, 50);
                altura += 10 + (linhas.Length * 9) + 4;
            }

            return Math.Max(altura, 400);
        }

        // ── Utilitários ───────────────────────────────────────────────────────

        private static string[] QuebrarTexto(string texto, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(texto)) return Array.Empty<string>();
            if (texto.Length <= maxChars) return new[] { texto };

            var partes = new System.Collections.Generic.List<string>();
            for (int i = 0; i < texto.Length; i += maxChars)
                partes.Add(texto.Substring(i, Math.Min(maxChars, texto.Length - i)));
            return partes.ToArray();
        }

        private static string FormatarCnpj(string cnpj)
        {
            if (cnpj?.Length == 14)
                return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}";
            return cnpj ?? string.Empty;
        }

        private static string FormatarCep(string cep)
        {
            if (cep?.Length == 8)
                return $"{cep[..5]}-{cep[5..]}";
            return cep ?? string.Empty;
        }
    }
}
