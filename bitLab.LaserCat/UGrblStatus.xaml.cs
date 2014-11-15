using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace bitLab.LaserCat
{
  /// <summary>
  /// Interaction logic for UGrblStatus.xaml
  /// </summary>
  public partial class UGrblStatus : UserControl
  {
    public UGrblStatus()
    {
      InitializeComponent();
      updateTxtProgressBar();
    }

    private void updateTxtProgressBar()
    {
      txtProgressBar.Text = String.Format("{0:0.0}%", progressBar.Value * 100.0 / progressBar.Maximum);
    }

    private void progressBar_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      updateTxtProgressBar();
    }
  }
}
