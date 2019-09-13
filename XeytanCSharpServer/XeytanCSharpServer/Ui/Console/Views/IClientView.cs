using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeytanCSharpServer.Models;

namespace XeytanCSharpServer.Ui.Console.Views
{
    interface IClientView : IView
    {
        void SetActiveClient(Client client);
    }
}