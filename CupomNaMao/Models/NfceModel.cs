using System;
using System.Collections.Generic;

namespace CupomNaMao.Models
{
    /// <summary>
    /// Representa os dados extraídos de um XML de NFC-e.
    /// </summary>
    public class NfceModel
    {
        // ── Emitente ──────────────────────────────────────────────────────────
        public string NomeEmitente { get; set; } = string.Empty;
        public string CnpjEmitente { get; set; } = string.Empty;
        public string EnderecoEmitente { get; set; } = string.Empty;
        public string MunicipioEmitente { get; set; } = string.Empty;
        public string UfEmitente { get; set; } = string.Empty;
        public string CepEmitente { get; set; } = string.Empty;
        public string TelefoneEmitente { get; set; } = string.Empty;
        public string IeEmitente { get; set; } = string.Empty;
        public string CscIdEmitente { get; set; } = string.Empty;

        // ── Identificação ─────────────────────────────────────────────────────
        public string ChaveAcesso { get; set; } = string.Empty;
        public string NumeroNota { get; set; } = string.Empty;
        public string SerieNota { get; set; } = string.Empty;
        public DateTime DataEmissao { get; set; }
        public string NaturezaOperacao { get; set; } = string.Empty;
        public int TipoEmissao { get; set; }
        public string Ambiente { get; set; } = string.Empty; // 1=Produção, 2=Homologação

        // ── Produtos ──────────────────────────────────────────────────────────
        public List<ProdutoModel> Produtos { get; set; } = new();

        // ── Totais ────────────────────────────────────────────────────────────
        public decimal TotalProdutos { get; set; }
        public decimal TotalDesconto { get; set; }
        public decimal TotalAcrescimo { get; set; }
        public decimal TotalNota { get; set; }
        public decimal TotalTributos { get; set; }
        public decimal TotalIcms { get; set; }
        public decimal TotalPis { get; set; }
        public decimal TotalCofins { get; set; }

        // ── Pagamento ─────────────────────────────────────────────────────────
        public List<PagamentoModel> Pagamentos { get; set; } = new();
        public decimal TotalPago { get; set; }
        public decimal Troco { get; set; }

        // ── Protocolo ─────────────────────────────────────────────────────────
        public string NumeroProtocolo { get; set; } = string.Empty;
        public DateTime? DataAutorizacao { get; set; }

        // ── QR Code ───────────────────────────────────────────────────────────
        public string UrlQrCode { get; set; } = string.Empty;
        public string UrlConsulta { get; set; } = string.Empty;

        // ── Informações adicionais ────────────────────────────────────────────
        public string InformacoesAdicionais { get; set; } = string.Empty;

        // ── Arquivo de origem ─────────────────────────────────────────────────
        public string CaminhoArquivo { get; set; } = string.Empty;
        public bool Valida { get; set; }
        public string ErroValidacao { get; set; } = string.Empty;

        // ── Propriedades calculadas ───────────────────────────────────────────
        public string ChaveAcessoFormatada =>
            ChaveAcesso.Length == 44
                ? string.Join(" ", System.Text.RegularExpressions.Regex.Matches(ChaveAcesso, ".{1,4}").Select(m => m.Value))
                : ChaveAcesso;

        public string NomeArquivo => System.IO.Path.GetFileName(CaminhoArquivo);

        public string SugestaoPdfNome =>
            $"NFCe_{SanitizarNome(NomeEmitente)}_{TotalNota:F2}_{DataEmissao:ddMMyyyy}.pdf"
                .Replace(",", ".")
                .Replace(" ", "_");

        private static string SanitizarNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return "Emitente";
            var invalidos = System.IO.Path.GetInvalidFileNameChars();
            return new string(nome.Where(c => !invalidos.Contains(c)).ToArray())
                .Replace(" ", "_")
                .Substring(0, Math.Min(nome.Replace(" ", "_").Length, 30));
        }
    }

    /// <summary>
    /// Representa um produto/item da NFC-e.
    /// </summary>
    public class ProdutoModel
    {
        public int NumeroItem { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Ncm { get; set; } = string.Empty;
        public string Cfop { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal Desconto { get; set; }
        public decimal Acrescimo { get; set; }
        public decimal ValorLiquido => ValorTotal - Desconto + Acrescimo;
    }

    /// <summary>
    /// Representa uma forma de pagamento da NFC-e.
    /// </summary>
    public class PagamentoModel
    {
        public string TipoPagamento { get; set; } = string.Empty;
        public decimal Valor { get; set; }

        public string TipoPagamentoDescricao => TipoPagamento switch
        {
            "01" => "Dinheiro",
            "02" => "Cheque",
            "03" => "Cartão de Crédito",
            "04" => "Cartão de Débito",
            "05" => "Crédito Loja",
            "10" => "Vale Alimentação",
            "11" => "Vale Refeição",
            "12" => "Vale Presente",
            "13" => "Vale Combustível",
            "15" => "Boleto Bancário",
            "16" => "Depósito Bancário",
            "17" => "PIX",
            "18" => "Transferência Bancária",
            "19" => "Programa de Fidelidade",
            "90" => "Sem Pagamento",
            "99" => "Outros",
            _ => $"Pagamento ({TipoPagamento})"
        };
    }

    /// <summary>
    /// Entrada do histórico de arquivos recentes.
    /// </summary>
    public class HistoricoItem
    {
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string NomeEmitente { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public DateTime DataEmissao { get; set; }
        public DateTime DataAbertura { get; set; } = DateTime.Now;
        public string NomeArquivo => System.IO.Path.GetFileName(CaminhoArquivo);
    }
}
