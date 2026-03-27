using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CupomNaMao.Models;
using CupomNaMao.Services;
using CupomNaMao.UI.ViewModels;

namespace CupomNaMao.UI.Views
{
    /// <summary>
    /// Code-behind da janela principal da aplicação Cupom Na Mão.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly XmlReaderService _xmlReader;
        private readonly QrCodeService _qrCodeService;
        private readonly DanfeGeneratorService _danfeGenerator;
        private readonly PdfService _pdfService;
        private readonly HistoricoService _historicoService;
        private readonly ConfiguracaoService _configService;

        public MainWindow()
        {
            InitializeComponent();

            // Injeção de dependências manual
            _xmlReader = new XmlReaderService();
            _qrCodeService = new QrCodeService();
            _danfeGenerator = new DanfeGeneratorService(_qrCodeService);
            _pdfService = new PdfService(_danfeGenerator);
            _historicoService = new HistoricoService();
            _configService = new ConfiguracaoService();

            _vm = new MainViewModel(_historicoService, _configService);
            DataContext = _vm;

            // Tema único da aplicação: Claro
            _configService.Config.Tema = "Claro";
            App.AlternarTema("Claro");
        }

        // ═══════════════════════════════════════════════════════════════════════
        // DRAG & DROP
        // ═══════════════════════════════════════════════════════════════════════

        private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
                _vm.ArrastandoArquivo = true;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            _vm.ArrastandoArquivo = false;

            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

            var arquivos = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            var xmls = arquivos.Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)).ToList();

            if (xmls.Count == 0)
            {
                MostrarErro("Nenhum arquivo XML encontrado nos arquivos arrastados.");
                return;
            }

            if (xmls.Count == 1)
                await CarregarXmlAsync(xmls[0]);
            else
                await ProcessarLoteAsync(xmls);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // BOTÕES PRINCIPAIS
        // ═══════════════════════════════════════════════════════════════════════

        private async void BtnSelecionarXml_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecionar XML de NFC-e",
                Filter = "Arquivos XML (*.xml)|*.xml|Todos os arquivos (*.*)|*.*",
                Multiselect = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() != true) return;

            if (dialog.FileNames.Length == 1)
                await CarregarXmlAsync(dialog.FileNames[0]);
            else
                await ProcessarLoteAsync(dialog.FileNames);
        }

        private async void BtnGerarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.NfceAtual == null) return;
            await GerarPdfAtualAsync();
        }

        private void BtnVisualizarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.PdfGerado || string.IsNullOrWhiteSpace(_vm.CaminhoPdfAtual)) return;

            try
            {
                _pdfService.AbrirPdf(_vm.CaminhoPdfAtual);
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao abrir PDF: {ex.Message}");
            }
        }

        private async void BtnSalvarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.NfceAtual == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Salvar DANFE NFC-e em PDF",
                Filter = "Arquivo PDF (*.pdf)|*.pdf",
                FileName = _vm.NfceAtual.SugestaoPdfNome,
                InitialDirectory = string.IsNullOrWhiteSpace(_configService.Config.PastaDestinoPdf)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : _configService.Config.PastaDestinoPdf
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                _vm.Carregando = true;
                _vm.MensagemStatus = "Gerando PDF...";

                await Task.Run(() => _pdfService.GerarESalvarEm(_vm.NfceAtual, dialog.FileName));

                _vm.MarcarPdfGerado(dialog.FileName);
                MostrarSucesso($"PDF salvo em: {dialog.FileName}");

                if (_configService.Config.AbrirPdfAposGerar)
                    _pdfService.AbrirPdf(dialog.FileName);
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao salvar PDF: {ex.Message}");
            }
            finally
            {
                _vm.Carregando = false;
            }
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.PdfGerado || string.IsNullOrWhiteSpace(_vm.CaminhoPdfAtual)) return;

            try
            {
                _pdfService.ImprimirPdf(_vm.CaminhoPdfAtual);
                _vm.MensagemStatus = "Enviado para impressão.";
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao imprimir: {ex.Message}");
            }
        }

        private void BtnCopiarChave_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.NfceAtual == null || string.IsNullOrWhiteSpace(_vm.NfceAtual.ChaveAcesso)) return;

            try
            {
                System.Windows.Clipboard.SetText(_vm.NfceAtual.ChaveAcesso);
                MostrarSucesso("Chave de acesso copiada para a área de transferência.");
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao copiar chave: {ex.Message}");
            }
        }

        private void BtnConsultarSefaz_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.NfceAtual == null) return;

            try
            {
                _pdfService.AbrirConsultaSefaz(_vm.NfceAtual);
                var uf = !string.IsNullOrWhiteSpace(_vm.NfceAtual.UfEmitente)
                    ? _vm.NfceAtual.UfEmitente
                    : "UF identificada pela chave";

                _vm.MensagemStatus = $"Abrindo consulta oficial da SEFAZ ({uf})...";
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao abrir SEFAZ: {ex.Message}");
            }
        }

        private async void BtnProcessarLote_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecionar XMLs para processamento em lote",
                Filter = "Arquivos XML (*.xml)|*.xml",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;
            if (dialog.FileNames.Length == 0) return;

            await ProcessarLoteAsync(dialog.FileNames);
        }

        private async void BtnGerarZip_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecionar XMLs para gerar ZIP",
                Filter = "Arquivos XML (*.xml)|*.xml",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;
            if (dialog.FileNames.Length == 0) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Salvar arquivo ZIP com os PDFs",
                Filter = "Arquivo ZIP (*.zip)|*.zip",
                FileName = $"DANFEs_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
            };

            if (saveDialog.ShowDialog() != true) return;

            try
            {
                _vm.Carregando = true;
                _vm.MensagemStatus = "Gerando PDFs e compactando...";

                var modelos = await Task.Run(() => _xmlReader.LerMultiplosXml(dialog.FileNames));
                var validos = modelos.Where(m => m.Valida).ToList();

                if (validos.Count == 0)
                {
                    MostrarErro("Nenhum XML válido encontrado para processar.");
                    return;
                }

                string caminhoZip = string.Empty;
                await Task.Run(() =>
                {
                    caminhoZip = _pdfService.GerarZip(validos, saveDialog.FileName,
                        (atual, total, nome) =>
                        {
                            Dispatcher.Invoke(() =>
                                _vm.MensagemStatus = $"Processando {atual}/{total}: {nome}");
                        });
                });

                MostrarSucesso($"ZIP gerado com {validos.Count} PDFs: {caminhoZip}");
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao gerar ZIP: {ex.Message}");
            }
            finally
            {
                _vm.Carregando = false;
            }
        }

        private void BtnNovoArquivo_Click(object sender, RoutedEventArgs e)
        {
            _vm.LimparEstado();
        }

        private void BtnLimparHistorico_Click(object sender, RoutedEventArgs e)
        {
            _vm.LimparHistorico();
        }

        private void BtnConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            var win = new ConfiguracoesWindow(_configService);
            win.Owner = this;
            win.ShowDialog();
        }

        private void BtnSobre_Click(object sender, RoutedEventArgs e)
        {
            var win = new SobreWindow();
            win.Owner = this;
            win.ShowDialog();
        }

        private void HistoricoItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string caminho)
                _ = CarregarXmlAsync(caminho);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // LÓGICA INTERNA
        // ═══════════════════════════════════════════════════════════════════════

        private async Task CarregarXmlAsync(string caminho)
        {
            try
            {
                _vm.Carregando = true;
                _vm.MensagemStatus = $"Lendo {Path.GetFileName(caminho)}...";

                NfceModel model = await Task.Run(() => _xmlReader.LerXml(caminho));

                if (!model.Valida)
                {
                    MostrarErro($"XML inválido: {model.ErroValidacao}");
                    return;
                }

                _vm.CarregarNfce(model);

                // Gerar PDF automaticamente se configurado
                if (_configService.Config.GerarPdfAutomaticamente)
                    await GerarPdfAtualAsync(silencioso: true);
            }
            catch (Exception ex)
            {
                MostrarErro($"Erro ao carregar XML: {ex.Message}");
            }
            finally
            {
                _vm.Carregando = false;
            }
        }

        private async Task GerarPdfAtualAsync(bool silencioso = false)
        {
            if (_vm.NfceAtual == null) return;

            try
            {
                _vm.Carregando = true;
                if (!silencioso) _vm.MensagemStatus = "Gerando PDF...";

                string pastaDestino = string.IsNullOrWhiteSpace(_configService.Config.PastaDestinoPdf)
                    ? Path.GetDirectoryName(_vm.NfceAtual.CaminhoArquivo) ?? Path.GetTempPath()
                    : _configService.Config.PastaDestinoPdf;

                string caminhoPdf = string.Empty;
                await Task.Run(() =>
                {
                    caminhoPdf = _pdfService.GerarESalvar(_vm.NfceAtual!, pastaDestino);
                });

                _vm.MarcarPdfGerado(caminhoPdf);

                if (!silencioso)
                    MostrarSucesso($"PDF gerado: {Path.GetFileName(caminhoPdf)}");

                if (_configService.Config.AbrirPdfAposGerar)
                    _pdfService.AbrirPdf(caminhoPdf);
            }
            catch (Exception ex) {
                MostrarErro($"Erro ao gerar PDF: {ex.Message}");
            }
            finally
            {
                _vm.Carregando = false;
            }
        }

        private async Task ProcessarLoteAsync(IEnumerable<string> arquivos)
        {
            var lista = arquivos.ToList();

            using (var pastaDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                pastaDialog.Description = "Selecione a pasta de destino para os PDFs";
                pastaDialog.UseDescriptionForTitle = true;

                if (pastaDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                string pastaDestino = pastaDialog.SelectedPath;

                try
                {
                    _vm.Carregando = true;
                    _vm.MensagemStatus = $"Processando {lista.Count} arquivo(s)...";

                    LoteResultado resultado = null!;
                    await Task.Run(() =>
                    {
                        var modelos = _xmlReader.LerMultiplosXml(lista);
                        var validos = modelos.Where(m => m.Valida).ToList();

                        resultado = _pdfService.ProcessarLote(validos, pastaDestino,
                            (atual, total, nome) =>
                            {
                                Dispatcher.Invoke(() =>
                                    _vm.MensagemStatus = $"Processando {atual}/{total}: {nome}");
                            });
                    });

                    string msg = $"Lote concluído: {resultado.Sucesso.Count} PDF(s) gerado(s).";
                    if (resultado.Erros.Count > 0)
                        msg += $" {resultado.Erros.Count} erro(s).";

                    MostrarSucesso(msg);

                    // Carregar o primeiro XML válido na tela
                    if (lista.Count > 0)
                        await CarregarXmlAsync(lista[0]);
                }
                catch (Exception ex)
                {
                    MostrarErro($"Erro no processamento em lote: {ex.Message}");
                }
                finally
                {
                    _vm.Carregando = false;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // FEEDBACK VISUAL
        // ═══════════════════════════════════════════════════════════════════════

        private void MostrarSucesso(string mensagem)
        {
            _vm.MensagemStatus = $"✅ {mensagem}";
        }

        private void MostrarErro(string mensagem)
        {
            _vm.MensagemStatus = $"❌ {mensagem}";
            System.Windows.MessageBox.Show(mensagem, "Erro — Cupom Na Mão",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }
}
