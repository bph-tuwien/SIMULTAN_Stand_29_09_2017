using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Visual = System.Windows.Media.Visual;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.ComponentModel;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using DXFImportExport;
using GeometryViewer.Utils;
using GeometryViewer.EntityGeometry;
using GeometryViewer.HelixToolkitCustomization;

namespace GeometryViewer
{
    public class ArchitectureDisplay : GroupModel3Dext
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================ STATIC FUNCTIONS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC CONSTANTS

        private static readonly double START_MARKER_SIZE = 0.05;
        private static readonly double THICKNESS_DEFAULT = 0.5;
        // private static readonly double THICKNESS_INFO = 0.25;
        private static readonly double THICKNESS_SELECTED_DEFAULT = 2;
        private static readonly float[] LINE_THICKNESS =
            new float[] { 3.0f, 2.75f, 2.5f, 2.25f, 2.0f, 1.75f, 1.5f, 1.25f, 1.0f, 0.75f, 0.5f, 0.25f };
 
        private static readonly Color Color_Selected = Color.Yellow;
        private static readonly Color Color_Inactive = Color.DimGray;

        #endregion

        #region STATIC GEOMETRY CONVERTER: DXF -> Coordinate and Connectedness Arrays

        private static void DXFGeometryToCoords(DXFGeometry _g,
            out List<Point3D> coords0, out List<Point3D> coords1, out List<int> connected)
        {
            coords0 = new List<Point3D>();
            coords1 = new List<Point3D>();
            connected = new List<int>();

            if (_g == null)
                return;

            int nrLines = _g.Coords.Count - 1;
            if (nrLines < 1)
                return;

            // process geometry
            for (int i = 0; i < nrLines; i++)
            {
                // swap Y and Z entries
                coords0.Add(new Point3D(-_g.Coords[i].X, _g.Coords[i].Z, _g.Coords[i].Y));
                coords1.Add(new Point3D(-_g.Coords[i + 1].X, _g.Coords[i + 1].Z, _g.Coords[i + 1].Y));
                connected.Add(connected.Count + 1);
            }
            connected[nrLines - 1] = -1;

            // process text, if there is any    
            int nrTextLines = _g.TextContent.Count;
            Matrix transfText = _g.TextTransf.ToMatrix();
            for (int j = 0; j < nrTextLines; j++)
            {
                Utils.FontAssembler.ConvertTextToGeometry(_g.TextContent[j], transfText,
                                                            ref coords0, ref coords1, ref connected, j);
            }

        }

        #endregion

        #region COLOR CONVERTER
        private static Color DXFColor2Color(DXFColor _color)
        {
            return new Color(_color.R, _color.G, _color.B, _color.A);
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== DEPENDENCY PROPERTIES FOR BINDING ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region ENTITY MANAGER -OK-

        public EntityManager EManager
        {
            get { return (EntityManager)GetValue(EManagerProperty); }
            set { SetValue(EManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EManagerProperty =
            DependencyProperty.Register("EManager", typeof(EntityManager), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MyEManagerPropertyChangedCallback)));

        private static void MyEManagerPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                // publish data to GUI
                ad.ZoneLayers = new List<Layer>(ad.EManager.Layers);
                ad.ZoneLayersFlat = new List<Layer>(ad.EManager.GetFlatLayerList());

                // generate dsiplayable 3D geometric data
                ad.RegenerateGeometry();
            }
        }

        #endregion

        #region DISPLAY in a TreeView -OK-
        public List<Layer> ZoneLayers
        {
            get { return (List<Layer>)GetValue(ZoneLayersProperty); }
            set { SetValue(ZoneLayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZoneLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoneLayersProperty =
            DependencyProperty.Register("ZoneLayers", typeof(List<Layer>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<Layer>()));

        public List<Layer> ZoneLayersFlat
        {
            get { return (List<Layer>)GetValue(ZoneLayersFlatProperty); }
            set { SetValue(ZoneLayersFlatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZoneLayersFlat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoneLayersFlatProperty =
            DependencyProperty.Register("ZoneLayersFlat", typeof(List<Layer>), typeof(ArchitectureDisplay),
             new UIPropertyMetadata(new List<Layer>()));

        public List<float> LineThicknessCollection
        {
            get { return (List<float>)GetValue(LineThicknessCollectionProperty); }
            set { SetValue(LineThicknessCollectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LineThicknessCollection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineThicknessCollectionProperty =
            DependencyProperty.Register("LineThicknessCollection", typeof(List<float>), typeof(ArchitectureDisplay), 
            new UIPropertyMetadata(new List<float>(ArchitectureDisplay.LINE_THICKNESS)));

        #endregion

        #region SELECTION -OK-

        public Entity SelectedEntity
        {
            get { return (Entity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(Entity), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedEntityPropertyChangedCallback),
                                         new CoerceValueCallback(MySelectedEntityCoerceValueCallback)));

        private static void MySelectedEntityPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                GeometricEntity ge = e.NewValue as GeometricEntity;
                Layer lay = e.NewValue as Layer;
                ArchitecturalLine aL = e.NewValue as ArchitecturalLine;

                if (ge != null)
                    ad.LayerOfSelectedEntity = ge.EntityLayer;
                else if (lay != null)
                    ad.LayerOfSelectedEntity = ad.EManager.GetParentLayer(lay);
                else
                    ad.LayerOfSelectedEntity = null;

                if (aL != null)
                    ad.ThicknessOfSelectedEntity = aL.LineThickness;
                else
                    ad.ThicknessOfSelectedEntity = 0.25f;

                // handle transfer of properties from one line to others
                if (aL != null && ad.prop_transf_source != null)
                {
                    aL.LineThickness = ad.prop_transf_source.LineThickness;
                    aL.EntityLayer = ad.prop_transf_source.EntityLayer;
                    aL.ColorByLayer = ad.prop_transf_source.ColorByLayer;
                    aL.EntityColor = ad.prop_transf_source.EntityColor;
                    // publish data to GUI
                    ad.ZoneLayers = new List<Layer>(ad.EManager.Layers);
                    ad.ZoneLayersFlat = new List<Layer>(ad.EManager.GetFlatLayerList());
                    // refresh viewport display
                    ad.UpdateGeometryColor();
                    ad.UpdateGeometryThickness();
                    // reset
                    ad.PropertiesTransferMode = false;
                }

                // update display
                ad.EManager.SelectGeometry(ge);
                ad.UpdateSelectedGeometryDisplay();

            }
        }

        private static object MySelectedEntityCoerceValueCallback(DependencyObject d, object value)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                // do not change selection if we are in EDIT MODE !!!
                if (ad.EditMode)
                    return ad.SelectedEntity;
                else
                    return value;
            }
            return value;
        }


        public Layer LayerOfSelectedEntity
        {
            get { return (Layer)GetValue(LayerOfSelectedEntityProperty); }
            set { SetValue(LayerOfSelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LayerOfSelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayerOfSelectedEntityProperty =
            DependencyProperty.Register("LayerOfSelectedEntity", typeof(Layer), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MyLayerOfSelectedEntityPropertyChangedCallback)));

        private static void MyLayerOfSelectedEntityPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                GeometricEntity sge = ad.SelectedEntity as GeometricEntity;
                Layer lay = ad.SelectedEntity as Layer;
                if (sge != null && ad.LayerOfSelectedEntity != EntityManager.NULL_LAYER && sge.EntityLayer.ID != ad.LayerOfSelectedEntity.ID)
                {
                    sge.EntityLayer = ad.LayerOfSelectedEntity;
                    // publish data to GUI
                    ad.ZoneLayers = new List<Layer>(ad.EManager.Layers);
                    ad.ZoneLayersFlat = new List<Layer>(ad.EManager.GetFlatLayerList());
                    // refresh viewport display
                    ad.UpdateGeometryColor();
                    ad.UpdateGeometryVisibility();
                }
                else if (lay != null && ad.EManager.GetParentLayer(lay) != ad.LayerOfSelectedEntity)
                {
                    //string dest = (ad.LayerOfSelectedEntity == null) ? "null" : ad.LayerOfSelectedEntity.EntityName;
                    //string debug = lay.EntityName + " >> " + dest;
                    bool success = ad.EManager.MoveLayer(lay, ad.LayerOfSelectedEntity);
                    if (success)
                    {
                        // publish data to GUI
                        ad.ZoneLayers = new List<Layer>(ad.EManager.Layers);
                        ad.SelectedEntity = lay; 
                        ad.ZoneLayersFlat = new List<Layer>(ad.EManager.GetFlatLayerList());
                        
                        // refresh viewport display
                        ad.UpdateGeometryColor();
                        ad.UpdateGeometryVisibility();
                    }
                }
            }
        }


        public float ThicknessOfSelectedEntity
        {
            get { return (float)GetValue(ThicknessOfSelectedEntityProperty); }
            set { SetValue(ThicknessOfSelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ThicknessOfSelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThicknessOfSelectedEntityProperty =
            DependencyProperty.Register("ThicknessOfSelectedEntity", typeof(float), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(0.5f, new PropertyChangedCallback(MyThicknessOfSelectedEntityPropertyChangedCallback)));

        private static void MyThicknessOfSelectedEntityPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                ArchitecturalLine aL = ad.SelectedEntity as ArchitecturalLine;
                if (aL != null && aL.LineThickness != ad.ThicknessOfSelectedEntity)
                {
                    aL.LineThickness = ad.ThicknessOfSelectedEntity;
                    // refresh viewport display
                    ad.UpdateGeometryThickness();
                }
            }
        }

        #endregion

        #region EDITING -OK-

        public bool EditMode
        {
            get { return (bool)GetValue(EditModeProperty); }
            set { SetValue(EditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditModeProperty =
            DependencyProperty.Register("EditMode", typeof(bool), typeof(ArchitectureDisplay), 
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyEditModePropertyChangedCallback)));

        private static void MyEditModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                if (ad.EditMode)
                    ad.SetGeometryHitVisibility(false);
                else
                    ad.SetGeometryHitVisibility(true);
            }
        }

        public BoundingBox SelectedGeometryBounds 
        { 
            get 
            {
                if (this.selectedGeometry != null)
                    return this.selectedGeometry.Bounds;
                else
                    return BoundingBox.FromPoints(new Vector3[] { Vector3.Zero });
            } 
        }



        public bool PropertiesTransferMode
        {
            get { return (bool)GetValue(PropertiesTransferModeProperty); }
            set { SetValue(PropertiesTransferModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PropertiesTransferMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PropertiesTransferModeProperty =
            DependencyProperty.Register("PropertiesTransferMode", typeof(bool), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyPropertiesTransferModePropertyChangedCallback)));

        private static void MyPropertiesTransferModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null)
            {
                if (ad.PropertiesTransferMode && ad.SelectedEntity != null)
                {
                    ArchitecturalLine aL = ad.SelectedEntity as ArchitecturalLine;
                    if (aL != null)
                    {
                        ad.prop_transf_source = aL;
                    }
                }
                if (!ad.PropertiesTransferMode)
                {
                    ad.prop_transf_source = null;
                }
            }
        }

        #endregion

        #region SEARCH -OK-

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SearchText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(string.Empty));

        #endregion

        #region DXF Import -OK-

        public List<DXFLayer> InputLayers
        {
            get { return (List<DXFLayer>)GetValue(InputLayersProperty); }
            set { SetValue(InputLayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputLayersProperty =
            DependencyProperty.Register("InputLayers", typeof(List<DXFLayer>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<DXFLayer>(), new PropertyChangedCallback(MyInputLayersPropertyChangedCallback)));

        private static void MyInputLayersPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null && ad.InputLayers != null && ad.InputLayers.Count > 0)
            {
                // add new layers
                foreach (DXFLayer layer in ad.InputLayers)
                {
                    Layer eLayer = new Layer(layer.EntName, ArchitectureDisplay.DXFColor2Color(layer.LayerColor));
                    eLayer.Visibility = (layer.entIsVisible) ? EntityVisibility.ON : EntityVisibility.OFF;
                    // do not import duplicate layers, use the ones already in the list
                    ad.EManager.AddLayer(eLayer, false);
                }
                ad.UpdateAfterAddingGeometry();
            }
        }

        public List<DXFGeometry> InputGeom
        {
            get { return (List<DXFGeometry>)GetValue(InputGeomProperty); }
            set { SetValue(InputGeomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputGeom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputGeomProperty =
            DependencyProperty.Register("InputGeom", typeof(List<DXFGeometry>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<DXFGeometry>(), new PropertyChangedCallback(MyInputGeomPropertyChangedCallback)));

        private static void MyInputGeomPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ArchitectureDisplay ad = d as ArchitectureDisplay;
            if (ad != null && ad.InputGeom != null && ad.InputGeom.Count > 0)
            {
                // reset the existing lines for the OcTree
                ad.ArcLinesToLinesWHist();
                // add new geometry
                foreach (DXFGeometry g in ad.InputGeom)
                {
                    ad.AddArchLine(g, false);
                }
                ad.UpdateAfterAddingGeometry();
                // transfer new information for the OcTree
                ad.LinesWHistToOcTree();
            }
        }
        

        #endregion

        #region GEOMETRY TRANSFER: -TO- building physics (ZoneGroupDisplay)

        private List<Point3D> polygonChain;
        public List<Point3D> PolygonChain
        {
            get { return polygonChain; }
            set
            {
                polygonChain = value;
                base.RegisterPropertyChanged("PolygonChain");
            }
        }

        #endregion

        #region GEOMETRY TRANSFER: -TO- line editing (LineGenerator3D)

        private List<Point3D> coords0ToLG;
        public List<Point3D> Coords0ToLG
        {
            get { return coords0ToLG; }
            set
            {
                coords0ToLG = value;
                base.RegisterPropertyChanged("Coords0ToLG");
            }
        }

        private List<Point3D> coords1ToLG;
        public List<Point3D> Coords1ToLG
        {
            get { return coords1ToLG; }
            set
            {
                coords1ToLG = value;
                base.RegisterPropertyChanged("Coords1ToLG");
            }
        }

        private List<int> connectedToLG;
        public List<int> ConnectedToLG
        {
            get { return connectedToLG; }
            set
            {
                connectedToLG = value;
                base.RegisterPropertyChanged("ConnectedToLG");
            }
        }

        #endregion

        #region GEOMETRY TRANSFER: -FROM- line editing (LineGenerator3D)

        public List<Point3D> Coords0FromLG
        {
            get { return (List<Point3D>)GetValue(Coords0FromLGProperty); }
            set { SetValue(Coords0FromLGProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Coords0FromLG.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Coords0FromLGProperty =
            DependencyProperty.Register("Coords0FromLG", typeof(List<Point3D>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<Point3D>()));

        public List<Point3D> Coords1FromLG
        {
            get { return (List<Point3D>)GetValue(Coords1FromLGProperty); }
            set { SetValue(Coords1FromLGProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Coords1FromLG.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Coords1FromLGProperty =
            DependencyProperty.Register("Coords1FromLG", typeof(List<Point3D>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<Point3D>()));

        public List<int> ConnectedFromLG
        {
            get { return (List<int>)GetValue(ConnectedFromLGProperty); }
            set { SetValue(ConnectedFromLGProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConnectedFromLG.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectedFromLGProperty =
            DependencyProperty.Register("ConnectedFromLG", typeof(List<int>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<int>()));


        #endregion

        #region GEOMETRY TRANSFER: -TO- OcTree

        public List<Utils.LineWHistory> LinesToOcTree
        {
            get { return (List<Utils.LineWHistory>)GetValue(LinesToOcTreeProperty); }
            set { SetValue(LinesToOcTreeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinesToOcTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinesToOcTreeProperty =
            DependencyProperty.Register("LinesToOcTree", typeof(List<Utils.LineWHistory>), typeof(ArchitectureDisplay),
            new UIPropertyMetadata(new List<Utils.LineWHistory>()));

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS MEMBERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CLASS MEMBERS

        // geometry internal
        private List<Layer> allLayers;
        private List<GeometricEntity> allGE;
        private LineGeometryModel3D selectedGeometry;
        private List<List<Utils.LineWHistory>> linesWH;

        private List<System.Windows.Visibility> childrenVisibility_prev;

        // selection
        public bool MouseSelectedNew { get; set; }
        public ICommand SelectNoneCmd { get; private set; }

        // data management
        public ICommand SearchByNameCmd { get; private set; }
        public ICommand SwitchVisibilityCmd { get; private set; }
        public ICommand ChangeColorCmd { get; private set; }

        public ICommand CreateNewLayerCmd { get; private set; }
        public ICommand DeleteSelectedEntityCmd { get; private set; }
        public ICommand CopySelectedEntityCmd { get; private set; }
        public ICommand SwitchVisibilityForAllCmd { get; private set; }

        public ICommand SortLayersCmd { get; private set; }

        // geoemtry transfer
        public ICommand TransferSingleToZoneGroupDisplCmd { get; private set; }
        public ICommand TransferSingleToLineGeneratorCmd { get; private set; }
        public ICommand UnpackSingleFromLineGeneratorCmd { get; private set; }

        // text editing
        public ICommand UpdateAfterTextEditCmd { get; private set;}

        // object properties transfer
        private ArchitecturalLine prop_transf_source;

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== INITIALIZATION ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INIT

        public ArchitectureDisplay()
        {
            // prepare for selection
            this.MouseSelectedNew = false;          
            this.SelectNoneCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSelectNoneCommand(),
                                                               (x) => CanExecute_OnSelectNoneCommand());
            // COMMANDS: data management
            this.SearchByNameCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSearchByNameCommand(x));
            this.SwitchVisibilityCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchVisibilityCommand(x));
            this.ChangeColorCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnChangeColorCommand(x));

            this.CreateNewLayerCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnCreateNewLayerCommand());
            this.DeleteSelectedEntityCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnDeleteSelectedEntity(),
                                                                          (x) => CanExecute_OnDeleteSelectedEntity(x));
            this.CopySelectedEntityCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnCopyEntityCommand(),
                                                                        (x) => CanExecute_OnCopyEntityCommand());
            this.SwitchVisibilityForAllCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchAllLayersCommand(x));

            this.SortLayersCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSortLayersCommand());

            // COMMANDS: geometry transfer
            this.TransferSingleToZoneGroupDisplCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferSingleToZoneGroupDisplay(),
                                                                                    (x) => CanExecute_OnTransferSingleToZoneGroupDisplay());

            this.TransferSingleToLineGeneratorCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferSingleToLineGenerator(),
                                                                             (x) => CanExecute_OnTransferSingleToLineGenerator());
            this.UnpackSingleFromLineGeneratorCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnUnpackSingleFromLineGenerator(),
                                                                             (x) => CanExecute_OnUnpackSingleFromLineGenerator());


            // COMMANDS: text editing
            this.UpdateAfterTextEditCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => UpdateGeometry(x));

            // data management
            this.prop_transf_source = null;

            // prepare for communication with the OcTree
            this.ArcLinesToLinesWHist();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================ CLASS DATA CONVERSION AND TRANSFER METHODS ============================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region DXF -> ArchitecturalLines

        private void AddArchLine(DXFGeometry _g, bool _updateDisplay = false)
        {
            if (_g == null)
                return;

            string name = _g.Name;
            string blockName, elementName;
            this.GetNameComponents(name, out blockName, out elementName);

            Layer layer = this.EManager.Layers.Find(x => x.EntityName == _g.LayerName);

            List<Point3D> coords0, coords1;
            List<int> connected;
            ArchitectureDisplay.DXFGeometryToCoords(_g, out coords0, out coords1, out connected);
            if (coords0.Count < 1)
                return;

            ArchitecturalLine aLine;
            if (layer == null)
            {
                if (blockName != null && blockName != string.Empty && elementName != null && elementName != string.Empty)
                {
                    Layer block = EManager.FindLayerByNameAndParent(blockName, EManager.Layers[0]);
                    if (block == null)
                    {
                        block = new Layer(blockName, ArchitectureDisplay.DXFColor2Color(_g.Color));
                        EManager.Layers[0].AddEntity(block);
                    }
                    aLine = ArchitecturalLine.CreateArchitecturalEntity(elementName, block, coords0, coords1, connected, 
                                                _g.TextContent, _g.TextTransf.ToMatrix());
                }
                else
                {
                    aLine = ArchitecturalLine.CreateArchitecturalEntity(name, this.EManager.Layers[0], coords0, coords1, connected,
                                                _g.TextContent, _g.TextTransf.ToMatrix());
                }
            }
            else
            {
                if (blockName != null && blockName != string.Empty && elementName != null && elementName != string.Empty)
                {
                    Layer block = EManager.FindLayerByNameAndParent(blockName, layer);
                    if (block == null)
                    {
                        block = new Layer(blockName, ArchitectureDisplay.DXFColor2Color(_g.Color));
                        layer.AddEntity(block);
                    }
                    aLine = ArchitecturalLine.CreateArchitecturalEntity(elementName, block, coords0, coords1, connected,
                                                _g.TextContent, _g.TextTransf.ToMatrix());
                }
                else
                {
                    aLine = ArchitecturalLine.CreateArchitecturalEntity(name, layer, coords0, coords1, connected,
                                                _g.TextContent, _g.TextTransf.ToMatrix());
                }
            }

            aLine.EntityColor = ArchitectureDisplay.DXFColor2Color(_g.Color);
            if (aLine.EntityColor != aLine.EntityLayer.EntityColor)
                aLine.ColorByLayer = false;

            aLine.LineThickness = (_g.Width.Count > 0 && _g.Width[0] > 0) ? _g.Width[0] : (float)ArchitectureDisplay.THICKNESS_DEFAULT;

            if (_updateDisplay)
                this.UpdateAfterAddingGeometry();

            // save info for the OcTree (the transfer happens in the calling method)
            ArchitecturalText aT = aLine as ArchitecturalText;
            if (aT == null)
            {
                List<Utils.LineWHistory> lwh = NewCoordsToLinesWHist(coords0, coords1, this.getFlatLinesWHistNr());
                this.linesWH.Add(lwh);
            }
        }

        private void GetNameComponents(string _name, out string blockName, out string elementName)
        {
            blockName = string.Empty;
            elementName = string.Empty;
            if (_name == null || _name.Count() < 5)
            {
                elementName = _name;
                return;
            }

            string start = _name.Substring(0, 5);
            if (start != "Block")
            {
                elementName = _name;
                return;
            }

            string[] components = _name.Split(new char[]{'\'', ':'});
            if (components.Count() == 4)
            {
                blockName = components[1] + " " + components[2];
                elementName = components[3];
            }
        }

        #endregion

        #region BULK GUI AND DISPLAY UPDATE METHODS
        private void UpdateAfterAddingGeometry()
        {
            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());

            // regenerate viewport display data
            this.RegenerateGeometry();
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== CLASS GEOMETRY MANAGEMENT METHODS ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region UPDATE 3D GEOMETRY DISPLAY

        private void UpdateGeometry(object _entityID)
        {
            long ID;
            bool success = long.TryParse(_entityID.ToString(), out ID);
            if (success)
                UpdateGeometry(ID);
        }

        private void UpdateGeometry(long _entityID)
        {
            for (int i = 0; i < this.allGE.Count; i++)
            {
                if (this.allGE[i].ID == _entityID)
                {
                    ArchitecturalLine aL = this.allGE[i] as ArchitecturalLine;
                    if (aL != null)
                    {
                        LineGeometryModel3D model = this.Children[i] as LineGeometryModel3D;
                        if (model != null)
                            model.Geometry = aL.Build(START_MARKER_SIZE);
                    }
                    break;
                }
            }
        }

        private void RegenerateGeometry()
        {
            this.allLayers = this.EManager.GetFlatLayerList();
            this.allGE = this.EManager.GetFlatGeometryList();
            if (this.allGE == null)
                return;
            int n = this.allGE.Count;
            if (n < 1)
                return;

            this.Children.Clear();

            // show the architectural lines
            foreach (GeometricEntity ge in allGE)
            {
                ArchitecturalLine aL = ge as ArchitecturalLine;
                if (aL != null)
                    AddArchLineGeometryDisplay(aL);
            }
            // show the selected line
            AddSelectedGeomDisplay();

            // RE-ATTACH TO RENDERER !!!
            if (this.renderHost != null)
                this.Attach(this.renderHost);
        }

        private void UpdateGeometryVisibility()
        {
            if (this.allGE == null)
                return;

            long selTag = -1;
            if (this.selectedGeometry != null)
                long.TryParse(this.selectedGeometry.Tag.ToString(), out selTag);

            // record previous visibiliy state (for OBJECT SNAP via LINES WITH HISTORY)
            this.childrenVisibility_prev = new List<Visibility>();
            foreach(var child in this.Children)
            {
                this.childrenVisibility_prev.Add(child.Visibility);
            }

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {
                // all geometry
                this.Children[i].Visibility =
                    (this.allGE[i].Visibility == EntityVisibility.ON) ? Visibility.Visible : Visibility.Collapsed;
                SelectableUserLine sul = this.Children[i] as SelectableUserLine;
                if (sul != null)
                    sul.IsHitTestVisible = (this.allGE[i].Visibility == EntityVisibility.ON) ? true : false;

                // architectural lines for object snap (VIA LINES WITH HISTORY)
                ArchitecturalLine aL = this.allGE[i] as ArchitecturalLine;
                if (aL != null)
                {
                    if (this.childrenVisibility_prev[i] != Visibility.Visible && this.Children[i].Visibility == Visibility.Visible)
                    {
                        this.VisibilityChangedCoordsToLinesWHist(aL, i, true, false);
                    }
                    else if (this.childrenVisibility_prev[i] == Visibility.Visible && this.Children[i].Visibility != Visibility.Visible)
                    {
                        this.VisibilityChangedCoordsToLinesWHist(aL, i, false, false);
                    }
                }
                
                // the selected geometry
                if (this.allGE[i].ID == selTag)
                {
                    if (this.allGE[i].Visibility != EntityVisibility.ON)
                    {
                        this.selectedGeometry.Geometry = null;
                        this.selectedGeometry.Tag = -1;
                    }
                }
            }

            // debug START
            string vis_prev = "";
            string vis_current = "";
            int nrCh = this.Children.Count;
            for (int j = 0; j < nrCh; j++)
            {
                // read out the previous ones:
                vis_prev += " " + this.childrenVisibility_prev[j].ToString();
                // save the current ones:
                vis_current += " " + this.Children[j].Visibility.ToString();
            }
            string debug = vis_prev + "\n" + vis_current;
            var test = this.linesWH;
            // debug END

            // communicate to OcTree
            this.LinesWHistToOcTree();
        }

        private void UpdateGeometryColor()
        {
            if (this.allGE == null)
                return;

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {
                LineGeometryModel3D lgm = this.Children[i] as LineGeometryModel3D;
                if (lgm != null)
                {
                    GeometricEntity ge = this.allGE[i] as GeometricEntity;
                    if (ge != null)
                        ge.EntityColor = (ge.ColorByLayer) ? this.allGE[i].EntityLayer.EntityColor : this.allGE[i].EntityColor;
                    
                    lgm.Color = this.allGE[i].EntityColor;
                }    
            }
        }

        private void UpdateGeometryThickness()
        {
            if (this.allGE == null)
                return;

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {
                LineGeometryModel3D lgm = this.Children[i] as LineGeometryModel3D;
                if (lgm != null)
                {
                    ArchitecturalLine aL = this.allGE[i] as ArchitecturalLine;
                    if (aL != null)
                        lgm.Thickness = aL.LineThickness;
                }
            }
        }

        private void SetGeometryHitVisibility(bool _isVisible)
        {
            if (this.allGE == null)
                return;

            long selTag = -1;
            long.TryParse(this.selectedGeometry.Tag.ToString(), out selTag);

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {
                LineGeometryModel3D lgm = this.Children[i] as LineGeometryModel3D;
                if (lgm != null)
                {
                    lgm.IsHitTestVisible = _isVisible;
                }

                // the selected geometry
                if (this.allGE[i].ID == selTag)
                {
                    this.selectedGeometry.Visibility = (_isVisible) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

        }


        #endregion

        #region UPDATE SELECTED 3D GEOMETRY DSIPLAY ?

        private void UpdateSelectedGeometryDisplay()
        {
            if (this.selectedGeometry == null)
                return;
            
            if (this.SelectedEntity != null)
            {
                ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
                if (aL != null)
                {
                    this.selectedGeometry.Geometry = aL.BuildSelectionGeometry();
                    this.selectedGeometry.Thickness = aL.LineThickness * 2f;
                    this.selectedGeometry.Tag = this.SelectedEntity.ID;
                    return;
                }
            }

            this.selectedGeometry.Geometry = null;
            this.selectedGeometry.Tag = -1;
        }

        #endregion

        #region 3D GEOMETRY DEFINITIONS

        private void AddArchLineGeometryDisplay(ArchitecturalLine _aL)
        {
            if (_aL == null)
                return;

            // line
            SelectableUserLine aLg = new SelectableUserLine()
            {
                Geometry = _aL.Build(START_MARKER_SIZE),
                Color = _aL.EntityColor,
                Thickness = _aL.LineThickness,
                Visibility = (_aL.Visibility == EntityVisibility.ON) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = (_aL.Visibility == EntityVisibility.ON) ? true : false,
                HitTestThickness = 3,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = _aL.ID,
            };
            aLg.MouseDown3D += aLine_MouseDown3D;
            aLg.MouseUp3D += aLine_MouseUp3D;
            this.Children.Add(aLg);
        }

        private void AddSelectedGeomDisplay()
        {
            this.selectedGeometry = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color_Selected,
                Thickness = THICKNESS_SELECTED_DEFAULT,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = -1,
            };
            this.Children.Add(this.selectedGeometry);
        }

        #endregion

        #region UPDATE GEOMETRY INFO FOR OCTREE

        private static List<Utils.LineWHistory> NewCoordsToLinesWHist(List<Point3D> _coords0, List<Point3D> _coords1, int _startIndex)
        {
            List<Utils.LineWHistory> result = new List<LineWHistory>();

            if (_coords0 == null || _coords1 == null)
                return result;

            int n = _coords0.Count;
            int m = _coords1.Count;
            if (n != m)
                return result;

            for (int i = 0; i < n; i++)
            {
                Utils.LineWHistory line = new Utils.LineWHistory(_startIndex + i, _coords0[i].ToVector3(),
                                                                                  _coords1[i].ToVector3(),
                                                                                  LineChange.DRAWN);
                result.Add(line);
            }

            return result;
        }

        private void EditedCoordsToLinesWHist(ArchitecturalLine _aL, int _atIndex, bool _resetOtherLines = true)
        {
            if (_aL == null)
                return;
            ArchitecturalText aT = _aL as ArchitecturalText;
            if (aT != null)
                return;

            // reset state of lines w hist
            if (_resetOtherLines)
                this.ArcLinesToLinesWHist();

            int n = this.linesWH.Count;
            if (_atIndex > -1 && _atIndex < n)
            {
                //// OLD: mark lines as edited (includes marking as drawn or deleted)
                //LineWHistory.editLinesWHistAt(ref this.linesWH, _atIndex, _aL.Coords0, _aL.Coords1);

                // NEW: 
                LineWHistory.deleteLinesWHistAt(ref this.linesWH, _atIndex);
                LineWHistory.drawLinesWHistAt(ref this.linesWH, n, _aL.Coords0, _aL.Coords1);
            }
            else
            {
                // add new lines
                if (n > 0)
                {
                    LineWHistory.drawLinesWHistAt(ref this.linesWH, n, _aL.Coords0, _aL.Coords1);
                }
            }

        }

        // _off2on = ture: line becomes visible (also for OBJECT SNAP)
        // _off2on = false: line becomes INvisible (also for OBJECT SNAP)
        // DO NOT CALL THIS METHOD if the line visiblilty has not changed !
        private void VisibilityChangedCoordsToLinesWHist(ArchitecturalLine _aL, int _atIndex, bool _off2on, bool _resetOtherLines = true)
        {
            if (_aL == null)
                return;
            ArchitecturalText aT = _aL as ArchitecturalText;
            if (aT != null)
                return;

            // reset state of lines w hist
            if (_resetOtherLines)
                this.ArcLinesToLinesWHist();

            int n = this.linesWH.Count;
            if (_off2on)
            {
                LineWHistory.drawLinesWHistAt(ref this.linesWH, _atIndex, _aL.Coords0, _aL.Coords1);
            }
            else
            {
                if (_atIndex > -1 && _atIndex < n)
                    LineWHistory.deleteLinesWHistAt(ref this.linesWH, _atIndex);
            }
        }

        private void DeletedCoordsToLinesWHist(int _atIndex, bool _resetOtherLines = true)
        {
            // reset state of lines w hist
            if (_resetOtherLines)
                this.ArcLinesToLinesWHist();

            int n = this.linesWH.Count;
            if (_atIndex > -1 && _atIndex < n)
            {
                // mark lines as deleted
                LineWHistory.deleteLinesWHistAt(ref this.linesWH, _atIndex);
            }
            
        }

        private void ArcLinesToLinesWHist()
        {
            this.linesWH = new List<List<Utils.LineWHistory>>();

            if (this.allGE == null || this.allGE.Count < 1)
                return;

            for (int i = 0; i < this.allGE.Count; i++)
            {
                ArchitecturalLine aL = this.allGE[i] as ArchitecturalLine;
                ArchitecturalText aT = this.allGE[i] as ArchitecturalText;
                if (aL == null || aT != null)
                    continue;

                int m = aL.Coords0.Count;
                List<Utils.LineWHistory> tmp = new List<Utils.LineWHistory>();
                for (int j = 0; j < m; j++)
                {
                    Utils.LineWHistory line = new Utils.LineWHistory(j, aL.Coords0[j].ToVector3(),
                                                                        aL.Coords1[j].ToVector3());
                    tmp.Add(line);
                }
                this.linesWH.Add(tmp);
            }
        }

        private void LinesWHistToOcTree(bool _purge_empty_lists = false)
        {
            // flatten the list of lists to a simple list
            List<Utils.LineWHistory> flatList = new List<LineWHistory>();
            foreach (var list in this.linesWH)
            {
                flatList.AddRange(list);
            }
            // transfer
            this.LinesToOcTree = new List<LineWHistory>(flatList);
            // reset internal representation
            LineWHistory.removeLinesWHistDeleted(ref this.linesWH, _purge_empty_lists);
            LineWHistory.resetHistLinesWHist(ref this.linesWH);
        }

        private int getFlatLinesWHistNr()
        {
            int nr = 0;
            foreach (var list in this.linesWH)
            {
                nr += list.Count;
            }
            return nr;
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= COMMANDS ============================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMANDS: Visibility, Color

        private void OnSwitchVisibilityCommand(object _e)
        {
            if (_e != null)
            {
                Entity ent = _e as Entity;
                if (ent != null)
                {
                    // process entity
                    EntityVisibility vis = ent.Visibility;
                    if (vis == EntityVisibility.ON)
                        ent.Visibility = EntityVisibility.OFF;
                    else if (vis == EntityVisibility.OFF)
                        ent.Visibility = EntityVisibility.ON;
                    // update geometry
                    UpdateGeometryVisibility();
                }
            }
        }

        private void OnChangeColorCommand(object _e)
        {
            if (_e != null)
            {
                Entity ent = _e as Entity;
                if (ent != null)
                {
                    Window window = Window.GetWindow(this);
                    if (window != null)
                    {
                        MainWindow mw = window as MainWindow;
                        if (mw != null)
                        {
                            System.Windows.Media.Color pickedCol;
                            int pickedInd = -1;
                            mw.OpenColorPicker(out pickedCol, out pickedInd);
                            
                            // process
                            ent.EntityColor = new SharpDX.Color(pickedCol.R, pickedCol.G, pickedCol.B, pickedCol.A);
                            GeometricEntity ge = ent as GeometricEntity;
                            if (ge != null)
                            {
                                if (pickedInd == -1)
                                    ge.ColorByLayer = true;
                                else
                                    ge.ColorByLayer = false;
                            }

                            // update geometry
                            UpdateGeometryColor();
                        }
                    }
                }
            }
        }

        private void OnSwitchAllLayersCommand(object _on)
        {
            if (_on == null)
                return;
            if (_on is bool)
            {
                bool switch_on = (bool)_on;
                foreach (Layer layer in this.allLayers)
                {
                    layer.Visibility = switch_on ? EntityVisibility.ON : EntityVisibility.OFF;
                }
                // update geometry
                UpdateGeometryVisibility();
            }
        }

        #endregion

        #region COMMANDS: Programmatical Deselection

        private void OnSelectNoneCommand()
        {
            this.SelectedEntity = null;
        }

        private bool CanExecute_OnSelectNoneCommand()
        {
            return (this.SelectedEntity != null);
        }

        #endregion

        #region COMMANDS: Search by Name, Sort by Name
        private void OnSearchByNameCommand(object _name)
        {
            if (_name == null)
                return;

            string strName = _name.ToString();
            if (strName == string.Empty)
                return;

            this.EManager.ProcessSearch(strName);
        }

        private void OnSortLayersCommand()
        {
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
        }

        #endregion

        #region COMMANDS: New, Delete, Copy

        private void OnCreateNewLayerCommand()
        {
            Layer parent = null;
            if (this.SelectedEntity != null)
            {
                parent = this.SelectedEntity as Layer;
            }

            Layer newLayer = new Layer();
            if (parent == null)
                this.EManager.AddLayer(newLayer);
            else
                parent.AddEntity(newLayer);

            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
            // regenerate viewport display data
            RegenerateGeometry();
        }

        private void OnDeleteSelectedEntity()
        {
            string message = "Do you really want to delete " + this.SelectedEntity.EntityName + " ?";
            string caption = "Deleting Object: " + this.SelectedEntity.EntityName;
            MessageBoxResult answer = MessageBox.Show(message, caption,
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer == MessageBoxResult.Yes)
            {
                bool success = this.EManager.RemoveEntity(this.SelectedEntity);
                if (success)
                {
                    // communicate to OcTree
                    ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
                    ArchitecturalText aT = this.SelectedEntity as ArchitecturalText;
                    if (aL != null & aT == null)
                    {
                        this.DeletedCoordsToLinesWHist(this.EManager.SelectedGeomIndex);
                        this.LinesWHistToOcTree(true);
                    }

                    this.SelectedEntity = null;
                    // publish data to GUI
                    this.ZoneLayers = new List<Layer>(this.EManager.Layers);
                    this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
                    // regenerate viewport display data
                    RegenerateGeometry(); 
                }
            }
        }

        // more than one type can be saparated by a '+'
        private bool CanExecute_OnDeleteSelectedEntity(object _o)
        {
            if (this.SelectedEntity == null)
                return false;

            if (_o == null)
                return true;

            string str_param = _o.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
            {
                Type targetType = Type.GetType(_o.ToString());
                if (targetType != null && this.SelectedEntity.GetType() == targetType)
                    return true;
                else
                    return false;
            }
            else
            {
                foreach (string strType in str_params_OR)
                {
                    Type targetType = Type.GetType(strType.ToString());
                    if (targetType != null && this.SelectedEntity.GetType() == targetType)
                        return true;
                }
                return false;
            }
        }

        private void OnCopyEntityCommand()
        {
            ArchitecturalLine original = this.SelectedEntity as ArchitecturalLine;
            if (original != null)
            {
                ArchitecturalLine copy = new ArchitecturalLine(original);
                this.SelectedEntity = copy;
                this.EManager.SelectGeometry(copy);
                this.UpdateAfterAddingGeometry();

                ArchitecturalText aT = this.SelectedEntity as ArchitecturalText;
                if (aT == null)
                {
                    // save for the OcTree
                    List<Utils.LineWHistory> lwh = NewCoordsToLinesWHist(copy.Coords0, copy.Coords1, this.getFlatLinesWHistNr());
                    this.linesWH.Add(lwh);
                    // transfer to OcTree
                    LinesWHistToOcTree();
                }
            }
        }

        private bool CanExecute_OnCopyEntityCommand()
        {
            ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
            return (aL != null);
        }


        #endregion

        #region COMMANDS: [Geometry Transfer] -TO- building physics (ZoneGroupDisplay)

        private void OnTransferSingleToZoneGroupDisplay()
        {
            ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
            if (aL == null)
                return;

            // extract polygon chain
            List<Point3D> tmpPolygon = aL.ExtractPolygonChain();

            // refill OUT container
            this.PolygonChain = new List<Point3D>(tmpPolygon);
        }

        private bool CanExecute_OnTransferSingleToZoneGroupDisplay()
        {
            ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
            return (aL != null);
        }

        #endregion

        #region COMMANDS: [Geometry Transfer] -TO- and -FROM- line editing (LineGenerator3D)

        private void OnTransferSingleToLineGenerator()
        {
            ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
            if (aL == null)
                return;

            // refill OUT containers
            this.Coords0ToLG = new List<Point3D>(aL.Coords0);
            this.Coords1ToLG = new List<Point3D>(aL.Coords1);
            this.ConnectedToLG = new List<int>(aL.Connected);

            this.EditMode = true;
        }

        private bool CanExecute_OnTransferSingleToLineGenerator()
        {
            ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
            return (aL != null);
        }

        private void OnUnpackSingleFromLineGenerator()
        {
            // check if the input is valid
            int n = Coords0FromLG.Count;
            int m = Coords1FromLG.Count;
            int k = ConnectedFromLG.Count;

            if (n != m || n != k)
                return;

            // internal unpacking
            ArchitecturalLine aL = this.SelectedEntity as ArchitecturalLine;
            if (this.EditMode && aL != null)
            {
                // adjust existing line (even if the containers are empty!)
                aL.Coords0 = new List<Point3D>(Coords0FromLG);
                aL.Coords1 = new List<Point3D>(Coords1FromLG);
                aL.Connected = new List<int>(ConnectedFromLG);

                // save for OcTree
                var test = this.Coords0ToLG;
                this.EditedCoordsToLinesWHist(aL, this.EManager.SelectedGeomIndex);
            }
            else
            {
                // create new line (no text can land here,because it is excluded from editing)
                ArchitecturalLine newLine = new ArchitecturalLine(this.EManager.Layers[0], 
                                                    Coords0FromLG, Coords1FromLG, ConnectedFromLG);
                newLine.LineThickness = (float) ArchitectureDisplay.THICKNESS_DEFAULT;
                
                this.SelectedEntity = newLine;
                this.EManager.SelectGeometry(newLine);

                // save for the OcTree
                List<Utils.LineWHistory> lwh = NewCoordsToLinesWHist(newLine.Coords0, newLine.Coords1, this.getFlatLinesWHistNr());
                this.linesWH.Add(lwh);
            }

            // communicate to display
            this.UpdateAfterAddingGeometry();
            this.EditMode = false;

            // communicate to OcTree
            this.LinesWHistToOcTree();
        }

        private bool CanExecute_OnUnpackSingleFromLineGenerator()
        {
            bool canExecute = (this.EditMode || this.Coords0FromLG.Count > 0);
            if (!canExecute)
            {
                MessageBox.Show("Currently there are no elements in edit mode.\nChoose an element for editing from the list.",
                                "Edit Geometry", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return canExecute;
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================= EVENT HANDLERS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MOUSE: Selection

        private void aLine_MouseDown3D(object sender, RoutedEventArgs e)
        {
            Viewport3DXext vpext = this.Parent as Viewport3DXext;
            if (vpext != null && vpext.ActionMode != Communication.ActionType.ARCHITECTURE)
                return;
            
            SelectableUserLine model = sender as SelectableUserLine;
            if (model != null && model.Tag != null)
            {
                // selection handling: polygons
                long tag = -1;
                long.TryParse(model.Tag.ToString(), out tag);
                foreach (GeometricEntity ge in this.allGE)
                {
                    if (ge.ID == tag)
                    {
                        this.SelectedEntity = ge;
                        break;
                    }
                }
                this.MouseSelectedNew = true;
            }

        }

        private void aLine_MouseUp3D(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= UTILS ================================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////


    }
}
