using System;
using System.Windows.Forms;

namespace BrightnessSwitch
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var brightnessControl = new LightControl();
            Application.Run();
        }
    }
}
