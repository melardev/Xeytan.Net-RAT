using C = System.Console;
namespace XeytanCSharpServer.Ui.Console.Views
{
    
    interface IView
    {
        void PrintBanner();

        string Loop();
    }
}
