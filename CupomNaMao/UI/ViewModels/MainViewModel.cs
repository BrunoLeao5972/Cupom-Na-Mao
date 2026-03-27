using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CupomNaMao.Models;
using CupomNaMao.Services;

namespace CupomNaMao.UI.ViewModels
{
    /// <summary>
    /// ViewModel principal da aplicação — gerencia estado e comandos da janela principal.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly HistoricoService _historicoService;
        private readonly ConfiguracaoService _configService;

        private NfceModel? _nfceAtual;
        private bool _nfceCarregada;
        private bool _pdfGerado;
        private bool _carregando;
        private bool _arrastandoArquivo;
        private string _mensagemStatus = "Pronto. Selecione ou arraste um arquivo XML de NFC-e.";
        private string _tituloArquivo = "Nenhum arquivo carregado";
        private string _nomePdfGerado = string.Empty;
        private string _caminhoPdfAtual = string.Empty;

        public MainViewModel(HistoricoService historicoService, ConfiguracaoService configService)
        {
            _historicoService = historicoService;
            _configService = configService;
            AtualizarHistorico();

            AbrirHistoricoCommand = new RelayCommand<string>(AbrirDoHistorico);
        }

        // ── Propriedades ──────────────────────────────────────────────────────

        public NfceModel? NfceAtual
        {
            get => _nfceAtual;
            set { _nfceAtual = value; OnPropertyChanged(); }
        }

        public bool NfceCarregada
        {
            get => _nfceCarregada;
            set { _nfceCarregada = value; OnPropertyChanged(); }
        }

        public bool PdfGerado
        {
            get => _pdfGerado;
            set { _pdfGerado = value; OnPropertyChanged(); }
        }

        public bool Carregando
        {
            get => _carregando;
            set { _carregando = value; OnPropertyChanged(); }
        }

        public bool ArrastandoArquivo
        {
            get => _arrastandoArquivo;
            set { _arrastandoArquivo = value; OnPropertyChanged(); }
        }

        public string MensagemStatus
        {
            get => _mensagemStatus;
            set { _mensagemStatus = value; OnPropertyChanged(); }
        }

        public string TituloArquivo
        {
            get => _tituloArquivo;
            set { _tituloArquivo = value; OnPropertyChanged(); }
        }

        public string NomePdfGerado
        {
            get => _nomePdfGerado;
            set { _nomePdfGerado = value; OnPropertyChanged(); }
        }

        public string CaminhoPdfAtual
        {
            get => _caminhoPdfAtual;
            set { _caminhoPdfAtual = value; OnPropertyChanged(); }
        }

        public bool AmbienteHomologacao =>
            NfceAtual?.Ambiente == "Homologação";

        public bool TemHistorico => Historico.Count > 0;

        public ObservableCollection<HistoricoItem> Historico { get; } = new();

        public ICommand AbrirHistoricoCommand { get; }

        // ── Métodos públicos ──────────────────────────────────────────────────

        public void CarregarNfce(NfceModel model)
        {
            NfceAtual = model;
            NfceCarregada = true;
            PdfGerado = false;
            CaminhoPdfAtual = string.Empty;
            NomePdfGerado = string.Empty;
            TituloArquivo = model.NomeArquivo;
            MensagemStatus = $"NFC-e carregada: {model.NomeEmitente} — {model.TotalNota:C2}";
            OnPropertyChanged(nameof(AmbienteHomologacao));

            _historicoService.Adicionar(model);
            AtualizarHistorico();
        }

        public void MarcarPdfGerado(string caminhoPdf)
        {
            CaminhoPdfAtual = caminhoPdf;
            NomePdfGerado = System.IO.Path.GetFileName(caminhoPdf);
            PdfGerado = true;
            MensagemStatus = $"PDF gerado: {NomePdfGerado}";
        }

        public void LimparEstado()
        {
            NfceAtual = null;
            NfceCarregada = false;
            PdfGerado = false;
            CaminhoPdfAtual = string.Empty;
            NomePdfGerado = string.Empty;
            TituloArquivo = "Nenhum arquivo carregado";
            MensagemStatus = "Pronto. Selecione ou arraste um arquivo XML de NFC-e.";
        }

        public void LimparHistorico()
        {
            _historicoService.Limpar();
            AtualizarHistorico();
        }

        public Configuracoes Config => _configService.Config;

        public void SalvarConfig() => _configService.Salvar();

        // ── Privados ──────────────────────────────────────────────────────────

        private void AtualizarHistorico()
        {
            Historico.Clear();
            foreach (var item in _historicoService.Itens)
                Historico.Add(item);
            OnPropertyChanged(nameof(TemHistorico));
        }

        private void AbrirDoHistorico(string? caminho)
        {
            // Será tratado pelo code-behind
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Implementação genérica de ICommand para uso no ViewModel.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) =>
            _canExecute?.Invoke(parameter is T t ? t : default) ?? true;

        public void Execute(object? parameter) =>
            _execute(parameter is T t ? t : default);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
