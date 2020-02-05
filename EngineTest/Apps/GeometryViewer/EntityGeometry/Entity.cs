using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.EntityDXF;

namespace GeometryViewer.EntityGeometry
{
    public enum EntityVisibility { ON, OFF, HALF, UNKNOWN }
    public enum EnityGeometryType { NONE, LINE, MESH }

    public abstract class Entity : INotifyPropertyChanged
    {
        internal static long Nr_Entities = 0;
        private static List<string> Entity_Names = new List<string>();
        private static SharpDX.Color Default_Color = SharpDX.Color.Black;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #region Visibility Utilities
        public static EntityVisibility ConvertStringToVis(string _vis)
        {
            if (_vis == null)
                return EntityVisibility.UNKNOWN;
            switch(_vis)
            {
                case "ON":
                    return EntityVisibility.ON;
                case "OFF":
                    return EntityVisibility.OFF;
                case "HALF":
                    return EntityVisibility.HALF;
                default:
                    return EntityVisibility.UNKNOWN;
            }
        }

        private int Visibility2SaveCode()
        {
            switch (this.Visibility)
            {
                case EntityVisibility.ON:
                    return 1;
                case EntityVisibility.OFF:
                    return 0;
                case EntityVisibility.HALF:
                    return 2;
                default:
                    return -11;
            }
        }

        public static EntityVisibility SaveCode2Visibility(int _code)
        {
            switch (_code)
            {
                case 1:
                    return EntityVisibility.ON;
                case 0:
                    return EntityVisibility.OFF;
                case 2:
                    return EntityVisibility.HALF;
                default:
                    return EntityVisibility.UNKNOWN;
            }
        }

        #endregion

        #region Identification: ID, EntityName
        public long ID { get; private set; }

        private string entityName;
        public string EntityName 
        {
            get { return this.entityName; } 
            set
            {
                if (value == null || value.Count() < 1)
                {
                    this.entityName = this.ID + ": " + this.GetDafaultName();
                    Entity.Entity_Names.Add(this.entityName);
                }
                else
                {
                    if (Entity.Entity_Names.Contains(value))
                    {
                        this.entityName = this.ID + ": " + this.GetDafaultName();
                        Entity.Entity_Names.Add(this.entityName);
                    }
                    else
                    {
                        this.entityName = value;
                    }
                }
                RegisterPropertyChanged("EntityName");
            }
        }

        #endregion

        #region Apperance: Color, Visibility

        private SharpDX.Color entityColor;
        public SharpDX.Color EntityColor
        {
            get { return this.entityColor; }
            set
            {
                if (value == null)
                    this.entityColor = Entity.Default_Color;
                else
                    this.entityColor = value;

                RegisterPropertyChanged("EntityColor");
            }
        }

        private EntityVisibility visibility;
        public EntityVisibility Visibility
        {
            get { return this.visibility; } 
            set
            {
                this.visibility = value;
                
                // propagate change to contained entities
                if (this.ContainedEntities != null)
                {
                    if (this.visibility != EntityVisibility.ON)
                    {
                        foreach (Entity e in this.ContainedEntities)
                        {
                            if (e.Visibility == EntityVisibility.ON)
                            {
                                e.Visibility = EntityVisibility.HALF;
                            }
                        }
                    }
                    else
                    {
                        foreach (Entity e in this.ContainedEntities)
                        {
                            if (e.Visibility == EntityVisibility.HALF)
                            {
                                e.Visibility = EntityVisibility.ON;
                            }
                        }
                    }
                }

                RegisterPropertyChanged("Visibility");
            }
        }

        #endregion

        #region Logic: Validity, Contained Entites, Associations

        private bool isValid;
        public bool IsValid
        {
            get { return this.isValid; } 
            protected set
            {
                this.isValid = value;
                RegisterPropertyChanged("IsValid");
            }
        }

        public virtual List<Entity> ContainedEntities { get; protected set; }

        protected bool associated_w_comp;
        public bool AssociatedWComp
        {
            get { return associated_w_comp; }
            protected set 
            { 
                associated_w_comp = value;
                RegisterPropertyChanged("AssociatedWComp");
            }
        }


        #endregion

        #region GUI Controls General TreeView: Selected, Expanded

        private bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                RegisterPropertyChanged("IsExpanded");
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                RegisterPropertyChanged("IsSelected");
            }
        }

        #endregion

        #region GUI Controls for Polygons: Labels(Zones), CtrlPoints, Geometry

        private bool showZones;
        public bool ShowZones
        {
            get { return this.showZones; }
            set
            {
                this.showZones = value;
                RegisterPropertyChanged("ShowZones");
            }
        }

        private bool showCtrlPoints;
        public bool ShowCtrlPoints
        {
            get { return this.showCtrlPoints; }
            set
            {
                this.showCtrlPoints = value;
                RegisterPropertyChanged("ShowCtrlPoints");
            }
        }

        public bool HasGeometry { get; protected set; }
        public EnityGeometryType HasGeometryType { get; protected set; }

        #endregion

        #region GUI Controls Architecture: Line Thickness, Contained Text

        private float lineThicknessGUI;
        public float LineThicknessGUI
        {
            get { return this.lineThicknessGUI; }
            protected set
            {
                this.lineThicknessGUI = value;
                RegisterPropertyChanged("LineThicknessGUI");
            }
        }

        private List<string> text;
        public virtual List<string> Text
        {
            get { return this.text; }
            set
            {
                this.text = value;
                RegisterPropertyChanged("Text");
            }
        }

        #endregion

        #region GUI Controls Building Physics: Levels

        private bool isTopClosure;
        public bool IsTopClosure
        {
            get { return this.isTopClosure; }
            set 
            { 
                this.isTopClosure = value;
                if (this.isTopClosure)
                    this.IsBottomClosure = false;
                RegisterPropertyChanged("IsTopClosure");
            }
        }

        private bool isBottomClosure;

        public bool IsBottomClosure
        {
            get { return this.isBottomClosure; }
            set 
            { 
                this.isBottomClosure = value;
                if (this.isBottomClosure)
                    this.IsTopClosure = false;
                RegisterPropertyChanged("IsBottomClosure");
            }
        }


        #endregion

        #region CONSTRUCTORS
        public Entity()
        {
            this.ID = (++Entity.Nr_Entities);
            this.EntityName = null;
            this.EntityColor = Entity.Default_Color;
            this.Visibility = EntityVisibility.ON;
            this.IsValid = true;
            this.ContainedEntities = null;
            this.AssociatedWComp = false;
            this.ShowZones = false;
            this.ShowCtrlPoints = true;
            this.HasGeometry = false;
            this.HasGeometryType = EnityGeometryType.NONE;
            this.IsExpanded = false;
            this.LineThicknessGUI = 1f;
            this.Text = new List<string>();
            this.IsTopClosure = false;
            this.IsBottomClosure = false;
        }

        public Entity(string _name)
        {
            this.ID = (++Entity.Nr_Entities);
            this.EntityName = _name;
            this.EntityColor = Entity.Default_Color;
            this.Visibility = EntityVisibility.ON;
            this.IsValid = true;
            this.ContainedEntities = null;
            this.AssociatedWComp = false;
            this.ShowZones = false;
            this.ShowCtrlPoints = true;
            this.HasGeometry = false;
            this.HasGeometryType = EnityGeometryType.NONE;
            this.IsExpanded = false;
            this.LineThicknessGUI = 1f;
            this.Text = new List<string>();
            this.IsTopClosure = false;
            this.IsBottomClosure = false;
        }

        public Entity(string _name, SharpDX.Color _color)
        {
            this.ID = (++Entity.Nr_Entities);
            this.EntityName = _name;
            this.EntityColor = _color;
            this.Visibility = EntityVisibility.ON;
            this.IsValid = true;
            this.ContainedEntities = null;
            this.AssociatedWComp = false;
            this.ShowZones = false;
            this.ShowCtrlPoints = true;
            this.HasGeometry = false;
            this.HasGeometryType = EnityGeometryType.NONE;
            this.IsExpanded = false;
            this.LineThicknessGUI = 1f;
            this.Text = new List<string>();
            this.IsTopClosure = false;
            this.IsBottomClosure = false;
        }

        #endregion

        #region PARSING .CTOR

        internal Entity(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp, 
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure)
        {
            this.ID = _id;
            this.EntityName = _name;
            this.EntityColor = _color;
            this.visibility = _vis;
            this.IsValid = true;
            this.AssociatedWComp = _assoc_w_comp;
            this.LineThicknessGUI = _line_thickness_GUI;
            this.Text = _mLtext;
            this.IsTopClosure = _is_top_closure;
            this.IsBottomClosure = _is_bottom_closure;

            this.ContainedEntities = null; // will be handled separately in the specialised .ctors
            
            this.ShowZones = false;
            this.ShowCtrlPoints = true;
            this.HasGeometry = false;
            this.HasGeometryType = EnityGeometryType.NONE;
            this.IsExpanded = false;
            this.IsSelected = false;
        }

        #endregion

        #region DEBUG STRUCTURE

        public string EStructure
        {
            get
            {
                string str = this.EntityName + "{";
                foreach(Entity e in this.ContainedEntities)
                {
                    str += e.EStructure;
                }
                str += "}";
                return str;
            }
        }

        #endregion

        public virtual string GetDafaultName()
        {
            return "Entity";
        }

        #region DXF Export

        public virtual void AddToExport(ref StringBuilder _sb, bool _with_contained_entites)
        {
            if (_sb == null) return;
            string tmp = string.Empty;

            // THE FOLLOWING 4 LINES ARE TO BE CALLED IN EACH NON-ABSTRACT SUBCLASS TO REFLECT THE DYNAMIC TYPE
            // ===============================================================================================
            //_sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());  // 0
            //_sb.AppendLine(DXFUtils.GV_ENTITY);                             // GV_ENTITY

            //_sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());    // 100 (subclass marker)
            //_sb.AppendLine(this.GetType().ToString());                      // GeometryViewer.EntityGeometry.Entity
            // ===============================================================================================
            
            // GENERAL
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_ID).ToString());     // 900 (custom code)
            _sb.AppendLine(this.ID.ToString());                             // e.g. 254

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());   // 2
            _sb.AppendLine(this.EntityName);                                // e.g. "Entity 254"

            _sb.AppendLine(((int)EntitySaveCode.VISIBILITY).ToString());    // 60
            _sb.AppendLine(this.Visibility2SaveCode().ToString());          // (0 = visible, 1 = invisible; custom: 2 = half, -11 = unknown)
            _sb.AppendLine(((int)EntitySaveCode.SPACE).ToString());         // 67       
            _sb.AppendLine("0");                                            // (absent or 0 = entity in model space, 1 = entity in paper space, default = 0)

            _sb.AppendLine(((int)DXFSpecSaveCodes.COLOR_INDEX).ToString()); // 62
            DXFImportExport.DXFColor dxf_color = new DXFImportExport.DXFColor((float)this.EntityColor.R, (float)this.EntityColor.G, (float)this.EntityColor.B, (float)this.EntityColor.A, true);
            int ic = DXFImportExport.DXFColor.DXFColor2Index(dxf_color);
            _sb.AppendLine(ic.ToString());

            _sb.AppendLine(((int)EntitySaveCode.TRUECOLOR).ToString());     // 420
            long tc = DXFImportExport.DXFColor.DXFColor2TrueColor(dxf_color);
            _sb.AppendLine(tc.ToString());                                  // 195(0..255)(0..255)(0..255)

            // SPECIFIC
            tmp = (this.IsValid) ? "1" : "0";
            _sb.AppendLine(((int)EntitySpecificSaveCode.VALIDITY).ToString());      // 1001
            _sb.AppendLine(tmp);                                                    // 1 = valid, 0 = invalid

            tmp = (this.AssociatedWComp) ? "1" : "0";
            _sb.AppendLine(((int)EntitySpecificSaveCode.ASSOC_W_OTHER).ToString());  // 1002
            _sb.AppendLine(tmp);                                                    // 1 = associated, 0 = not associated

            // ---CONTAINED ENTITIES START---
            // ------------------------------            
            if (this.ContainedEntities == null || this.ContainedEntities.Count == 0 || !_with_contained_entites)
            {
                _sb.AppendLine(((int)EntitySpecificSaveCode.CONTAINED_ENTITIES).ToString());    // 1003
                _sb.AppendLine("0");
            }
            else
            {
                _sb.AppendLine(((int)EntitySpecificSaveCode.CONTAINED_ENTITIES).ToString());    // 1003
                _sb.AppendLine(this.ContainedEntities.Count.ToString());

                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                  // 0
                _sb.AppendLine(DXFUtils.ENTITY_SEQUENCE);                                       // ENTSEQ

                foreach (Entity e in this.ContainedEntities)
                {
                    if (e != null)
                        e.AddToExport(ref _sb, _with_contained_entites);
                }

                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                  // 0
                _sb.AppendLine(DXFUtils.SEQUENCE_END);                                          // SEQEND
                _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                  // 0
                _sb.AppendLine(DXFUtils.ENTITY_CONTINUE);                                       // ENTCTN
            }            
            // ------------------------------
            // ---CONTAINED ENTITIES END  ---

            // ARC properties
            _sb.AppendLine(((int)EntitySpecificSaveCode.VISLINE_THICKNESS).ToString());         // 1004
            _sb.AppendLine(DXFUtils.ValueToString(this.LineThicknessGUI, "F8"));                // "0.25"

            foreach(string line in this.Text)
            {
                if (string.IsNullOrEmpty(line)) continue;

                _sb.AppendLine(((int)EntitySpecificSaveCode.TEXT_LINE).ToString());             // 1005
                _sb.AppendLine(line);
            }

            // BPH properties
            tmp = (this.IsTopClosure) ? "1" : "0";
            _sb.AppendLine(((int)EntitySpecificSaveCode.IS_TOP_CLOSURE).ToString());             // 1006
            _sb.AppendLine(tmp);

            tmp = (this.IsBottomClosure) ? "1" : "0";
            _sb.AppendLine(((int)EntitySpecificSaveCode.IS_BOTTOM_CLOSURE).ToString());          // 1007
            _sb.AppendLine(tmp);

            // signify end of complex entity
            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());                      // 0
            _sb.AppendLine(DXFUtils.SEQUENCE_END);                                              // SEQEND
        }

        public virtual void AddToACADExport(ref StringBuilder _sb, string _layer_name_visible, string _layer_name_hidden)
        {

        }

        #endregion
    }

    public class EntityComparer : IEqualityComparer<Entity>
    {
        public bool Equals(Entity _e1, Entity _e2)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(_e1, _e2)) return true;

            //Check whether any of the compared objects is null. 
            if (Object.ReferenceEquals(_e2, null) || Object.ReferenceEquals(_e2, null))
                return false;

            return (_e1.ID == _e2.ID);
        }

        // If Equals() returns true for a pair of objects  
        // then GetHashCode() must return the same value for these objects. 
        public int GetHashCode(Entity _e)
        {
            //Check whether the object is null 
            if (Object.ReferenceEquals(_e, null)) return 0;

            //Get hash code for the ID field
            return _e.ID.GetHashCode();
        }
    }

    [ValueConversion(typeof(EnityGeometryType), typeof(Boolean))]
    public class EntityGeometryToBooleanConverter : IValueConverter
    {
        private static EnityGeometryType GetEntityGeometry(string _geom)
        {
            if (_geom == null)
                return EnityGeometryType.NONE;

            switch (_geom)
            {
                case "LINE":
                    return EnityGeometryType.LINE;
                case "MESH":
                    return EnityGeometryType.MESH;
                default:
                    return EnityGeometryType.NONE;
            }
        }

        // in order to react to more than one geometry type at once
        // use + as OR
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            EnityGeometryType eg = EnityGeometryType.NONE;
            if (value is EnityGeometryType)
                eg = (EnityGeometryType)value;

            string str_param = parameter.ToString();
            string[] str_params_OR = str_param.Split(new char[] { '+' });

            if (str_params_OR.Count() < 2)
                return (eg == GetEntityGeometry(str_param));
            else
            {
                foreach (string p in str_params_OR)
                {
                    if (eg == GetEntityGeometry(p))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }

    [ValueConversion(typeof(System.Collections.IList), typeof(ListCollectionView))]
    public class TreeViewSortingEntityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Collections.IList collection = value as System.Collections.IList;
            if (collection == null || parameter == null)
                return null;

            ListCollectionView view = new ListCollectionView(collection);
            SortDescription sort = new SortDescription(parameter.ToString(), ListSortDirection.Ascending);
            view.SortDescriptions.Add(sort);

            return view;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

    }

}
