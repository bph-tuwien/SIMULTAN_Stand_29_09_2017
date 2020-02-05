using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;

namespace GeometryViewer.HelixToolkitCustomization
{
    public class GroupModel3Dext : GroupModel3D, IBoundable, INotifyPropertyChanged
    {

        // ------------------------------------------- INITIALIZERS ----------------------------------------------- //
        public GroupModel3Dext()
        {
            this.UndoManager = ActionManager.GetInstance();
        }

        // -------------------------------------- INOTIFYPROPERTYCHANGED ------------------------------------------ //

        public event PropertyChangedEventHandler PropertyChanged;

        public void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        // -------------------------------------------- IBOUNDABLE ------------------------------------------------ //
        public BoundingBox Bounds { get { return GetBounds(); } }

        protected virtual BoundingBox GetBounds()
        {
            BoundingBox bb = new BoundingBox();
            var children = this.Children;
            if (children != null && children.Count > 0)
            {
                foreach(var child in children)
                {
                    var model = child as IBoundable;
                    if (model != null && model.Visibility == Visibility.Visible)
                    {
                        bb = BoundingBox.Merge(bb, model.Bounds);
                    }
                }
            }
            return bb;
        }

        // ----------------------------------------------- UNDO --------------------------------------------------- //
        public ActionManager UndoManager { get; private set; }
        protected Func<int> undoAction;
        protected Func<int> redoAction;

        protected void TransmitActions()
        {
            ActionManager.RecordActionCallback(this, this.undoAction, this.redoAction);
            this.undoAction = null;
            this.redoAction = null;
        }
    }
}
