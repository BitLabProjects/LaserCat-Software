using bitLab.LaserCat.ViewModels;
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
  /// Interaction logic for UCuttingPlane.xaml
  /// </summary>
  public partial class UCuttingPlane : UserControl
  {
    public UCuttingPlane()
    {
      InitializeComponent();
    }

    private void Grid_SizeChanged_1(object sender, SizeChangedEventArgs e)
    {
      SetCuttingPlaneVMSize();
    }

    private void Grid_Loaded_1(object sender, RoutedEventArgs e)
    {
      SetCuttingPlaneVMSize();
    }

    private void SetCuttingPlaneVMSize()
    {
      var cuttingPlaneVM = RootGrid.DataContext as CCuttingPlaneVM;
      if (cuttingPlaneVM == null)
        return;
      cuttingPlaneVM.Resize(RootGrid.ActualWidth, RootGrid.ActualHeight);
    }
  }
}
