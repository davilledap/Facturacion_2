using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Facturacion
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Facturacion()
                ///Testing Git 2018
                ///////asdfasdfasf
                /////asdfasdf
                ///asdfasdfa
                ///asfda
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
