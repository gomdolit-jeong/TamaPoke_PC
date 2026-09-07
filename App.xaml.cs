using System.Configuration;
using System.Data;
using System.Windows;
using TamaPoke.Models;

namespace TamaPoke
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 앱이 시작될 때 포켓몬 데이터를 가장 먼저 메모리에 로드합니다.
            PokemonDex.LoadPokemonData();
        }
    }

}
