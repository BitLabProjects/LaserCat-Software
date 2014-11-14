using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bitLab.LaserCat.ViewModels;
using bitLab.ViewModel;

namespace bitLab.LaserCat
{
  public class CLaserCatApp
  {
    public void Startup()
    {
      CBaseVM.Dispatcher = App.Current.Dispatcher;
      var VM = new CLaserCatVM();
      Logging.Log.LogInfo("Starting up User interface");

      var window = new MainWindow();
      window.DataContext = VM;
      window.Show();
      Logging.Log.LogInfo("Application initialized");
    }
  }
}
