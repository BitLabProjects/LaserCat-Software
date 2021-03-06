﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using bitLab.LaserCat.ViewModels;
using bitLab.ViewModel;
using bitLab.LaserCat.Model;

namespace bitLab.LaserCat
{
  public class CLaserCatApp
  {
    public void Startup()
    {
      System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-us");
      System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-us");

      CBaseVM.Dispatcher = App.Current.Dispatcher;

      Logging.Log.LogInfo("Initializing application");
      CLaserCat.Create();

      var VM = new CLaserCatVM();
      Logging.Log.LogInfo("Starting up User interface");

      var window = new MainWindow();
      window.DataContext = VM;
      window.Show();
      Logging.Log.LogInfo("Application initialized");
    }
  }
}
