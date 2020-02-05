using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;
using System.Reflection;

namespace ComponentBuilder.WpfUtils
{
    public static class CanvasUtils
    {
        public static void SaveCanvasAsImage(Canvas _to_save, string _name, double _dpi = 96d)
        {
            if (_to_save == null) return;
            string file_name = (string.IsNullOrEmpty(_name)) ? "Canvas_" : _name + "_";

            // get info about the DPI of the displaying device
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

            int dpiX = (dpiXProperty == null) ? 96 : (int)dpiXProperty.GetValue(null, null);
            int dpiY = (dpiYProperty == null) ? 96 : (int)dpiYProperty.GetValue(null, null);

            double sc_factX = _dpi / dpiX;
            double sc_factY = _dpi / dpiY;

            // determine position in visual parent to avoid cropping
            Visual visual_parent = LogicalTreeHelper.GetParent(_to_save) as Visual;
            Point offset_in_visual_parent = new Point(0, 0);
            if (visual_parent != null)
            {
                // if the visual parent is a scroll viewer, scroll to top left to avoid cropping
                ScrollViewer scrl_viewer = visual_parent as ScrollViewer;
                if (scrl_viewer != null)
                {
                    scrl_viewer.ScrollToTop();
                    scrl_viewer.ScrollToLeftEnd();
                }

                var transf = _to_save.TransformToAncestor(visual_parent);
                offset_in_visual_parent = transf.Transform(new Point(0, 0));
            }
            offset_in_visual_parent.X = Math.Max(0, offset_in_visual_parent.X);
            offset_in_visual_parent.Y = Math.Max(0, offset_in_visual_parent.Y);

            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = file_name + DateTime.Now.ToString("yyyyMMdd"), // Default file name
                    DefaultExt = ".png", // Default file extension
                    Filter = "png files|*.png" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the image
                    Rect rect = new Rect(_to_save.RenderSize);
                    RenderTargetBitmap rtb = new RenderTargetBitmap((int)((rect.Right + offset_in_visual_parent.X) * sc_factX),
                      (int)((rect.Bottom + offset_in_visual_parent.Y)* sc_factY), _dpi, _dpi, System.Windows.Media.PixelFormats.Default);
                    rtb.Render(_to_save);
                    
                    //endcode as PNG
                    BitmapEncoder pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

                    // save
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        pngEncoder.Save(ms);
                        ms.Close();
                        using (System.IO.FileStream fs = System.IO.File.Create(dlg.FileName))
                        {
                            byte[] content_B = ms.ToArray();
                            fs.Write(content_B, 0, content_B.Length);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Canvas as Image", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Canvas as Image", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
