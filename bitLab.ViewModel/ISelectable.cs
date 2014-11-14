using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitLab.ViewModel
{
  public delegate void SelectionChangedEventHandler(object sender, EventArgs e);
  public delegate void ShapeChangedEventHandler(object sender, EventArgs e);

  public interface ISelectable
  {
    event SelectionChangedEventHandler SelectionChanged;
    bool IsSelected { get; set; }
  }
}
