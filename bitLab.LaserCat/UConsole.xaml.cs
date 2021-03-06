﻿using System;
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
  /// Interaction logic for UConsole.xaml
  /// </summary>
  public partial class UConsole : UserControl
  {
    public UConsole()
    {
      InitializeComponent();
    }

    private void Grid_Loaded_1(object sender, RoutedEventArgs e)
    {
      var grid = sender as Grid;
      GeneralTransform groupBoxTransform = grid.TransformToAncestor(scrollViewer);
      Rect rectangle = groupBoxTransform.TransformBounds(new Rect(new Point(0, 0), grid.RenderSize));
      scrollViewer.ScrollToVerticalOffset(rectangle.Top + scrollViewer.VerticalOffset);
    }
  }
}
