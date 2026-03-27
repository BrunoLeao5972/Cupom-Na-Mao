using System.Windows;

namespace CupomNaMao.UI.Views
{
    public partial class SobreWindow : Window
    {
        public SobreWindow()
        {
            InitializeComponent();
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
