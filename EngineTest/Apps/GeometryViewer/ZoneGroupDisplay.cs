using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.EntityGeometry;
using GeometryViewer.Communication;
using GeometryViewer.HelixToolkitCustomization;
using GeometryViewer.ComponentInteraction;
using GeometryViewer.EntityDXF;

namespace GeometryViewer
{
    public class ZoneGroupDisplay : GroupModel3Dext
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================================== DEPENDENCY PROPERTIES ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region TEXTURES

        public BitmapSource OpacityMap
        {
            get { return (BitmapSource)GetValue(OpacityMapProperty); }
            set { SetValue(OpacityMapProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OpacityMap.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OpacityMapProperty =
            DependencyProperty.Register("OpacityMap", typeof(BitmapSource), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(null));

        #endregion

        #region ENTITY MANAGER -OK- | MATERIAL MANAGER -OK- 

        public EntityManager EManager
        {
            get { return (EntityManager)GetValue(EManagerProperty); }
            set { SetValue(EManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EManagerProperty =
            DependencyProperty.Register("EManager", typeof(EntityManager), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MyEManagerPropertyChangedCallback),
                new CoerceValueCallback(MyEManagerPropertyCoerceValueCallback)));

        private static object MyEManagerPropertyCoerceValueCallback(DependencyObject d, object baseValue)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd == null) return baseValue;

            if (zgd.EManager != null)
            {
                // detach event handlers from the zoned volumes (added 30.11.2016)
                List<GeometricEntity> list_geom = zgd.EManager.GetFlatGeometryList();
                foreach (GeometricEntity ge in list_geom)
                {
                    ZonedVolume zv = ge as ZonedVolume;
                    if (zv == null) continue;

                    zv.PropertyChanged -= zgd.volume_PropertyChanged;
                }
            }

            return baseValue;
        }

        private static void MyEManagerPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                zgd.UpdateAfterGeometryManagerChange();
            }
        }


        public bool MyEManagerContentChange
        {
            get { return (bool)GetValue(MyEManagerContentChangeProperty); }
            set { SetValue(MyEManagerContentChangeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyEManagerContentChange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyEManagerContentChangeProperty =
            DependencyProperty.Register("MyEManagerContentChange", typeof(bool), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(false, new PropertyChangedCallback(MyEManagerContentChangePropertyChangedCallback)));

        private static void MyEManagerContentChangePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd == null) return;

            if (zgd.EManager == null)
                zgd.SomeGeometryLoaded = false;
            else
                zgd.SomeGeometryLoaded = (zgd.EManager.GetFlatGeometryList().Count() > 0);
        }

        public bool SomeGeometryLoaded
        {
            get { return (bool)GetValue(SomeGeometryLoadedProperty); }
            set { SetValue(SomeGeometryLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SomeGeometryLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SomeGeometryLoadedProperty =
            DependencyProperty.Register("SomeGeometryLoaded", typeof(bool), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(false));


        public bool VolumeVisiblityChanged
        {
            get { return (bool)GetValue(VolumeVisiblityChangedProperty); }
            set { SetValue(VolumeVisiblityChangedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VolumeVisiblityChanged.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VolumeVisiblityChangedProperty =
            DependencyProperty.Register("VolumeVisiblityChanged", typeof(bool), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(false));

        public MaterialManager MLManager
        {
            get { return (MaterialManager)GetValue(MLManagerProperty); }
            set { SetValue(MLManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MLManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MLManagerProperty =
            DependencyProperty.Register("MLManager", typeof(MaterialManager), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MyMLManagerPropertyChangedCallback)));

        private static void MyMLManagerPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                zgd.UpdateAfterMaterialMangerChange();
            }
        }
        
        #endregion

        #region Display in a TreeView -OK-

        // ---------------------------------- LAYER MANAGEMENT ---------------------------------- //

        public List<Layer> ZoneLayers
        {
            get { return (List<Layer>)GetValue(ZoneLayersProperty); }
            set { SetValue(ZoneLayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZoneLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoneLayersProperty =
            DependencyProperty.Register("ZoneLayers", typeof(List<Layer>), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(new List<Layer>()));

        public List<Layer> ZoneLayersFlat
        {
            get { return (List<Layer>)GetValue(ZoneLayersFlatProperty); }
            set { SetValue(ZoneLayersFlatProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZoneLayersFlat.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoneLayersFlatProperty =
            DependencyProperty.Register("ZoneLayersFlat", typeof(List<Layer>), typeof(ZoneGroupDisplay),
             new UIPropertyMetadata(new List<Layer>()));

        // ---------------------------------- POLYGON EDITING ----------------------------------- //

        public List<ZonedPolygonVertexVis> ZonePolygonVertices
        {
            get { return (List<ZonedPolygonVertexVis>)GetValue(ZonePolygonVerticesProperty); }
            set { SetValue(ZonePolygonVerticesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZonePolygonVertices.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZonePolygonVerticesProperty =
            DependencyProperty.Register("ZonePolygonVertices", typeof(List<ZonedPolygonVertexVis>), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(new List<ZonedPolygonVertexVis>()));


        public List<ZoneOpeningVis> ZonePolygonOpenings
        {
            get { return (List<ZoneOpeningVis>)GetValue(ZonePolygonOpeningsProperty); }
            set { SetValue(ZonePolygonOpeningsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZonePolygonOpenings.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZonePolygonOpeningsProperty =
            DependencyProperty.Register("ZonePolygonOpenings", typeof(List<ZoneOpeningVis>), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(new List<ZoneOpeningVis>()));

        // ----------------------------------- VOLUME EDITING ----------------------------------- //

        public List<ZonedPolygonGroup> ZonedVolumeLevels
        {
            get { return (List<ZonedPolygonGroup>)GetValue(ZonedVolumeLevelsProperty); }
            set { SetValue(ZonedVolumeLevelsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZonedVolumeLevels.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZonedVolumeLevelsProperty =
            DependencyProperty.Register("ZonedVolumeLevels", typeof(List<ZonedPolygonGroup>), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(new List<ZonedPolygonGroup>()));


        public List<ZonedVolumeSurfaceVis> ZonedVolumeSurfaces
        {
            get { return (List<ZonedVolumeSurfaceVis>)GetValue(ZonedVolumeSurfacesProperty); }
            set { SetValue(ZonedVolumeSurfacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZonedVolumeSurfaces.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZonedVolumeSurfacesProperty =
            DependencyProperty.Register("ZonedVolumeSurfaces", typeof(List<ZonedVolumeSurfaceVis>), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(new List<ZonedVolumeSurfaceVis>()));


        public List<ComponentInteraction.Material> MaterialLibrary
        {
            get { return (List<ComponentInteraction.Material>)GetValue(MaterialLibraryProperty); }
            set { SetValue(MaterialLibraryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaterialLibrary.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaterialLibraryProperty =
            DependencyProperty.Register("MaterialLibrary", typeof(List<ComponentInteraction.Material>), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(new List<ComponentInteraction.Material>()));
        

        #endregion

        #region SELECTION -OK-

        public Entity SelectedEntity
        {
            get { return (Entity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(Entity), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedEntityPropertyChangedCallback),
                                         new CoerceValueCallback(MySelectedEntityCoerceValueCallback)));

        private static void MySelectedEntityPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                GeometricEntity ge = e.NewValue as GeometricEntity;
                Layer lay = e.NewValue as Layer;

                if (ge != null)
                    zgd.LayerOfSelectedEntity = ge.EntityLayer;
                else if (lay != null)
                    zgd.LayerOfSelectedEntity = zgd.EManager.GetParentLayer(lay);
                else
                    zgd.LayerOfSelectedEntity = null;

                zgd.EManager.SelectGeometry(ge);
                zgd.SelectedEntityIsPolygon = zgd.EManager.SelectedEntityIsPolygon;
                if (zgd.SelectedEntityIsPolygon)
                    zgd.SelectedPolygonHeight = zgd.EManager.SelectedPolygon.Height;
                if (zgd.ZoneEditMode == ZoneEditType.VOLUME_PICK)
                    zgd.PickedZonedVolume = zgd.EManager.SelectedVolume;
                zgd.UpdateSelectedGeometryDisplay();


                if (zgd.SelectedEntityIsPolygon)
                    zgd.LabelsOfSelectedPolygon = zgd.EManager.SelectedPolygon.GetLabelsAsString();
                else
                    zgd.LabelsOfSelectedPolygon = string.Empty;
            }
        }

        private static object MySelectedEntityCoerceValueCallback(DependencyObject d, object value)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                // do not change selection if we are in POLYGON EDIT MODE !!!
                if (zgd.ZoneEditMode != ZoneEditType.NO_EDIT && zgd.ZoneEditMode != ZoneEditType.VOLUME_PICK)
                    return zgd.SelectedEntity;
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
            DependencyProperty.Register("LayerOfSelectedEntity", typeof(Layer), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MyLayerOfSelectedEntityPropertyChangedCallback)));

        private static void MyLayerOfSelectedEntityPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                GeometricEntity sge = zgd.SelectedEntity as GeometricEntity;
                Layer lay = zgd.SelectedEntity as Layer;
                if (sge != null && zgd.LayerOfSelectedEntity != EntityManager.NULL_LAYER && sge.EntityLayer.ID != zgd.LayerOfSelectedEntity.ID)
                {
                    sge.EntityLayer = zgd.LayerOfSelectedEntity;
                    zgd.moved_ge_to_another_layer = true;
                    // publish data to GUI
                    zgd.ZoneLayers = new List<Layer>(zgd.EManager.Layers);
                    zgd.ZoneLayersFlat = new List<Layer>(zgd.EManager.GetFlatLayerList());
                    // refresh viewport display
                    zgd.UpdateGeometryColor();
                    zgd.UpdateGeometryVisibility();
                }
                else if (lay != null && zgd.EManager.GetParentLayer(lay) != zgd.LayerOfSelectedEntity)
                {
                    if (!zgd.moved_ge_to_another_layer)
                    {
                        //string dest = (zgd.LayerOfSelectedEntity == null) ? "null" : zgd.LayerOfSelectedEntity.EntityName;
                        //string debug = lay.EntityName + " >> " + dest;
                        bool success = zgd.EManager.MoveLayer(lay, zgd.LayerOfSelectedEntity);
                        if (success)
                        {
                            // publish data to GUI
                            zgd.ZoneLayers = new List<Layer>(zgd.EManager.Layers);
                            zgd.SelectedEntity = lay;
                            zgd.ZoneLayersFlat = new List<Layer>(zgd.EManager.GetFlatLayerList());

                            // refresh viewport display
                            zgd.UpdateGeometryColor();
                            zgd.UpdateGeometryVisibility();
                        }
                    }
                    else
                    {
                        zgd.moved_ge_to_another_layer = false;
                    }
                    
                }
            }
        }

        private bool moved_ge_to_another_layer = false;

        public string LabelsOfSelectedPolygon
        {
            get { return (string)GetValue(LabelsOfSelectedPolygonProperty); }
            set { SetValue(LabelsOfSelectedPolygonProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelsOfSelectedPolygon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelsOfSelectedPolygonProperty =
            DependencyProperty.Register("LabelsOfSelectedPolygon", typeof(string), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(string.Empty));
        

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

        #endregion

        #region SEARCH -OK-

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SearchText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(string.Empty));

        #endregion

        #region Transfer from ArchitectureDisplay -OK-

        public List<Point3D> PolygonDefinitionIn
        {
            get { return (List<Point3D>)GetValue(PolygonDefinitionInProperty); }
            set { SetValue(PolygonDefinitionInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PolygonDefinitionIn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PolygonDefinitionInProperty =
            DependencyProperty.Register("PolygonDefinitionIn", typeof(List<Point3D>), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(new List<Point3D>()));

        #endregion

        #region EDIT MODES GENERAL -OK-

        // can be: NO_EDIT = 0, POLYGON_VERTEX, POLYGON_OPENING, VOLUME_CREATE, VOLUME_CREATE_COMPLEX,  VOLUME_POLYGON
        public ZoneEditType ZoneEditMode
        {
            get { return (ZoneEditType)GetValue(ZoneEditModeProperty); }
            set { SetValue(ZoneEditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ZoneEditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoneEditModeProperty =
            DependencyProperty.Register("ZoneEditMode", typeof(ZoneEditType), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(ZoneEditType.NO_EDIT, new PropertyChangedCallback(MyZoneEditModePropertyChangedCallback)));

        private static void MyZoneEditModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            ZoneEditType zet = ZoneEditType.NO_EDIT;
            if (e.NewValue is ZoneEditType)
                zet = (ZoneEditType)e.NewValue;

            if (zgd != null)
            {
                // do something, if necessary...
            }
        }

        #endregion

        #region EDIT POLYGON MODES -OK-

        public bool SelectedEntityIsPolygon
        {
            get { return (bool)GetValue(SelectedEntityIsPolygonProperty); }
            set { SetValue(SelectedEntityIsPolygonProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntityIsPolygon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityIsPolygonProperty =
            DependencyProperty.Register("SelectedEntityIsPolygon", typeof(bool), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(false));


        public double SelectedPolygonHeight
        {
            get { return (double)GetValue(SelectedPolygonHeightProperty); }
            set { SetValue(SelectedPolygonHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedPolygonHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedPolygonHeightProperty =
            DependencyProperty.Register("SelectedPolygonHeight", typeof(double), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(0.0, new PropertyChangedCallback(MySelectedPolygonHeightPropertyChangedCallback)));

        private static void MySelectedPolygonHeightPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                ZonedPolygon zp = zgd.SelectedEntity as ZonedPolygon;
                if (zp != null)
                {
                    zp.Height = (float) zgd.SelectedPolygonHeight;
                }
            }
        }       

        // can be: NONE, VERTEX_EDIT, VERTEX_ADD, VERTEX_REMOVE, POLY_REVERSE, POLY_LABELS_EDIT, POLY_LABELS_DEFAULT,
        //         OPENING_EDIT, OPENING_ADD, OPENING_REMOVE, ISBEING_DELETED
        private ZonePolygonEditModeType prevPolygonEditModeType = ZonePolygonEditModeType.NONE;
        public ZonePolygonEditModeType PolygonEditModeType
        {
            get { return (ZonePolygonEditModeType)GetValue(PolygonEditModeTypeProperty); }
            set { SetValue(PolygonEditModeTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PolygonEditModeType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PolygonEditModeTypeProperty =
            DependencyProperty.Register("PolygonEditModeType", typeof(ZonePolygonEditModeType), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(ZonePolygonEditModeType.NONE));

        #endregion

        #region SELECTED VERTEX -OK-

        public ZonedPolygonVertexVis SelectedVertex
        {
            get { return (ZonedPolygonVertexVis)GetValue(SelectedVertexProperty); }
            set { SetValue(SelectedVertexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedVertex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedVertexProperty =
            DependencyProperty.Register("SelectedVertex", typeof(ZonedPolygonVertexVis), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedVertexPropertyChangedCallback)));

        private static void MySelectedVertexPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                ZonedPolygonVertexVis zpv = e.NewValue as ZonedPolygonVertexVis;
                zgd.EManager.SelectVertex(zpv);
                zgd.UpdateSelectedVertexDisplay();
            }
        }

        #endregion

        #region MOVE VERTEX -OK-

        public double PolygonVertexFwDist
        {
            get { return (double)GetValue(PolygonVertexFwDistProperty); }
            set { SetValue(PolygonVertexFwDistProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PolygonVertexFwDist.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PolygonVertexFwDistProperty =
            DependencyProperty.Register("PolygonVertexFwDist", typeof(double), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(0.0, new PropertyChangedCallback(MyPolygonVertexFwDistPropertyChangedCallback)));

        private static void MyPolygonVertexFwDistPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null && zgd.SelectedVertex != null)
            {
                // set twin value
                if (zgd.PolygonVertexFwDist > 0)
                    zgd.PolygonVertexBwDist = 0;

                // process vertex
                // zgd.SelectedVertex.SetAt(true, (float)zgd.PolygonVertexFwDist);
                zgd.EManager.MoveVertexFromSelectedPolygon(zgd.SelectedVertex, true, (float)zgd.PolygonVertexFwDist);
                zgd.UpdateGeometry(zgd.SelectedEntity.ID);
                zgd.UpdateSelectedVertexDisplay();
            }
        }

        public double PolygonVertexBwDist
        {
            get { return (double)GetValue(PolygonVertexBwDistProperty); }
            set { SetValue(PolygonVertexBwDistProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PolygonVertexBwDist.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PolygonVertexBwDistProperty =
            DependencyProperty.Register("PolygonVertexBwDist", typeof(double), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(0.0, new PropertyChangedCallback(MyPolygonVertexBwDistPropertyChangedCallback)));

        private static void MyPolygonVertexBwDistPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null && zgd.SelectedVertex != null)
            {
                // set twin value
                if (zgd.PolygonVertexBwDist > 0)
                    zgd.PolygonVertexFwDist = 0;

                // preocess vertex
                // zgd.SelectedVertex.SetAt(false, (float)zgd.PolygonVertexFwDist);
                zgd.EManager.MoveVertexFromSelectedPolygon(zgd.SelectedVertex, false, (float)zgd.PolygonVertexBwDist);
                zgd.UpdateGeometry(zgd.SelectedEntity.ID);
                zgd.UpdateSelectedVertexDisplay();
            }
        }

        #endregion

        #region SELECTED OPENING -OK-

        public ZoneOpeningVis SelectedOpening
        {
            get { return (ZoneOpeningVis)GetValue(SelectedOpeningProperty); }
            set { SetValue(SelectedOpeningProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedOpening.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedOpeningProperty =
            DependencyProperty.Register("SelectedOpening", typeof(ZoneOpeningVis), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedOpeningPropertyChangedCallback)));

        private static void MySelectedOpeningPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                ZoneOpeningVis zod = e.NewValue as ZoneOpeningVis;
                zgd.EManager.SelectOpening(zod);
                zgd.UpdateSelectedOpeningDisplay();
            }
        }

        #endregion

        #region EDIT VOLUME MODES -OK-

        // can be: NONE, LEVEL_ADD, LEVEL_DELETE, MATERIAL_ASSIGN
        private ZonedVolumeEditModeType prevVolumeEditModeType = ZonedVolumeEditModeType.NONE;
        public ZonedVolumeEditModeType VolumeEditModeType
        {
            get { return (ZonedVolumeEditModeType)GetValue(VolumeEditModeTypeProperty); }
            set { SetValue(VolumeEditModeTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VolumeEditModeType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VolumeEditModeTypeProperty =
            DependencyProperty.Register("VolumeEditModeType", typeof(ZonedVolumeEditModeType), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(ZonedVolumeEditModeType.NONE));

        public ZonedPolygonGroup SelectedVolumeLevel
        {
            get { return (ZonedPolygonGroup)GetValue(SelectedVolumeLevelProperty); }
            set { SetValue(SelectedVolumeLevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedVolumeLevel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedVolumeLevelProperty =
            DependencyProperty.Register("SelectedVolumeLevel", typeof(ZonedPolygonGroup), typeof(ZoneGroupDisplay), 
            new UIPropertyMetadata(null));

        #endregion

        #region SELECTED VOLUME SURFACE -OK-

        public ZonedVolumeSurfaceVis SelectedVSurf
        {
            get { return (ZonedVolumeSurfaceVis)GetValue(SelectedVSurfProperty); }
            set { SetValue(SelectedVSurfProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedVSurf.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedVSurfProperty =
            DependencyProperty.Register("SelectedVSurf", typeof(ZonedVolumeSurfaceVis), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedVSurfPropertyChangedCallback)));

        private static void MySelectedVSurfPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                ZonedVolumeSurfaceVis zvsv = e.NewValue as ZonedVolumeSurfaceVis;
                zgd.EManager.SelectSurface(zvsv);
                zgd.UpdateSelectedSurfaceDisplay();
            }
        }



        public ComponentInteraction.Material MaterialOfSelectedVSurf
        {
            get { return (ComponentInteraction.Material)GetValue(MaterialOfSelectedVSurfProperty); }
            set { SetValue(MaterialOfSelectedVSurfProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaterialOfSelectedVSurf.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaterialOfSelectedVSurfProperty =
            DependencyProperty.Register("MaterialOfSelectedVSurf", typeof(ComponentInteraction.Material), typeof(ZoneGroupDisplay),
            new UIPropertyMetadata(ComponentInteraction.Material.Default, new PropertyChangedCallback(MyMaterialOfSelectedVSurfPropertyChangedCallback)));


        private static void MyMaterialOfSelectedVSurfPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ZoneGroupDisplay zgd = d as ZoneGroupDisplay;
            if (zgd != null)
            {
                if (zgd.SelectedVSurf != null && zgd.MaterialOfSelectedVSurf != null)
                {
                    zgd.SelectedVSurf.AssocMaterial = zgd.MaterialOfSelectedVSurf;
                    // propagate change to volume
                    zgd.EManager.AssignMaterialToSurface(zgd.SelectedVSurf);
                    // update visible geometry
                    zgd.UpdateGeometry(zgd.SelectedEntity.ID);
                    zgd.UpdateSelectedSurfaceDisplay();
                }
                    
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS MEMBERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CLASS MEMBERS

        private ZoneEditModeManager zemManager;
        public ICommand SwitchZoneEditModeCmd { get; private set; }

        private List<Layer> allLayers;
        private List<GeometricEntity> allGE;
        private List<EntityVisibility> allLayersVisibilityState;
        private List<EntityVisibility> allGEVisibilityState;
        private LineGeometryModel3D selectedGeometry;
        private LineGeometryModel3D feedbackGeometry;
        private LineGeometryModel3D volOpacityGeometry;
        private List<int> volOpacity_neighborInds;

        public bool MouseSelectedNew { get; set; }
        public ICommand SwitchVisibilityCmd { get; private set; }
        public ICommand SwitchZoneVisibilityCmd { get; private set; }
        public ICommand ChangeColorCmd { get; private set; }
        public ICommand CreateNewLayerCmd { get; private set; }
        public ICommand DeleteSelectedEntityCmd { get; private set; }
        public ICommand SwitchVisibilityForAllCmd { get; private set; }
        public ICommand SaveVisibilityStateCmd { get; private set; }
        public ICommand RestoreVisibilityStateCmd { get; private set; }
        public ICommand SwitchAllOffButSelectedCmd { get; private set; }
        public ICommand SwitchOpacityGradientVolumeCmd { get; private set; }

        public ICommand SortLayersCmd { get; private set; }
        public ICommand TransferPolygonDefinionCmd { get; private set; }
        public ICommand SelectNoneCmd { get; private set; }
        public ICommand SearchByNameCmd { get; private set; }

        public ICommand SwitchPolygonEditModeTypeCmd { get; private set; }
        public ICommand ReversePolygonCmd { get; private set; }
        public ICommand DeleteVertexCmd { get; private set; }
        public ICommand ResetPolygonLabelsCmd { get; private set; }
        public ICommand BatchSetPolygonLabelsCmd { get; private set; }

        public ICommand DeleteOpeningCmd { get; private set; }
        public bool PolygonOpeningFirstPointDefined { get; private set; }
        private Point3D openingFirstPoint;
        private int openingIndexInPolygon;

        public ICommand SwitchVolumeEditModeTypeCmd { get; private set; }

        private List<ZonedPolygon> zoned_volume_ruling_polygons;
        private List<ZonedPolygonGroup> zoned_volume_ruling_levels;

        public ICommand CreateZonedVolumeByExtrusionCmd { get; private set; }
        public ICommand DeleteVolumeLevelCmd { get; private set; }

        private ZonedVolume picked_ZonedVolume;
        public ZonedVolume PickedZonedVolume 
        {
            get { return this.picked_ZonedVolume; }
            private set
            {
                this.picked_ZonedVolume = value;
                this.RegisterPropertyChanged("PickedZonedVolume");
            }
        }

        public ICommand ConvertZonedVolumeToSpaceCmd { get; private set; }
        public List<string> AllSpaceNames { get; private set; }

        public ICommand ExportSelectedToACADCmd { get; private set; }
        public ICommand ExportAllToAutoCADCmd { get; private set; }
        public ICommand SaveGeometryCmd { get; private set; }
        public ICommand OpenGeometryFileCmd {get; private set; }

        private ZonedPolygon poly_to_split_1;
        private ZonedPolygon poly_to_split_2;
        private Point3D poly_split_Point_A;
        private Point3D poly_split_Point_B;
        private int poly_split_nr_chosen_points;

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== INITIALIZATION ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC
        static ZoneGroupDisplay()
        {
            VolumeMat = new PhongMaterial();
            VolumeMat.DiffuseColor = new Color4(1f, 1f, 1f, 0.75f);
            VolumeMat.AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            VolumeMat.EmissiveColor = new Color4(0.25f, 0.25f, 0.25f, 0f);
            VolumeMat.SpecularColor = new Color4(1f, 1f, 1f, 1f);
            VolumeMat.SpecularShininess = 3;

            SelectMat = new PhongMaterial();
            SelectMat.DiffuseColor = new Color4(1f, 1f, 1f, 0.5f);
            SelectMat.AmbientColor = new Color4(0.9f, 0.9f, 0.8f, 1f);
            SelectMat.EmissiveColor = new Color4(0.5f, 0.5f, 0.5f, 0f);
            SelectMat.SpecularColor = new Color4(1f, 1f, 1f, 1f);
            SelectMat.SpecularShininess = 3;

            AlertMat = new PhongMaterial();
            AlertMat.DiffuseColor = new Color4(0.8f, 0f, 0f, 0.25f);
            AlertMat.AmbientColor = new Color4(0.6f, 0f, 0f, 1f);
            AlertMat.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            AlertMat.SpecularShininess = 1;

            OffsetMat = new PhongMaterial();
            OffsetMat.DiffuseColor = new Color4(1f, 1f, 1f, 0.3f);
            OffsetMat.AmbientColor = new Color4(0.9f, 0.9f, 0.9f, 1f);
            OffsetMat.EmissiveColor = new Color4(1f, 1f, 1f, 0f);
            OffsetMat.SpecularColor = new Color4(1f, 1f, 1f, 1f);
            OffsetMat.SpecularShininess = 1;
        }
        #endregion

        #region INSTANCE
        public ZoneGroupDisplay()
        {
            this.zemManager = new ZoneEditModeManager();
            this.SwitchZoneEditModeCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchZoneEditModeCommand(x));

            // prepare for selection
            this.MouseSelectedNew = false;

            // general commands
            this.SwitchVisibilityCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchVisibilityCommand(x));
            this.SwitchZoneVisibilityCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchZoneVisibilityCommand(x));
            this.ChangeColorCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnChangeColorCommand(x));
            this.CreateNewLayerCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnCreateNewLayerCommand());
            this.DeleteSelectedEntityCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnDeleteSelectedEntity(),
                                                                          (x) => CanExecute_OnDeleteSelectedEntity(x));
            this.SwitchVisibilityForAllCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchAllLayersCommand(x));
            this.SaveVisibilityStateCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSaveVisibilityStateCommand());
            this.RestoreVisibilityStateCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnRestoreVisibilityStateCommand(),
                                                                            (x) => CanExecute_OnRestoreVisibilityStateCommand());
            this.SwitchAllOffButSelectedCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchAllOffButSelected(),
                                                                             (x) => CanExecute_OnSwitchAllOffButSelected());
            this.SwitchOpacityGradientVolumeCmd = 
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchOpacityGradientOfVolumes(x));

            this.SortLayersCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSortLayersCommand());

            this.TransferPolygonDefinionCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnTransferPolygonDefinionCommand(),
                                                                             (x) => CanExecute_OnTransferPolygonDefinionCommand());
            this.SelectNoneCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSelectNoneCommand(),
                                                                (x) => CanExecute_OnSelectNoneCommand());
            this.SearchByNameCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSearchByNameCommand(x));

            // polygon and vertex editing commands
            this.SwitchPolygonEditModeTypeCmd = 
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchPolygonEditModeTypeCommand(x));
            this.ReversePolygonCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnReversePolygonCommand(),
                                                                    (x) => CanExecute_OnReversePolygonCommand());
            this.DeleteVertexCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnDeleteVertexCommand(),
                                                                  (x) => CanExecute_OnDeleteVertexCommand());
            this.ResetPolygonLabelsCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnResetPolygonLabelsCommand(),
                                                                        (x) => CanExecute_OnResetPolygonLabelsCommand());
            this.BatchSetPolygonLabelsCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnBatchSetPolygonLabels(x),
                                                                           (x) => CanExecute_OnBatchSetPolygonLabels(x));

            // polygon openeings editing commands
            this.DeleteOpeningCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnDeleteOpeningCommand(),
                                                                   (x) => CanExecute_OnDeleteOpeningCommand());
            // prepare for creating of new openings
            this.PolygonOpeningFirstPointDefined = false;
            this.openingFirstPoint = new Point3D(0, 0, 0);
            this.openingIndexInPolygon = -1;

            // prepare container for creating zoned volumes
            this.zoned_volume_ruling_polygons = new List<ZonedPolygon>();
            this.zoned_volume_ruling_levels = new List<ZonedPolygonGroup>();

            // volume creation and editing commands
            this.SwitchVolumeEditModeTypeCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnSwitchVolumeEditModeTypeCommand(x));
            this.CreateZonedVolumeByExtrusionCmd =
                new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnCreateZonedVolumeByExtrusionCommand(),
                                               (x) => CanExecute_OnCreateZonedVolumeByExtrusionCommand());
            this.DeleteVolumeLevelCmd
                = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnDeleteVolumeLevelCommand(),
                                                 (x) => CanExecute_OnDeleteVolumeLevelCommand());

            // export to AutoCAD
            this.ExportSelectedToACADCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => this.OnExportSelectedToACAD(true),
                                                                               (x) => CanExecute_OnExportSelectedToACAD(true));
            this.ExportAllToAutoCADCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => this.OnExportSelectedToACAD(false),
                                                                             (x) => CanExecute_OnExportSelectedToACAD(false));
            // save as DXF
            this.SaveGeometryCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => this.OnSaveGeometry(),
                                                                       (x) => CanExecute_OnSaveGeometry());
            this.OpenGeometryFileCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => this.OnReadGeometryFromDXF(),
                                                                           (x) => CanExecute_OnSaveGeometry()); 
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region UPDATE after Change of the Managers

        private void UpdateAfterGeometryManagerChange()
        {
            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());

            // attach event handlers to the zoned volumes (added 30.11.2016)
            List<GeometricEntity> list_geom = this.EManager.GetFlatGeometryList();
            foreach (GeometricEntity ge in list_geom)
            {
                ZonedVolume zv = ge as ZonedVolume;
                if (zv == null) continue;

                zv.PropertyChanged += this.volume_PropertyChanged;
            }

            // generate dsiplayable 3D geometric data
            this.RegenerateGeometry();
            this.VolumeVisiblityChanged = !(this.VolumeVisiblityChanged);
        }

        private void UpdateAfterMaterialMangerChange()
        {
            // publish data to GUI
            this.MaterialLibrary = new List<ComponentInteraction.Material>(this.MLManager.Materials);
        }

        #endregion

        #region UPDATE 3D GEOMETRY DISPLAY

        private void UpdateGeometry(long _entityID)
        {
            for (int i = 0; i < this.allGE.Count; i++)
            {
                if (this.allGE[i].ID == _entityID)
                {
                    
                    // all polygon models
                    ZonedPolygon zp = this.allGE[i] as ZonedPolygon;
                    if (zp != null)
                    {
                        LineGeometryModel3D modelCTRLP = this.Children[(i + MODELS_OFFSET)* 3] as LineGeometryModel3D;
                        if (modelCTRLP != null)
                        {
                            modelCTRLP.Geometry = zp.BuildCtrlPoints(START_MARKER_SIZE);
                        }

                        LineGeometryModel3D modelLABELS = this.Children[(i + MODELS_OFFSET) * 3 + 1] as LineGeometryModel3D;
                        if (modelLABELS != null)
                        {
                            modelLABELS.Geometry = zp.BuildZoneDescriptors(ZONE_TEXT_SIZE);
                        }

                        LineGeometryModel3D modelMAIN = this.Children[(i + MODELS_OFFSET) * 3 + 2] as LineGeometryModel3D;
                        if (modelMAIN != null)
                        {
                            modelMAIN.Geometry = zp.Build(START_MARKER_SIZE);
                        }
                    }

                    // all volume models
                    ZonedVolume zv = this.allGE[i] as ZonedVolume;
                    if (zv != null)
                    {
                        LineGeometryModel3D modelOUTER = this.Children[(i + MODELS_OFFSET) * 3] as LineGeometryModel3D;
                        if (modelOUTER != null)
                        {
                            modelOUTER.Geometry = zv.BuildOuterOffsetLines();
                        }

                        LineGeometryModel3D modelINNER = this.Children[(i + MODELS_OFFSET) * 3 + 1] as LineGeometryModel3D;
                        if (modelINNER != null)
                        {
                            modelINNER.Geometry = zv.BuildInnerOffsetLines();
                        }

                        UserMesh modelVOL = this.Children[(i + MODELS_OFFSET) * 3 + 2] as UserMesh;
                        if (modelVOL != null)
                        {
                            modelVOL.Geometry = zv.BuildVolumeWOpenings();
                        }
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

            // DEFINE MODELS IN STEPS OF 3!!!
            
            // show the selected polygon, vertex or opening
            AddSelectedGeomDisplay();

            // show feedback to user
            AddFeedBackGeomDisplay();

            // help line geometry for gradient opacity around selected volume
            AddVolumeOpacityGradientGeomDisplay();

            // show the defining polygons(3 models per geometric object!)
            foreach(GeometricEntity ge in allGE)
            {
                try
                {
                    ZonedPolygon zp = ge as ZonedPolygon;
                    ZonedVolume zv = ge as ZonedVolume;
                    if (zp != null)
                        AddZonedPolygon(zp);
                    if (zv != null)
                        AddZonedVolume(zv);
                }
                catch(Exception e)
                {
                    ZoneGroupDisplay.ERR_MESSAGES += e.Message + "\n";
                    MessageBox.Show(e.Message, "Display Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }

            // RE-ATTACH TO RENDERER !!!
            if (this.renderHost != null)
                this.Attach(this.renderHost);

            // communiate to other children in the same viewport
            this.MyEManagerContentChange = !(this.MyEManagerContentChange);
        }

        private void UpdateGeometryVisibility()
        {
            if (this.allGE == null)
                return;
            
            long selTag = -1;
            long.TryParse(this.selectedGeometry.Tag.ToString(), out selTag);

            // assumes the order of Geometric Entities and Children is the same!
            for(int i = 0; i < this.allGE.Count; i++)
            {
                // all geometry
                this.Children[(i + MODELS_OFFSET) * 3].Visibility =
                    (this.allGE[i].Visibility == EntityVisibility.ON && this.allGE[i].ShowZones) ? Visibility.Visible : Visibility.Collapsed;
                this.Children[(i + MODELS_OFFSET) * 3 + 1].Visibility =
                    (this.allGE[i].Visibility == EntityVisibility.ON && this.allGE[i].ShowZones) ? Visibility.Visible : Visibility.Collapsed;

                this.Children[(i + MODELS_OFFSET) * 3 + 2].Visibility =
                    (this.allGE[i].Visibility == EntityVisibility.ON) ? Visibility.Visible : Visibility.Collapsed;
                SelectableUserLine sul = this.Children[(i + MODELS_OFFSET) * 3 + 2] as SelectableUserLine;
                if (sul != null)
                {
                    sul.IsHitTestVisible = (this.allGE[i].Visibility == EntityVisibility.ON) ? true : false;
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

            // communiate to other children in the same viewport
            this.MyEManagerContentChange = !(this.MyEManagerContentChange); 
        }

        private void UpdateGeometryColor()
        {
            if (this.allGE == null)
                return;

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {
                // handle the zoned POLYGONS              
                LineGeometryModel3D lgm_CP = this.Children[(i + MODELS_OFFSET) * 3] as LineGeometryModel3D;
                LineGeometryModel3D lgm_Z  = this.Children[(i + MODELS_OFFSET) * 3 + 1] as LineGeometryModel3D;
                LineGeometryModel3D lgm = this.Children[(i + MODELS_OFFSET) * 3 + 2] as LineGeometryModel3D;
                if (lgm_CP != null && lgm_Z != null && lgm != null)
                {
                    lgm.Color = this.allGE[i].EntityLayer.EntityColor;
                    lgm_CP.Color = this.allGE[i].EntityLayer.EntityColor;
                    lgm_Z.Color = this.allGE[i].EntityLayer.EntityColor;
                }

                // handle the zoned VOLUMES               
                LineGeometryModel3D lgm_OUTER = this.Children[(i + MODELS_OFFSET) * 3] as LineGeometryModel3D;
                LineGeometryModel3D lgm_INNER = this.Children[(i + MODELS_OFFSET) * 3 + 1] as LineGeometryModel3D;
                UserMesh mgm = this.Children[(i + MODELS_OFFSET) * 3 + 2] as UserMesh;
                if (lgm_OUTER != null && lgm_INNER != null && mgm != null)
                {
                    // specify the volume color
                    PhongMaterial coloredMat = this.DeriveVolMaterial(this.allGE[i].EntityLayer.EntityColor);

                    // assign
                    mgm.Material = coloredMat;
                    mgm.PassiveMaterial = coloredMat;
                    lgm_OUTER.Color = this.allGE[i].EntityLayer.EntityColor;
                    lgm_INNER.Color = this.allGE[i].EntityLayer.EntityColor;
                }
            }
        }

        #endregion

        #region UPDATE 3D GEOMETRY DISPLAY FOR POLYGON EDIT MODE

        private void UpdateGeometryDisplayOnEnteringPolygonEditMode()
        {
            if (this.allGE == null)
                return;

            long selTag = -1;
            long.TryParse(this.selectedGeometry.Tag.ToString(), out selTag);

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {                
                LineGeometryModel3D lgm_CP = this.Children[(i + MODELS_OFFSET) * 3] as LineGeometryModel3D;
                LineGeometryModel3D lgm_Z = this.Children[(i + MODELS_OFFSET) * 3 + 1] as LineGeometryModel3D;
                LineGeometryModel3D lgm = this.Children[(i + MODELS_OFFSET) * 3 + 2] as LineGeometryModel3D;

                if (lgm_CP == null || lgm_Z == null || lgm == null)
                    continue;

                if (this.allGE[i].ID != selTag)
                {
                    lgm.Color = Color_Inactive;
                    lgm_CP.Color = Color_Inactive;
                    lgm_Z.Color = Color_Inactive;
                    lgm.IsHitTestVisible = false;
                    lgm_CP.IsHitTestVisible = false;
                    lgm_Z.IsHitTestVisible = false;
                }
                else
                {
                    this.allGE[i].ShowZones = true;
                    lgm_Z.Visibility = Visibility.Visible;
                }

            }

        }

        private void UpdateGeometryDisplayOnExitingPolygonEditMode()
        {
            if (this.allGE == null)
                return;

            // assumes the order of GE and Children is the same!
            for (int i = 0; i < this.allGE.Count; i++)
            {                
                LineGeometryModel3D lgm_CP = this.Children[(i + MODELS_OFFSET) * 3] as LineGeometryModel3D;
                LineGeometryModel3D lgm_Z = this.Children[(i + MODELS_OFFSET) * 3 + 1] as LineGeometryModel3D;
                LineGeometryModel3D lgm = this.Children[(i + MODELS_OFFSET) * 3 + 2] as LineGeometryModel3D;

                if (lgm_CP == null || lgm_Z == null || lgm == null)
                    continue;

                lgm.Color = this.allGE[i].EntityLayer.EntityColor;
                lgm_CP.Color = this.allGE[i].EntityLayer.EntityColor;
                lgm_Z.Color = this.allGE[i].EntityLayer.EntityColor;
                
                lgm.IsHitTestVisible = true;
                lgm_CP.IsHitTestVisible = true;
                lgm_Z.IsHitTestVisible = true;
            }

            // revert the selection to the polygon
            GeometricEntity ge = this.SelectedEntity as GeometricEntity;
            if (ge != null)
            {
                this.EManager.SelectGeometry(ge);
                this.SelectedEntityIsPolygon = this.EManager.SelectedEntityIsPolygon;
                this.UpdateSelectedGeometryDisplay();
            }
        }

        #endregion

        #region UPDATE SELECTED 3D GEOMETRY DISPLAY

        private void UpdateSelectedGeometryDisplay()
        {
            if (this.selectedGeometry == null)
                this.AddSelectedGeomDisplay();

            if (this.EManager.SelectedEntityIsPolygon && this.EManager.SelectedPolygon != null
                && this.EManager.SelectedPolygon.Visibility == EntityVisibility.ON)
            {
                this.selectedGeometry.Geometry = this.EManager.SelectedPolygon.Build(START_MARKER_SIZE);
                this.selectedGeometry.Tag = this.EManager.SelectedPolygon.ID;
            }
            else if (this.EManager.SelectedEntityIsVolume && this.EManager.SelectedVolume != null
                && this.EManager.SelectedVolume.Visibility == EntityVisibility.ON)
            {
                this.selectedGeometry.Geometry = this.EManager.SelectedVolume.BuildSelectionGeometry();
                this.selectedGeometry.Tag = this.EManager.SelectedVolume.ID;
            }
            else
            {
                this.selectedGeometry.Geometry = null;
                this.selectedGeometry.Tag = -1;
            }
        }

        private void UpdateSelectedVertexDisplay()
        {
            if (this.SelectedVertex != null)
            {
                this.selectedGeometry.Geometry = this.SelectedVertex.Build(START_MARKER_SIZE);
                this.selectedGeometry.Tag = this.SelectedVertex.ID;
            }
            else
            {
                this.selectedGeometry.Geometry = null;
                this.selectedGeometry.Tag = -1;
            }
        }

        private void UpdateSelectedOpeningDisplay()
        {
            if (this.SelectedOpening != null)
            {
                this.selectedGeometry.Geometry = this.SelectedOpening.Build(OPENING_MARKER_SIZE);
                this.selectedGeometry.Tag = this.SelectedOpening.ID;
            }
            else
            {
                this.selectedGeometry.Geometry = null;
                this.selectedGeometry.Tag = -1;
            }
        }

        private void UpdateSelectedSurfaceDisplay()
        {
            if (this.SelectedVSurf != null)
            {
                this.selectedGeometry.Geometry = this.SelectedVSurf.Build(START_MARKER_SIZE);
                this.selectedGeometry.Tag = this.SelectedVSurf.ID;
            }
            else
            {
                this.selectedGeometry.Geometry = null;
                this.selectedGeometry.Tag = -1;
            }
        }

        #endregion

        #region 3D GEOMETRY DEFINITIONS

        private void AddZonedPolygon(ZonedPolygon _zp)
        {
            if (_zp == null)
                return;

            // polygon control points
            LineGeometryModel3D zpcp = new LineGeometryModel3D()
            {
                Geometry = _zp.BuildCtrlPoints(START_MARKER_SIZE),
                Color = _zp.EntityLayer.EntityColor,
                Thickness = THICKNESS_INFO,
                Visibility = (_zp.Visibility == EntityVisibility.ON && _zp.ShowZones) ? Visibility.Visible : Visibility.Collapsed,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = _zp.ID,
            };
            this.Children.Add(zpcp);

            // polygon zone descriptors
            LineGeometryModel3D zdesc = new LineGeometryModel3D()
            {
                Geometry = _zp.BuildZoneDescriptors(ZONE_TEXT_SIZE),
                Color = _zp.EntityLayer.EntityColor,
                Thickness = THICKNESS_INFO,
                Visibility = (_zp.Visibility == EntityVisibility.ON && _zp.ShowZones) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = _zp.ID,
            };
            this.Children.Add(zdesc);

            // actual polygon
            SelectableUserLine zpg = new SelectableUserLine()
            {
                Geometry = _zp.Build(START_MARKER_SIZE),
                Color = _zp.EntityLayer.EntityColor,
                Thickness = THICKNESS_DEFAULT,
                Visibility = (_zp.Visibility == EntityVisibility.ON) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = (_zp.Visibility == EntityVisibility.ON) ? true : false,
                HitTestThickness = 3,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = _zp.ID,
            };
            zpg.MouseDown3D += polygon_MouseDown3D;
            zpg.MouseUp3D += polygon_MouseUp3D;
            this.Children.Add(zpg);
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

        private void AddZonedVolume(ZonedVolume _zv)
        {
            if (_zv == null)
                return;

            // get the mesh(es)
            HelixToolkit.SharpDX.Wpf.MeshGeometry3D model = _zv.BuildVolumeWOpenings();
            HelixToolkit.SharpDX.Wpf.LineGeometry3D model_OUT = _zv.BuildOuterOffsetLines();
            HelixToolkit.SharpDX.Wpf.LineGeometry3D model_IN = _zv.BuildInnerOffsetLines();
            
            // specify the volume color
            PhongMaterial coloredMat = this.DeriveVolMaterial(_zv.EntityLayer.EntityColor);
 
            // OUTER SURFACES acc. to assigned materials (thickness, position of wall axis plane)
            LineGeometryModel3D zvg_outer = new LineGeometryModel3D()
            {
                Geometry = model_OUT,
                Color = _zv.EntityLayer.EntityColor,
                Thickness = THICKNESS_DEFAULT,
                Visibility = (_zv.Visibility == EntityVisibility.ON && _zv.ShowZones) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = _zv.ID,
            };
            this.Children.Add(zvg_outer);

            // INNER SURFACES acc. to assigned materials (thickness, position of wall axis plane)
            LineGeometryModel3D zvg_inner = new LineGeometryModel3D()
            {
                Geometry = model_IN,
                Color = _zv.EntityLayer.EntityColor,
                Thickness = THICKNESS_INFO,
                Visibility = (_zv.Visibility == EntityVisibility.ON && _zv.ShowZones) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = _zv.ID,
            };
            this.Children.Add(zvg_inner);

            // actual volume
            UserMesh zvg = new UserMesh()
            {
                Geometry = model,
                Material = coloredMat,
                PassiveMaterial = coloredMat,
                SelectionMaterial = ZoneGroupDisplay.SelectMat,
                Visibility = (_zv.Visibility == EntityVisibility.ON) ? Visibility.Visible : Visibility.Collapsed,
                IsHitTestVisible = true,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Tag = _zv.ID,
            };
            zvg.MouseDown3D += volume_MouseDown3D;
            this.Children.Add(zvg);

        }

        private void AddFeedBackGeomDisplay()
        {
            LineBuilder b = new LineBuilder();
            b.AddBox(Vector3.Zero, START_MARKER_SIZE, START_MARKER_SIZE, START_MARKER_SIZE);

            this.feedbackGeometry = new LineGeometryModel3D()
            {
                Geometry = b.ToLineGeometry3D(),
                Color = Color_FeedBack,
                Thickness = THICKNESS_SELECTED_DEFAULT,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = -1,
            };
            this.Children.Add(this.feedbackGeometry);
        }

        private void AddVolumeOpacityGradientGeomDisplay()
        {
            this.volOpacityGeometry = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color.Black,
                Thickness = THICKNESS_INFO,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
                Tag = -1,
            };
            this.Children.Add(this.volOpacityGeometry);
        }

        private PhongMaterial DeriveVolMaterial(Color _col)
        {
            PhongMaterial coloredMat = new PhongMaterial();
            if ((_col.R == 0 && _col.G == 0 && _col.B == 0) ||
                (_col.R == 255 && _col.G == 255 && _col.B == 255))
            {
                coloredMat.DiffuseMap = this.OpacityMap;
                coloredMat.DiffuseColor = new Color4(1f, 1f, 1f, (float)OPACITY_DEF);
                coloredMat.EmissiveColor = new Color4(_col.R, _col.G, _col.B, 0f);
                coloredMat.SpecularColor = VolumeMat.SpecularColor;
                coloredMat.SpecularShininess = VolumeMat.SpecularShininess;
            }
            else
            {
                coloredMat.DiffuseMap = null;
                coloredMat.DiffuseColor = new Color4(_col.R / 32, _col.G / 32, _col.B / 32, (float)OPACITY_DEF);
            }
            return coloredMat;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= COMMANDS ============================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMANDS: Switching ZONE Edit Mode
        private void OnSwitchZoneEditModeCommand(object _mode)
        {
            if (_mode == null)
                return;

            this.ZoneEditMode = this.zemManager.SetEditMode(_mode.ToString());

            switch (this.ZoneEditMode)
            {
                case ZoneEditType.POLYGON_VERTEX:
                    this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
                    this.UpdateGeometryDisplayOnEnteringPolygonEditMode();
                    break;
                case ZoneEditType.POLYGON_OPENING:
                    this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
                    this.UpdateGeometryDisplayOnEnteringPolygonEditMode();
                    break;
                case ZoneEditType.VOLUME_CREATE:
                    this.zoned_volume_ruling_polygons.Add(this.EManager.SelectedPolygon);
                    break;
                case ZoneEditType.VOLUME_CREATE_COMPLEX:
                    break;
                case ZoneEditType.VOLUME_SURFACE:
                    this.ZonedVolumeSurfaces = new List<ZonedVolumeSurfaceVis>(this.EManager.SurfacesOfSelectedVolume);
                    // TODO: Modify Display of other objects in the scene
                    break;
                case ZoneEditType.VOLUME_PICK:
                    // is used by commands from other Display classes, that require a ZonedVolume (e.g. SpaceDisplay)
                    // the logic is implemented in the mouse event handler
                    break;
                case ZoneEditType.POLYGON_SPLIT:
                    break;
                default:
                    // returning from polygon editing
                    this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>();
                    this.ZonePolygonOpenings = new List<ZoneOpeningVis>();
                    this.UpdateGeometryDisplayOnExitingPolygonEditMode();
                    this.OnSwitchPolygonEditModeTypeCommand("NONE");

                    // returning from volume creation (ruling POLYGONS)
                    if (this.zoned_volume_ruling_polygons.Count > 1)
                        this.CreateZonedVolumeFromSelection();
                    this.zoned_volume_ruling_polygons = new List<ZonedPolygon>();

                    // returning from volume creation (ruling LEVELS)
                    if (this.zoned_volume_ruling_levels.Count > 1)
                        this.CreateZonedVolumeFromLevels();
                    this.zoned_volume_ruling_levels = new List<ZonedPolygonGroup>();
                    this.ZonedVolumeLevels = new List<ZonedPolygonGroup>();

                    // returning from volume material assignment
                    this.ZonedVolumeSurfaces = new List<ZonedVolumeSurfaceVis>();

                    // returning from polygon split
                    this.poly_to_split_1 = null;
                    this.poly_to_split_2 = null;
                    this.poly_split_nr_chosen_points = 0;

                    break;
            }
        }
        #endregion

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

        private void OnSwitchZoneVisibilityCommand(object _e)
        {
            if (_e != null)
            {
                Entity ent = _e as Entity;
                if (ent != null)
                {
                    bool zonesOn = ent.ShowZones;
                    ent.ShowZones = !zonesOn;
                    // update geometry
                    UpdateGeometryVisibility();
                }
            }
        }

        private void OnChangeColorCommand(object _e)
        {
            if (_e != null)
            {
                Layer layer = _e as Layer;
                if (layer != null)
                {
                    Window window = Window.GetWindow(this);
                    if (window != null)
                    {
                        MainWindow mw = window as MainWindow;
                        if (mw != null)
                        {
                            System.Windows.Media.Color nc = mw.OpenColorPicker();
                            // process layer
                            layer.EntityColor = new SharpDX.Color(nc.R, nc.G, nc.B, nc.A);
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
                // switch all geometry on
                if (switch_on)
                {
                    foreach(GeometricEntity ge in this.allGE)
                    {
                        ge.Visibility = EntityVisibility.ON;
                    }
                }
                // switch all layers on
                foreach(Layer layer in this.allLayers)
                {
                    layer.Visibility = switch_on ? EntityVisibility.ON : EntityVisibility.OFF;
                }
                // update geometry
                UpdateGeometryVisibility();
            }
        }

        private void OnSaveVisibilityStateCommand()
        {
            this.allLayersVisibilityState = new List<EntityVisibility>();
            foreach(Layer layer in this.allLayers)
            {
                this.allLayersVisibilityState.Add(layer.Visibility);
            }

            this.allGEVisibilityState = new List<EntityVisibility>();
            foreach(GeometricEntity ge in this.allGE)
            {
                this.allGEVisibilityState.Add(ge.Visibility);
            }
        }

        private void OnRestoreVisibilityStateCommand()
        {
            int nG = this.allGE.Count;
            for (int i = 0; i < nG; i++ )
            {
                this.allGE[i].Visibility = this.allGEVisibilityState[i];
            }

            int nL = this.allLayers.Count;
            for (int i = 0; i < nL; i++ )
            {
                this.allLayers[i].Visibility = this.allLayersVisibilityState[i];
            }

            // update geometry
            UpdateGeometryVisibility();
        }

        private bool CanExecute_OnRestoreVisibilityStateCommand()
        {
            if (this.allLayersVisibilityState == null || this.allGEVisibilityState == null)
                return false;

            int nL = this.allLayers.Count;
            int nLV = this.allLayersVisibilityState.Count;

            int nG = this.allGE.Count;
            int nGV = this.allGEVisibilityState.Count;

            return (nL == nLV && nG == nGV);
        }

        private void OnSwitchAllOffButSelected()
        {
            GeometricEntity sge = this.SelectedEntity as GeometricEntity;
            if (sge == null)
                return;

            List<Layer> parentChain = this.EManager.GetParentLayerChain(sge.EntityLayer);
            if (parentChain == null || parentChain.Count < 1)
                return;

            List<Layer> allAncestors = new List<Layer>();
            allAncestors.AddRange(parentChain);

            if (this.EManager.SelectedEntityIsPolygon)
            {              
                // turn all layers OFF (except the parent chain layers)
                foreach (Layer layer in this.allLayers)
                {
                    if (!allAncestors.Contains(layer))
                        layer.Visibility = EntityVisibility.OFF;
                }

                // turn all geometry OFF, except the selected one
                foreach (GeometricEntity ge in this.allGE)
                {
                    if (ge.ID != sge.ID)
                        ge.Visibility = EntityVisibility.OFF;
                }
            }
            else if(this.EManager.SelectedEntityIsVolume)
            {                
                ZonedVolume szv = this.SelectedEntity as ZonedVolume;
                if (szv != null)
                {
                    // leave all defining polygons visible as well
                    List<ZonedPolygon> defPolys = szv.Defining_Polygons;
                    foreach (ZonedPolygon dzp in defPolys)
                    {
                        List<Layer> dzp_parentChain = this.EManager.GetParentLayerChain(dzp.EntityLayer);
                        allAncestors.AddRange(dzp_parentChain);
                    }
                    allAncestors = allAncestors.Distinct().ToList();

                    // turn all layers OFF (except the parent chain layers)
                    foreach (Layer layer in this.allLayers)
                    {
                        if (!allAncestors.Contains(layer))
                            layer.Visibility = EntityVisibility.OFF;
                    }

                    // turn all geometry OFF, except the selected one
                    // and its defining polygons
                    foreach (GeometricEntity ge in this.allGE)
                    {
                        if (ge.ID != sge.ID && !defPolys.Contains(ge))
                            ge.Visibility = EntityVisibility.OFF;
                    }

                }
  
            }

            // update geometry
            UpdateGeometryVisibility();
        }

        private bool CanExecute_OnSwitchAllOffButSelected()
        {
            if (this.SelectedEntity == null)
                return false;

            GeometricEntity ge = this.SelectedEntity as GeometricEntity;
            return (ge != null);
        }

        private void OnSwitchOpacityGradientOfVolumes(object _on)
        {
            bool turnOn = false;
            if (_on is bool)
                turnOn = (bool)_on;
            else
                return;

            if (this.allGE == null)
                return;

            if (turnOn)
            {
                ZonedVolume zv = this.SelectedEntity as ZonedVolume;
                if (zv == null)
                    return;

                long selTag = -1;
                long.TryParse(this.selectedGeometry.Tag.ToString(), out selTag);

                // show lines of closest volumes
                SortAndShowVolumesRelToSelectedVolume();

                // assumes the order of GE and Children is the same!
                for (int i = 0; i < this.allGE.Count; i++)
                {
                    MeshGeometryModel3D mgm = this.Children[(i + MODELS_OFFSET) * 3 + 2] as MeshGeometryModel3D;
                    if (mgm == null)
                        continue;

                    PhongMaterial mat = mgm.Material as PhongMaterial;
                    if (mat == null)
                        continue;

                    if (this.allGE[i].ID != selTag && !this.volOpacity_neighborInds.Contains(i))
                    {
                        mat.DiffuseColor = new Color4(mat.DiffuseColor.Red, mat.DiffuseColor.Green,
                                                      mat.DiffuseColor.Blue, (float)OPACITY_LOW);
                    }
                    else if (this.allGE[i].ID != selTag && this.volOpacity_neighborInds.Contains(i))
                    {
                        mat.DiffuseColor = new Color4(mat.DiffuseColor.Red, mat.DiffuseColor.Green,
                                                        mat.DiffuseColor.Blue, (float)OPACITY_MID);
                    }
                    else
                    {
                        mat.DiffuseColor = new Color4(mat.DiffuseColor.Red, mat.DiffuseColor.Green,
                                                      mat.DiffuseColor.Blue, (float)OPACITY_DEF);
                    }
                }                
            }
            else
            {
                // assumes the order of GE and Children is the same!
                for (int i = 0; i < this.allGE.Count; i++)
                {
                    MeshGeometryModel3D mgm = this.Children[(i + MODELS_OFFSET) * 3 + 2] as MeshGeometryModel3D;
                    if (mgm == null)
                        continue;

                    PhongMaterial mat = mgm.Material as PhongMaterial;
                    if (mat == null)
                        continue;

                    mat.DiffuseColor = new Color4(mat.DiffuseColor.Red, mat.DiffuseColor.Green,
                                                        mat.DiffuseColor.Blue, (float)OPACITY_DEF);
                }
                // hide lines of closest volumes
                this.volOpacity_neighborInds = new List<int>();
                this.volOpacityGeometry.Geometry = null;
                this.volOpacityGeometry.Visibility = Visibility.Collapsed;
            }

        }

        private void SortAndShowVolumesRelToSelectedVolume()
        {
            ZonedVolume zv = this.SelectedEntity as ZonedVolume;
            if (zv == null)
                return;

            this.volOpacity_neighborInds = new List<int>();

            Vector3 pivot_selection = zv.Volume_Pivot;
            BoundingBox bounds_selection = this.selectedGeometry.Bounds;
            float search_radius = Vector3.DistanceSquared(bounds_selection.Maximum, bounds_selection.Minimum) * OPACITY_RADIUS_FACT;

            LineBuilder b = new LineBuilder();
            for (int i = 0; i < this.allGE.Count; i++ )
            {
                ZonedVolume zvi = this.allGE[i] as ZonedVolume;
                if (zvi == null)
                    continue;

                float dist = Vector3.DistanceSquared(pivot_selection, zvi.Volume_Pivot);
                if (dist < search_radius)
                {
                    // add to geometry
                    zvi.AddSelectionGeometry(ref b);
                    this.volOpacity_neighborInds.Add(i);
                }
            }
            this.volOpacityGeometry.Geometry = b.ToLineGeometry3D();
            this.volOpacityGeometry.Color = this.LayerOfSelectedEntity.EntityColor;
            this.volOpacityGeometry.Visibility = Visibility.Visible;
        }

        #endregion

        #region COMMANDS: New, Delete

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
            MessageBoxResult answer = MessageBox.Show( message, caption, 
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer == MessageBoxResult.Yes)
            {
                bool success = this.EManager.RemoveEntity(this.SelectedEntity);
                if (success)
                {
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
                foreach(string strType in str_params_OR)
                {
                    Type targetType = Type.GetType(strType.ToString());
                    if (targetType != null && this.SelectedEntity.GetType() == targetType)
                        return true;
                }
                return false;
            }

            
        }
        #endregion

        #region COMMANDS: Define Polygons from other Geometry

        private void OnTransferPolygonDefinionCommand()
        {
            ZonedPolygon polygon;
            Layer layer = null;
            if (this.SelectedEntity != null)
            {
                layer = this.SelectedEntity as Layer;
            }
            if (layer == null)
                polygon = new ZonedPolygon(this.EManager.Layers[0], this.PolygonDefinitionIn);
            else
                polygon = new ZonedPolygon(layer, this.PolygonDefinitionIn);

            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
            this.SelectedEntity = polygon;
            // regenerate viewport display data
            RegenerateGeometry();
        }

        private bool CanExecute_OnTransferPolygonDefinionCommand()
        {
            bool canExecute = (this.PolygonDefinitionIn != null && this.PolygonDefinitionIn.Count > 2);
            if (!canExecute)
            {
                MessageBox.Show("Not enough points for creating a polygon.\nCheck if the chosen element contains a connected polyline.",
                                "Zone Polygon Definition", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return canExecute;
        }

        #endregion

        #region COMMANDS: Programmatical Deselction
        
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

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================= COMMANDS FOR POLYGON STRUCTURAL PARTS: VERTICES, OPENINGS ====================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMANDS: Switch POLYGON Edit Mode Type (process POLY_LABELS_EDIT and OPENING_EDIT)
        private void OnSwitchPolygonEditModeTypeCommand(object _mode)
        {
            if (_mode == null || this.EManager.SelectedPolygon == null)
                return;

            // process objects (here we are returning from the mode, not entering it!)
            switch (this.PolygonEditModeType)
            {
                case ZonePolygonEditModeType.VERTEX_EDIT:
                case ZonePolygonEditModeType.VERTEX_ADD:
                case ZonePolygonEditModeType.VERTEX_REMOVE:
                case ZonePolygonEditModeType.POLY_REVERSE:
                case ZonePolygonEditModeType.POLY_LABELS_DEFAULT:
                case ZonePolygonEditModeType.OPENING_REMOVE:
                case ZonePolygonEditModeType.ISBEING_DELETED:
                    break;
                case ZonePolygonEditModeType.POLY_LABELS_EDIT:
                    this.EManager.SetPolygonLabels();
                    this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
                    this.UpdateGeometry(this.SelectedEntity.ID);
                    break;
                case ZonePolygonEditModeType.OPENING_EDIT:
                    this.EManager.ModifyAllOpeningsOfSelectedPolygon();
                    this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
                    this.UpdateGeometry(this.SelectedEntity.ID);
                    break;
                case ZonePolygonEditModeType.OPENING_ADD:
                    this.feedbackGeometry.Instances = new List<SharpDX.Matrix>();
                    this.feedbackGeometry.Visibility = Visibility.Collapsed;
                    this.PolygonOpeningFirstPointDefined = false;
                    this.openingFirstPoint = new Point3D(0, 0, 0);
                    this.openingIndexInPolygon = -1;
                    break;
                default:
                    break;
            }

            // PERFORM THE ACTUAL SWITCH
            // twice the same mode turns it off
            ZonePolygonEditModeType input = ZonedPolygon.GetEditModeType(_mode.ToString());
            if (input == this.prevPolygonEditModeType)
            {
                this.EManager.SelectedPolygon.EditModeType = ZonePolygonEditModeType.NONE;
                prevPolygonEditModeType = ZonePolygonEditModeType.NONE;
            }
            else
            {
                this.EManager.SelectedPolygon.EditModeType = input;
                prevPolygonEditModeType = input;
            }

            this.PolygonEditModeType = this.EManager.SelectedPolygon.EditModeType;
        }
        #endregion

        #region COMMANDS: Batch Label Editing

        private void OnBatchSetPolygonLabels(object _input)
        {
            this.EManager.SetPolygonLabels(_input.ToString());
            this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
            this.UpdateGeometry(this.SelectedEntity.ID);
        }

        private bool CanExecute_OnBatchSetPolygonLabels(object _input)
        {
            return (_input != null && this.PolygonEditModeType == ZonePolygonEditModeType.NONE && this.SelectedEntityIsPolygon);
        }

        #endregion

        #region COMMANDS: Vertex Editing

        private void OnReversePolygonCommand()
        {
            this.EManager.ReverseSelectedPolygon();
            this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
            this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
            UpdateGeometry(this.SelectedEntity.ID);           
            this.selectedGeometry.Geometry = null;
            this.selectedGeometry.Tag = -1;
        }

        private bool CanExecute_OnReversePolygonCommand()
        {
            return (this.SelectedEntityIsPolygon && this.ZoneEditMode == ZoneEditType.POLYGON_VERTEX);
        }

        private void OnDeleteVertexCommand()
        {
            this.EManager.RemoveVertexFromSelectedPolygon(this.SelectedVertex);
            this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
            this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
            UpdateGeometry(this.SelectedEntity.ID);
            this.SelectedVertex = null;
            this.selectedGeometry.Geometry = null;
            this.selectedGeometry.Tag = -1;
        }

        private bool CanExecute_OnDeleteVertexCommand()
        {
            return (this.ZoneEditMode == ZoneEditType.POLYGON_VERTEX && 
                this.EManager.SelectedPolygon != null && this.SelectedVertex != null);
        }

        private void OnResetPolygonLabelsCommand()
        {
            this.EManager.ResetPolygonLabels();
            this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
            UpdateGeometry(this.SelectedEntity.ID);
            this.selectedGeometry.Geometry = null;
            this.selectedGeometry.Tag = -1;
        }

        private bool CanExecute_OnResetPolygonLabelsCommand()
        {
            return (this.SelectedEntityIsPolygon && this.ZoneEditMode == ZoneEditType.POLYGON_VERTEX);
        }

        #endregion

        #region COMMANDS: Openings Editing

        private void OnDeleteOpeningCommand()
        {
            this.EManager.RemoveOpeningFromSelectedPolygon(this.SelectedOpening);
            this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
            UpdateGeometry(this.SelectedEntity.ID);
            this.SelectedOpening = null;
            this.selectedGeometry.Geometry = null;
            this.selectedGeometry.Tag = -1;
        }
        private bool CanExecute_OnDeleteOpeningCommand()
        {
            return (this.ZoneEditMode == ZoneEditType.POLYGON_OPENING && 
                    this.EManager.SelectedPolygon != null && this.SelectedOpening != null);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================= METHODS AND COMMANDS FOR ZONED VOLUME CREATION AND EDITING ===================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CREATE

        private void OnCreateZonedVolumeByExtrusionCommand()
        {
            if (this.SelectedEntity == null || !this.SelectedEntityIsPolygon)
                return;
            
            ZonedPolygon first = this.EManager.SelectedPolygon;
            if (first == null)
                return;

            Layer layer = this.EManager.SelectedPolygon.EntityLayer;
            ZonedVolume volume = new ZonedVolume(layer, first, new Vector3(0, first.Height, 0));
            volume.PropertyChanged += volume_PropertyChanged;
            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
            // regenerate viewport display data
            RegenerateGeometry();
        }

        private bool CanExecute_OnCreateZonedVolumeByExtrusionCommand()
        {
            return (this.SelectedEntity != null && this.SelectedEntityIsPolygon);
        }

        private void CreateZonedVolumeFromSelection()
        {
            if (this.SelectedEntity == null || !this.SelectedEntityIsPolygon)
                return;

            Layer layer = this.EManager.SelectedPolygon.EntityLayer;
            ZonedVolume volume = new ZonedVolume(layer, this.zoned_volume_ruling_polygons);
            volume.PropertyChanged += volume_PropertyChanged;
            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
            // regenerate viewport display data
            RegenerateGeometry();
        }

        void volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ZonedVolume zv = sender as ZonedVolume;
            if (e == null || zv == null)
                return;

            if (e.PropertyName == "IsDirty")
            {
                if (zv.IsDirty)
                    this.UpdateGeometry(zv.ID);
            }
            else if (e.PropertyName == "Visibility")
            {
                this.VolumeVisiblityChanged = !(this.VolumeVisiblityChanged);
            }
        }


        private void CreateZonedVolumeFromLevels()
        {
            Layer layer = this.EManager.Layers[0];
            if (this.LayerOfSelectedEntity != null)
                layer = this.LayerOfSelectedEntity;

            ZonedVolume volume = new ZonedVolume(layer, this.zoned_volume_ruling_levels);
            volume.PropertyChanged += volume_PropertyChanged;
            // publish data to GUI
            this.ZoneLayers = new List<Layer>(this.EManager.Layers);
            this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
            // regenerate viewport display data
            RegenerateGeometry();
        }

        #endregion

        #region COMMANDS: Switch VOLUME Edit Mode Type (process creation of volumes w split-levels or holes)

        private void OnSwitchVolumeEditModeTypeCommand(object _mode)
        {
            if (_mode == null)
                return;

            // process objects (here we are returning from the mode, not entering it!)
            switch (this.VolumeEditModeType)
            {
                case ZonedVolumeEditModeType.LEVEL_ADD:
                    // create a level:
                    int nrL = this.zoned_volume_ruling_levels.Count;
                    this.zoned_volume_ruling_levels.Add(new ZonedPolygonGroup("level_" + nrL, this.zoned_volume_ruling_polygons));
                    this.zoned_volume_ruling_polygons = new List<ZonedPolygon>();
                    this.ZonedVolumeLevels = new List<ZonedPolygonGroup>(this.zoned_volume_ruling_levels);
                    break;
                case ZonedVolumeEditModeType.LEVEL_DELETE:
                    break;
                case ZonedVolumeEditModeType.MATERIAL_ASSIGN:
                    break;
                default:
                    // reset feedback geometry
                    this.feedbackGeometry.Instances = new List<SharpDX.Matrix>();
                    this.feedbackGeometry.Visibility = Visibility.Collapsed;
                    break;
            }

            // PERFORM THE ACTUAL SWITCH
            // twice the same mode turns it off
            ZonedVolumeEditModeType input = ZonedVolume.GetEditModeType(_mode.ToString());
            if (input == this.prevVolumeEditModeType)
            {
                if (this.EManager.SelectedVolume != null)
                    this.EManager.SelectedVolume.EditModeType = ZonedVolumeEditModeType.NONE;
                prevVolumeEditModeType = ZonedVolumeEditModeType.NONE;
                this.VolumeEditModeType = ZonedVolumeEditModeType.NONE;
            }
            else
            {
                if (this.EManager.SelectedVolume != null)
                    this.EManager.SelectedVolume.EditModeType = input;
                prevVolumeEditModeType = input;
                this.VolumeEditModeType = input;
            }
        }

        #endregion

        #region COMMANDS: Edit Volume

        private void OnDeleteVolumeLevelCommand()
        {
            this.zoned_volume_ruling_levels.Remove(this.SelectedVolumeLevel);
            this.SelectedVolumeLevel = null;
            this.ZonedVolumeLevels = new List<ZonedPolygonGroup>(this.zoned_volume_ruling_levels);
        }

        private bool CanExecute_OnDeleteVolumeLevelCommand()
        {
            return (this.zoned_volume_ruling_levels.Count > 0 && this.SelectedVolumeLevel != null);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================ METHODS AND COMMANDS FOR DXF EXPORT AND IMPORT ============================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMANDS: Export DXF / Save DXF

        private void OnReadGeometryFromDXF()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + EntityDXF.DXFUtils.FILE_EXT_GEOMETRY
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFDecoder dxf_decoder = new DXFDecoder(this.EManager, this.MLManager);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        this.UpdateAfterMaterialMangerChange();
                        this.UpdateAfterGeometryManagerChange();
                        this.EManager.CleanUpAfterParsing();
                    }
                }
            }
            catch (Exception ex)
            {
                ZoneGroupDisplay.ERR_MESSAGES += ex.Message + "\n";
                MessageBox.Show(ex.Message, "Custom Geometry DXF Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Custom Geometry DXF Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void OnSaveGeometry()
        {
            this.SelectedEntity = null;
            try
            {
                // Configure save file dialog box
                string file_name = "ProjectGeometry";
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = file_name,                                           // Default file name
                    DefaultExt = EntityDXF.DXFUtils.FILE_EXT_GEOMETRY,              // Default file extension
                    Filter = "dxf files|*." + EntityDXF.DXFUtils.FILE_EXT_GEOMETRY  // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export strings
                    // MATERIALS fisrt!!!
                    StringBuilder export_MAT = this.MLManager.ExportMaterials(false);
                    string content_MAT = export_MAT.ToString();
                    StringBuilder export_GEOM = this.EManager.ExportEntites(true);
                    string content_GEOM = export_GEOM.ToString();
                    string content = string.Concat(content_MAT, content_GEOM);

                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Selection as DXF", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Selection as DXF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecute_OnSaveGeometry()
        {
            return (this.EManager != null && this.MLManager != null);
        }

        private void OnExportSelectedToACAD(bool _only_selected)
        {
            try
            {
                // Configure save file dialog box
                string file_name = (_only_selected) ? this.SelectedEntity.EntityName : "Geometry";
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = file_name,                      // Default file name
                    DefaultExt = ".dxf",                       // Default file extension
                    Filter = "dxf files|*.dxf"                 // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export string
                    StringBuilder export;
                    if (_only_selected)
                        export = this.EManager.ExportSelectedForACAD();
                    else
                        export = this.EManager.ExportEntitiesForACAD();
                    string content = export.ToString();
                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Selection as DXF", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Selection as DXF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecute_OnExportSelectedToACAD(bool _only_selected)
        {
            if (_only_selected)
                return (this.EManager != null && this.SelectedEntity != null &&
                    (this.SelectedEntity is ZonedPolygon || this.SelectedEntity is ZonedVolume));
            else
                return (this.EManager != null);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= UTILS ================================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MATERIALS

        public void UpdateMaterials()
        {
            this.MaterialLibrary = new List<ComponentInteraction.Material>(this.MLManager.Materials);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== EVENT HANDLERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MOUSE: Polygon Selection, Adding Vertices, Openings, Volumes
        
        private void polygon_MouseDown3D(object sender, RoutedEventArgs e)
        {
            Viewport3DXext vpext = this.Parent as Viewport3DXext;
            if (vpext != null && vpext.ActionMode != Communication.ActionType.BUILDING_PHYSICS)
                return;

            // release feedback geometry for polygon editing
            this.feedbackGeometry.Instances = new List<SharpDX.Matrix>();
            this.feedbackGeometry.Visibility = Visibility.Collapsed;
            
            SelectableUserLine model = sender as SelectableUserLine;
            if (model != null && model.Tag != null)
            {
                if (this.ZoneEditMode == ZoneEditType.NO_EDIT)
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
                else if (this.ZoneEditMode == ZoneEditType.POLYGON_VERTEX &&
                            this.PolygonEditModeType == ZonePolygonEditModeType.VERTEX_ADD)
                {
                    // adding a vertex
                    if (model.IndexSelected.HasValue)
                    {
                        this.EManager.AddVertexToSelectedPolygon(model.HitPos, (int)model.IndexSelected.Value);
                        this.ZonePolygonVertices = new List<ZonedPolygonVertexVis>(this.EManager.VerticesOfSelectedPolygon);
                        this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
                        this.UpdateGeometry(this.SelectedEntity.ID);
                        this.SelectedVertex = this.ZonePolygonVertices[(int)model.IndexSelected.Value + 1];
                    }
                }
                else if (this.ZoneEditMode == ZoneEditType.POLYGON_OPENING && 
                            this.PolygonEditModeType == ZonePolygonEditModeType.OPENING_ADD)
                {
                    this.MouseHandler_Opening_Add(model);
                }
                else if (this.ZoneEditMode == ZoneEditType.VOLUME_CREATE)
                {
                    // creating a volume by selecting additional polygons
                    this.MouseHandler_Volume_Create(model);
                }
                else if (this.ZoneEditMode == ZoneEditType.VOLUME_CREATE_COMPLEX)
                {
                    // creating a volume by creating levels (zoned polygon groups)
                    this.MouseHandler_Volume_CreateComplex(model);
                }
                else if (this.ZoneEditMode == ZoneEditType.POLYGON_SPLIT)
                {
                    // split polygon along another                   
                    this.MouseHandler_PolygonSplit(model);
                }
            }  
        }

        private void polygon_MouseUp3D(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Mouse: Volume Selection, external Picking

        public Point3D HitPointOnVolumeMesh { get; private set; }

        /// <summary>
        /// For external mesh selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void volume_MouseDown3D(object sender, RoutedEventArgs e)
        {
            Viewport3DXext vpext = this.Parent as Viewport3DXext;
            if (vpext != null && vpext.ActionMode != Communication.ActionType.BUILDING_PHYSICS)
                return;

            UserMesh model = sender as UserMesh;
            if (model != null && model.Tag != null)
            {
                this.HitPointOnVolumeMesh = model.HitPoint; // added 30.08.2017
                if (this.ZoneEditMode == ZoneEditType.NO_EDIT || this.ZoneEditMode == ZoneEditType.VOLUME_PICK)
                {
                    // selection handling: volumes
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
        }

        #endregion

        #region MOUSE HANDLER: Adding Opening to a Polygon

        private void MouseHandler_Opening_Add(SelectableUserLine model)
        {
            int polygonIdCheck = -1;
            int.TryParse(model.Tag.ToString(), out polygonIdCheck);
            if (polygonIdCheck != (int)this.SelectedEntity.ID)
                return;


            // define first anchor point of the polygon opening
            if (!this.PolygonOpeningFirstPointDefined && model.IndexSelected.HasValue)
            {
                this.openingFirstPoint = model.HitPos;
                this.openingIndexInPolygon = (int)model.IndexSelected.Value;
                this.PolygonOpeningFirstPointDefined = true;

                // give visual feedback
                this.feedbackGeometry.Instances = new List<SharpDX.Matrix> 
                { 
                    Matrix.Translation(this.openingFirstPoint.ToVector3()) 
                };
                this.feedbackGeometry.Visibility = Visibility.Visible;
            }
            // define second anchor point of the polygon opening
            else if (this.PolygonOpeningFirstPointDefined && model.IndexSelected.HasValue)
            {
                if ((int)model.IndexSelected.Value != this.openingIndexInPolygon)
                {
                    // give visual feedback
                    this.feedbackGeometry.Instances = new List<SharpDX.Matrix> 
                    { 
                        Matrix.Translation(this.openingFirstPoint.ToVector3()),
                        Matrix.Translation(model.HitPos.ToVector3())
                    };
                    this.feedbackGeometry.Visibility = Visibility.Visible;

                    string message = "Both points must lie on the same polygon segment!\nPick the second point again...";
                    string caption = "Polygon Opening Defintion";
                    MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    this.EManager.AddOpeningToSelectedPolygon(this.openingFirstPoint, model.HitPos,
                                                                (int)model.IndexSelected.Value, 1f, 1f);
                    this.ZonePolygonOpenings = new List<ZoneOpeningVis>(this.EManager.OpeningsOfSelectedPolygon);
                    this.UpdateGeometry(this.SelectedEntity.ID);
                    this.SelectedOpening = this.ZonePolygonOpenings.Last();
                    this.PolygonOpeningFirstPointDefined = false;

                    // give visual feedback
                    this.feedbackGeometry.Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion

        #region MOUSE HANDLER: Selecting Polygons for Zoned Volume Definition

        private void MouseHandler_Volume_Create(SelectableUserLine model)
        {
            long tag = -1;
            long.TryParse(model.Tag.ToString(), out tag);
            foreach (GeometricEntity ge in this.allGE)
            {
                ZonedPolygon zp = ge as ZonedPolygon;
                if (zp != null && zp.ID == tag)
                {
                    if (!this.zoned_volume_ruling_polygons.Contains(zp))
                    {
                        this.zoned_volume_ruling_polygons.Add(zp);

                        // give visual feedback
                        this.feedbackGeometry.Instances = GetFeedBackInstancesForVolumeCreation(this.zoned_volume_ruling_polygons);
                        this.feedbackGeometry.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        string message = "Polygon selected twice.\nEach polygon can be used only once in a volume definition.";
                        string caption = "Volume Defintion";
                        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                        this.feedbackGeometry.Visibility = Visibility.Visible;
                    }
                    break;
                }
            }
        }

        private static List<SharpDX.Matrix> GetFeedBackInstancesForVolumeCreation(List<ZonedPolygon> _polygons)
        {
            if (_polygons == null || _polygons.Count < 1)
                return new List<Matrix>{ Matrix.Identity };

            List<SharpDX.Matrix> instances = new List<Matrix>();
            foreach (ZonedPolygon zp in _polygons)
            {
                foreach(Point3D coord in zp.Polygon_Coords)
                {
                    instances.Add(Matrix.Translation(coord.ToVector3()));
                }
            }
            return instances;
        }

        #endregion

        #region MOUSE HANDLER: Selecting Polygons for a Volume LEVEL Definition

        private void MouseHandler_Volume_CreateComplex(SelectableUserLine model)
        {
            if (this.VolumeEditModeType == ZonedVolumeEditModeType.LEVEL_ADD)
            {
                MouseHandler_Volume_Create(model);
            }
        }


        #endregion

        #region MOUSE HANDLER: Splitting Polygons

        private void MouseHandler_PolygonSplit(SelectableUserLine _model)
        {
            if (this.poly_to_split_1 == null && this.poly_to_split_2 == null)
            {
                // POLYGON SELECTION

                this.poly_to_split_1 = this.SelectedEntity as ZonedPolygon;
                if (this.poly_to_split_1 == null || _model == null)
                {
                    this.OnSwitchZoneEditModeCommand(ZoneEditType.POLYGON_SPLIT);
                    return;
                }

                long tag = -1;
                long.TryParse(_model.Tag.ToString(), out tag);
                if (tag < 0)
                {
                    this.OnSwitchZoneEditModeCommand(ZoneEditType.POLYGON_SPLIT);
                    return;
                }

                if (tag == this.poly_to_split_1.ID)
                {
                    MessageBox.Show("The same polygon cannot be selected twice!", "Split Polygons", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (GeometricEntity ge in this.allGE)
                {
                    ZonedPolygon zp = ge as ZonedPolygon;
                    if (zp != null && zp.ID == tag)
                    {
                        this.poly_to_split_2 = zp;
                        // give visual feedback
                        this.feedbackGeometry.Instances = GetFeedBackInstancesForVolumeCreation(new List<ZonedPolygon> { zp });
                        this.feedbackGeometry.Visibility = Visibility.Visible;
                        MessageBox.Show("Select the positions of the 2 splitting points...", "Split Polygons", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
                }
                // reset points
                this.poly_split_nr_chosen_points = 0;
            }
            else
            {
                if (this.poly_to_split_1 == null || this.poly_to_split_2 == null || _model == null)
                {
                    this.OnSwitchZoneEditModeCommand(ZoneEditType.POLYGON_SPLIT);
                    return;
                }

                // SPLIT POINT SELECTION
                if (this.poly_split_nr_chosen_points == 0)
                {
                    if (_model.IndexSelected.HasValue)
                    {
                        this.poly_split_Point_A = _model.HitPos;
                        // give visual feedback
                        this.feedbackGeometry.Instances = new List<SharpDX.Matrix> 
                        { 
                            Matrix.Translation(this.poly_split_Point_A.ToVector3()) 
                        };
                        this.feedbackGeometry.Visibility = Visibility.Visible;
                        this.poly_split_nr_chosen_points++;
                    }                        
                }
                else
                {
                    if (_model.IndexSelected.HasValue)
                    {
                        this.poly_split_Point_B = _model.HitPos;
                        // give visual feedback
                        this.feedbackGeometry.Instances = new List<SharpDX.Matrix> 
                        { 
                            Matrix.Translation(this.poly_split_Point_A.ToVector3()),
                            Matrix.Translation(this.poly_split_Point_B.ToVector3())
                        };
                        this.feedbackGeometry.Visibility = Visibility.Visible;
                        this.poly_split_nr_chosen_points++;
                    } 

                    // ALL INFO COMPLETE -> perform split
                    double tolerance = 0.1; //  Utils.CommonExtensions.LINEDISTCALC_TOLERANCE;
                    List<Vector3> poly_1, poly_2, poly_3, poly_4;
                    Utils.MeshesCustom.SplitPolygonAlongAnotherWIntersection(Utils.CommonExtensions.ConvertPoints3DListToVector3List(this.poly_to_split_1.Polygon_Coords), 
                                                                Utils.CommonExtensions.ConvertPoints3DListToVector3List(this.poly_to_split_2.Polygon_Coords),
                                                                this.poly_split_Point_A.ToVector3(), this.poly_split_Point_B.ToVector3(), tolerance,
                                                                out poly_1, out poly_2, out poly_3, out poly_4);
                    this.AddPolygonToEntities(poly_1, "Split_Polygon_1");
                    this.AddPolygonToEntities(poly_2, "Split_Polygon_2");
                    this.AddPolygonToEntities(poly_3, "Split_Polygon_3");
                    this.AddPolygonToEntities(poly_4, "Split_Polygon_4");

                    // publish data to GUI
                    this.ZoneLayers = new List<Layer>(this.EManager.Layers);
                    this.ZoneLayersFlat = new List<Layer>(this.EManager.GetFlatLayerList());
                    this.SelectedEntity = null;
                    // reset feedback geometry
                    this.feedbackGeometry.Visibility = System.Windows.Visibility.Collapsed;
                    // regenerate viewport display data
                    RegenerateGeometry();
                    // exit the editing mode
                    this.OnSwitchZoneEditModeCommand(ZoneEditType.POLYGON_SPLIT);
                }
            }

        }

        private void AddPolygonToEntities(List<Vector3> _vertices, string _name)
        {
            if (_vertices == null || _vertices.Count < 3) return;

            List<Point3D> vertex_points = Utils.CommonExtensions.ConvertVector3ListToPoint3DList(_vertices);

            ZonedPolygon polygon;
            Layer layer = null;
            if (this.SelectedEntityIsPolygon)
            {
                layer = (this.SelectedEntity as ZonedPolygon).EntityLayer;
            }
            if (layer == null)
                polygon = new ZonedPolygon(_name, this.EManager.Layers[0], vertex_points);
            else
                polygon = new ZonedPolygon(_name, layer, vertex_points);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ STATIC DATA =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC CONSTANTS

        private static int MODELS_OFFSET = 1;

        private static readonly double START_MARKER_SIZE = 0.1;
        private static readonly double ZONE_TEXT_SIZE = 0.25;
        private static readonly double OPENING_MARKER_SIZE = 0.15;
        private static readonly double THICKNESS_DEFAULT = 1;
        private static readonly double THICKNESS_INFO = 0.5;
        private static readonly double THICKNESS_SELECTED_DEFAULT = 3;
        private static readonly Color Color_Selected = Color.Yellow;
        private static readonly Color Color_FeedBack = Color.Cyan;
        private static readonly Color Color_FeedBack_Extra = Color.Blue;
        private static readonly Color Color_Inactive = Color.DimGray;
        private static PhongMaterial VolumeMat;
        private static PhongMaterial SelectMat;
        private static PhongMaterial AlertMat;
        private static PhongMaterial OffsetMat;
        private static double OPACITY_DEF = 0.65;
        private static double OPACITY_MID = 0.3;
        private static double OPACITY_LOW = 0.1;
        private static float OPACITY_RADIUS_FACT = 1;

        #endregion

        #region STATIC VARIABLES

        protected static string ERR_MESSAGES = string.Empty;

        #endregion

    }
}
