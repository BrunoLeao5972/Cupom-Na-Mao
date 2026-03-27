using System.Windows;

namespace CupomNaMao
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        /// <summary>
        /// Alterna entre o tema claro e escuro em tempo de execução.
        /// </summary>
        public static void AlternarTema(string tema)
        {
            var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
            dicts.Clear();

            var uri = tema == "Escuro"
                ? new System.Uri("UI/Themes/DarkTheme.xaml", System.UriKind.Relative)
                : new System.Uri("UI/Themes/LightTheme.xaml", System.UriKind.Relative);

            dicts.Add(new ResourceDictionary { Source = uri });
        }
    }
}
