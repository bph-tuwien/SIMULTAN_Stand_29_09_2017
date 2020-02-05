using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;

using GeometryViewer.ComponentReps;

namespace GeometryViewer.UIElements
{
    public enum ComponentRepWindowMode { CREATE, EDIT, INFO }
    /// <summary>
    /// Interaction logic for ComponentRepWindow.xaml
    /// </summary>
    public partial class ComponentRepWindow : Window
    {
        #region STATIC

        private static readonly string SUP2, SUP3;
        static ComponentRepWindow()
        {
            char sup2 = '\u00B2';
            char sup3 = '\u00B3';
            SUP2 = sup2.ToString();
            SUP3 = sup3.ToString();
        }

        #endregion

        #region CLASS MEMBERS

        // general
        private System.IFormatProvider format;
        private ComponentRepWindowMode mode;
        private CompRepDescirbes comp_current;
        public ComponentRepWindowMode Mode { get { return this.mode; } }

        #endregion

        #region .CTOR

        public ComponentRepWindow()
        {
            InitializeComponent();
            this.format = new System.Globalization.NumberFormatInfo();
            this.mode = ComponentRepWindowMode.CREATE;
            SetControls();
        }

        public ComponentRepWindow(ComponentRepWindowMode _mode, CompRepDescirbes _comp)
        {
            InitializeComponent();
            this.format = new System.Globalization.NumberFormatInfo();
            this.mode = _mode;
            this.comp_current = _comp;
            this.tb_NamePreview.Text = (_comp == null) ? string.Empty : _comp.Comp_Description;
            SetControls();
        }

        private void SetControls()
        {
            switch(this.mode)
            {
                case ComponentRepWindowMode.CREATE:
                    break;
                case ComponentRepWindowMode.EDIT:
                    this.Title = "Komponente bearbeiten";
                    break;
                case ComponentRepWindowMode.INFO:
                    this.Title = "Komponenteneigenschaften";
                    this.btn_CANCEL.IsEnabled = false;
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region PROPERTIES: ZONE (data from associated ZonedVolume)
        private EntityGeometry.ZonedVolume zone;
        public EntityGeometry.ZonedVolume Zone 
        {
            get { return this.zone; }
            set
            {
                this.zone = value;
                if (this.zone != null)
                {
                    tb_GeomName.Text = "DERIVED FROM THE ASSOCIATED GEOMETRY: " + this.zone.EntityName.ToUpper();

                    tb_zElevN.Text = this.zone.Elevation_NET.ToString("F2", this.format) + " m";
                    tb_zElevG.Text = this.zone.Elevation_GROSS.ToString("F2", this.format) + " m";
                    tb_zElevA.Text = this.zone.Elevation_AXES.ToString("F2", this.format) + " m";

                    tb_zCeilN.Text = this.zone.Ceiling_NET.ToString("F2", this.format) + " m";
                    tb_zCeilG.Text = this.zone.Ceiling_GROSS.ToString("F2", this.format) + " m";
                    tb_zCeilA.Text = this.zone.Ceiling_AXES.ToString("F2", this.format) + " m";

                    tb_zHeightN.Text = this.zone.MaxHeight_NET.ToString("F2", this.format) + " m";
                    tb_zHeightG.Text = this.zone.MaxHeight_GROSS.ToString("F2", this.format) + " m";
                    tb_zHeightA.Text = this.zone.MaxHeight_AXES.ToString("F2", this.format) + " m";

                    tb_zPerimeter.Text = this.zone.Perimeter.ToString("F2", this.format) + " m";

                    tb_zAreaN.Text = this.zone.Area_NET.ToString("F2", this.format) + " m" + SUP2;
                    tb_zAreaG.Text = this.zone.Area_GROSS.ToString("F2", this.format) + " m" + SUP2;
                    tb_zAreaA.Text = this.zone.Area_AXES.ToString("F2", this.format) + " m" + SUP2;

                    tb_zVolumeN.Text = this.zone.Volume_NET.ToString("F2", this.format) + " m" + SUP3;
                    tb_zVolumeG.Text = this.zone.Volume_GROSS.ToString("F2", this.format) + " m" + SUP3;
                    tb_zVolumeA.Text = this.zone.Volume_AXES.ToString("F2", this.format) + " m" + SUP3;    
                }
                else
                {
                    tb_GeomName.Text = "DERIVED FROM THE ASSOCIATED GEOMETRY: ";

                    tb_zElevN.Text = "__ m";
                    tb_zElevG.Text = "__ m";
                    tb_zElevA.Text = "__ m";

                    tb_zHeightN.Text = "__ m";
                    tb_zHeightG.Text = "__ m";
                    tb_zHeightA.Text = "__ m";

                    tb_zPerimeter.Text = "__ m";

                    tb_zAreaN.Text = "__ m" + SUP2;
                    tb_zAreaG.Text = "__ m" + SUP2;
                    tb_zAreaA.Text = "__ m" + SUP2;

                    tb_zVolumeN.Text = "__ m" + SUP3;
                    tb_zVolumeG.Text = "__ m" + SUP3;
                    tb_zVolumeA.Text = "__ m" + SUP3;
                }
            }
        }
        #endregion

        #region OK Button

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (this.Mode == ComponentRepWindowMode.EDIT)
            {
                // TODO
            }
            else if (this.Mode == ComponentRepWindowMode.INFO)
            {
                // DO NOTHING
            }

            this.DialogResult = true;
            this.Close();
        }

        #endregion

        
    }


}
