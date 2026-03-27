using System;
using System.IO;
using System.Text.Json;

namespace CupomNaMao.Services
{
    /// <summary>
    /// Configurações persistentes da aplicação.
    /// </summary>
    public class Configuracoes
    {
        public bool GerarPdfAutomaticamente { get; set; } = true;
        public bool AbrirPdfAposGerar { get; set; } = false;
        public string PastaDestinoPdf { get; set; } = string.Empty;
        public string Tema { get; set; } = "Claro"; // "Claro" ou "Escuro"
        public bool MostrarTotalTributos { get; set; } = true;
        public bool MostrarInformacoesAdicionais { get; set; } = true;
        public int TamanhoQrCode { get; set; } = 150;
        public bool AbrirConsultaSefazNoNavegador { get; set; } = true;
    }

    /// <summary>
    /// Serviço para carregar e salvar as configurações da aplicação.
    /// </summary>
    public class ConfiguracaoService
    {
        private readonly string _caminhoArquivo;
        private Configuracoes _config = new();

        public ConfiguracaoService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pasta = Path.Combine(appData, "CupomNaMao");
            Directory.CreateDirectory(pasta);
            _caminhoArquivo = Path.Combine(pasta, "configuracoes.json");
            Carregar();
        }

        public Configuracoes Config => _config;

        public void Salvar()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_caminhoArquivo, json);
            }
            catch { /* Ignorar erros de escrita */ }
        }

        private void Carregar()
        {
            try
            {
                if (!File.Exists(_caminhoArquivo)) return;
                var json = File.ReadAllText(_caminhoArquivo);
                _config = JsonSerializer.Deserialize<Configuracoes>(json) ?? new Configuracoes();
            }
            catch
            {
                _config = new Configuracoes();
            }
        }
    }
}
