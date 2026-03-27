using System.Windows;
using CupomNaMao.Services;

namespace CupomNaMao.UI.Views
{
    public partial class ConfiguracoesWindow : Window
    {
        private readonly ConfiguracaoService _configService;

        public ConfiguracoesWindow(ConfiguracaoService configService)
        {
            InitializeComponent();
            _configService = configService;
            DataContext = configService.Config;
        }

        private void BtnProcurarPasta_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecione a pasta padrão para salvar PDFs",
                UseDescriptionForTitle = true,
                SelectedPath = TxtPastaDestino.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtPastaDestino.Text = dialog.SelectedPath;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            _configService.Config.Tema = "Claro";
            App.AlternarTema("Claro");

            _configService.Salvar();
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
