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
  /// Interaction logic for UProgressBarWithPercent.xaml
  /// </summary>
  public partial class UProgressBarWithPercent : UserControl
  {
    public UProgressBarWithPercent()
    {
      InitializeComponent();
      stackPanel.DataContext = this;
      updateTxtProgressBar();
    }

    public double Maximum
    {
      get { return (double)GetValue(MaximumProperty); }
      set { SetValue(MaximumProperty, value); }
    }
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(double), typeof(UProgressBarWithPercent), new PropertyMetadata(100.0));

    public double Value
    {
      get { return (double)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(double), typeof(UProgressBarWithPercent), new PropertyMetadata(100.0));

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
