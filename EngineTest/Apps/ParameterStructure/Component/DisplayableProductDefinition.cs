using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace ParameterStructure.Component
{
    public abstract class DisplayableProductDefinition : INotifyPropertyChanged
    {
        #region STATIC UTILS: Info (hierarchical lists)

        public static List<T> FlattenHierarchicalRecord<T>(List<T> _record, bool _include_invisible = true) where T : DisplayableProductDefinition
        {
            if (_record == null) return null;

            List<T> record_flat = new List<T>();
            foreach (T item in _record)
            {
                item.IsMarkable = true;
                if (!_include_invisible && item.IsExcludedFromDisplay) continue;

                record_flat.Add(item);
                List<T> subItems = DisplayableProductDefinition.GetFlatSubElementList(item);
                foreach (T si in subItems)
                {
                    si.IsMarkable = false;
                }
                record_flat.AddRange(subItems);
            }

            return record_flat;
        }

        public static List<T> GetFlatSubElementList<T>(T _element) where T : DisplayableProductDefinition
        {
            if (_element.ContainedOfSameType == null || _element.ContainedOfSameType.Count < 1)
                return new List<T>();

            List<T> list = new List<T>();
            foreach (T item in _element.ContainedOfSameType)
            {
                if (item == null) continue;
                list.Add(item);
                if (item.ContainedOfSameType.Count > 0)
                    list.AddRange(DisplayableProductDefinition.GetFlatSubElementList(item));
            }
            return list;
        }

        // gets the top-level parent
        public static T GetParent<T>(T _element, List<T> _element_record) where T : DisplayableProductDefinition
        {
            if (_element == null || _element_record == null) return null;
            if (_element_record.Count == 0) return null;

            // top level
            if (_element_record.Contains(_element)) return null;

            foreach (T item in _element_record)
            {
                List<T> itemContent = DisplayableProductDefinition.GetFlatSubElementList(item);
                if (itemContent.Contains(_element))
                    return item;
            }

            return null;
        }

        public static List<T> GetParentChain<T>(T _element, List<T> _element_record) where T : DisplayableProductDefinition
        {
            if (_element == null || _element_record == null) return null;
            if (_element_record.Count == 0) return null;

            // if at top level
            if (_element_record.Contains(_element)) return new List<T> { _element };

            List<T> chain = new List<T>();

            // get top-level parent
            T topParent = null;
            foreach (T item in _element_record)
            {
                List<T> itemContent = DisplayableProductDefinition.GetFlatSubElementList(item);
                if (itemContent.Contains(_element))
                {
                    topParent = item;
                    break;
                }
            }

            if (topParent == null)
                return new List<T> { _element };

            // proceed from the top parent down
            chain.Add(topParent);
            T currentParent = topParent;
            while (currentParent.ID != _element.ID)
            {
                foreach (T item in currentParent.ContainedOfSameType)
                {
                    if (item == null)
                        continue;

                    if (item.ID == _element.ID)
                    {
                        currentParent = item;
                        break;
                    }

                    List<T> subNodeContent = DisplayableProductDefinition.GetFlatSubElementList(item);
                    if (subNodeContent.Contains(_element))
                    {
                        currentParent = item;
                        break;
                    }
                }
                chain.Add(currentParent);
            }
            return chain;
        }

        #endregion

        #region STATIC UTILS: Selection

        public static void SelectElement<T>(T _element, List<T> _element_record, ref List<T> _element_record_flat) where T : DisplayableProductDefinition
        {
            if (_element_record == null) return;
            if (_element_record.Count == 0) return;
            if (_element_record_flat == null)
                _element_record_flat = DisplayableProductDefinition.FlattenHierarchicalRecord(_element_record);

            foreach (T item in _element_record_flat)
            {
                if (_element == null || item.ID != _element.ID)
                {
                    item.IsSelected = false;
                    item.IsParentOfSelected = false;
                }
                else
                {
                    item.IsSelected = true;
                    List<T> parents = DisplayableProductDefinition.GetParentChain(item, _element_record);
                    if (parents != null && parents.Count > 0)
                    {
                        foreach (T parent in parents)
                        {
                            if (parent.ID == _element.ID)
                                continue;
                            parent.IsParentOfSelected = true;
                            parent.IsExpanded = true;
                        }
                    }
                }
            }
        }

        public static T SelectElement<T>(long _id, List<T> _element_record, ref List<T> _element_record_flat) where T : DisplayableProductDefinition
        {
            if (_id < 0) return null;
            if (_element_record == null) return null;
            if (_element_record.Count == 0) return null;
            if (_element_record_flat == null)
                _element_record_flat = DisplayableProductDefinition.FlattenHierarchicalRecord(_element_record);
            T selected = null;

            foreach (T item in _element_record_flat)
            {
                if (item == null || item.ID != _id)
                {
                    item.IsSelected = false;
                    item.IsParentOfSelected = false;
                }
                else
                {
                    item.IsSelected = true;
                    selected = item;
                    List<T> parents = DisplayableProductDefinition.GetParentChain(item, _element_record);
                    if (parents != null && parents.Count > 0)
                    {
                        foreach (T parent in parents)
                        {
                            if (parent.ID == _id)
                                continue;
                            parent.IsParentOfSelected = true;
                            parent.IsExpanded = true;
                        }
                    }
                }
            }

            return selected;
        }

        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion  

        #region PROPERTIES: General (ID, TypeName, Description)

        protected long id;
        public long ID
        {
            get { return this.id; }
            internal set
            {
                this.id = value;
                this.RegisterPropertyChanged("ID");
            }
        }

        protected string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.RegisterPropertyChanged("Name");
            }
        }

        protected string description;
        public string Description
        {
            get { return this.description; }
            set
            {
                this.description = value;
                this.RegisterPropertyChanged("Description");
            }
        }

        #endregion

        #region PROPERTIES: IsSelected, IsExpanded

        protected bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                this.RegisterPropertyChanged("IsExpanded");
            }
        }

        protected bool isSelected;
        public virtual bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                this.ParentIsSelected = this.isSelected;                
                this.RegisterPropertyChanged("IsSelected");
            }
        }

        // derived
        protected bool is_parent_of_selected;
        public bool IsParentOfSelected
        {
            get { return this.is_parent_of_selected; }
            set
            {
                this.is_parent_of_selected = value;
                this.RegisterPropertyChanged("IsParentOfSelected");
            }
        }

        // derived
        protected bool parent_is_selected;
        public bool ParentIsSelected
        {
            get { return this.parent_is_selected; }
            set
            {
                this.parent_is_selected = value;
                this.RegisterPropertyChanged("ParentIsSelected");
            }
        }


        #endregion

        #region PROPERTIES: IsMarkable, IsMarked, IsLocked, HideDetails

        protected bool is_markable;
        public bool IsMarkable
        {
            get { return this.is_markable; }
            internal set
            {
                this.is_markable = value;
                if (!this.is_markable)
                    this.is_marked = false;
                this.RegisterPropertyChanged("IsMarkable");
            }
        }

        protected bool is_marked;
        public bool IsMarked
        {
            get { return this.is_marked; }
            set
            {
                if (this.is_markable)
                {
                    this.is_marked = value;
                    this.RegisterPropertyChanged("IsMarked");
                }
            }
        }

        protected bool is_locked;
        public bool IsLocked
        {
            get { return this.is_locked; }
            set
            {
                this.is_locked = value;
                this.RegisterPropertyChanged("IsLocked");
            }
        }

        protected bool hide_details;
        public bool HideDetails
        {
            get { return this.hide_details; }
            set
            { 
                this.hide_details = value;
                this.IsLocked |= this.hide_details; // added 27.04.2017
                this.RegisterPropertyChanged("HideDetails");
            }
        }


        #endregion

        #region PROPERTIES: IsHidden (for filtering of lists)

        protected bool is_hidden;
        public bool IsHidden
        {
            get { return this.is_hidden; }
            internal set
            {
                this.is_hidden = value;
                this.RegisterPropertyChanged("IsHidden");
            }
        }

        #endregion

        #region PROPERTIES: IsExcludedFromDisplay (user-defined), IsHighlighted (components within networks)

        // can be applied to ANY product by ANY user regardless of access profile
        protected bool is_excluded_from_display;
        public bool IsExcludedFromDisplay
        {
            get { return this.is_excluded_from_display; }
            set 
            { 
                this.is_excluded_from_display = value;
                this.RegisterPropertyChanged("IsExcludedFromDisplay");
            }
        }

        public virtual bool ApplyIsExcludedFromDisplay { get; internal set; }

        protected bool is_bound_in_sel_nw;
        public bool IsBoundInSelNW
        {
            get { return this.is_bound_in_sel_nw; }
            set 
            { 
                this.is_bound_in_sel_nw = value;
                this.RegisterPropertyChanged("IsBoundInSelNW");
            }
        }


        #endregion

        #region PROPERTIES: IsAutomaticallyGenerated

        protected bool is_automatically_generated;
        public bool IsAutomaticallyGenerated
        {
            get { return this.is_automatically_generated; }
            protected set 
            { 
                this.is_automatically_generated = value;
                this.RegisterPropertyChanged("IsAutomaticallyGenerated");
            }
        }

        #endregion

        #region PROPERTIES(abstract): Contained Elements of SAME TYPE

        // builds the hierarchical structure
        internal abstract IReadOnlyList<DisplayableProductDefinition> ContainedOfSameType { get; }

        #endregion

        #region PROPERTIES: Commands

        public ICommand ToggleExcludedFromDisplay { get; protected set; }

        #endregion

        #region .CTOR

        internal DisplayableProductDefinition()
        {
            this.IsAutomaticallyGenerated = false;
            this.ToggleExcludedFromDisplay = new Utils.PS_RelayCommand((x) => this.IsExcludedFromDisplay = !this.IsExcludedFromDisplay);
        }

        #endregion

    }
}
