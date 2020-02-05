using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace GeometryViewer.Utils
{
    #region List With Child
    public class ListWithChild<T1, T2> : List<T1>
    {
        private List<T2> child;
        public ReadOnlyCollection<T2> Child
        {
            get { return this.child.AsReadOnly(); }
        }

        public event EventHandler OnAdd;
        public event EventHandler OnModify;

        public ListWithChild() : base()
        {
            this.child = new List<T2>();   
        }

        public void Add(T1 _item, T2 _childItem)
        {
            base.Add(_item);

            ListWithChildEventArgs<T1, T2> evargs = new ListWithChildEventArgs<T1, T2>(_item, _childItem, this.Count - 1);
            if (this.OnAdd != null)
                OnAdd.Invoke(this, evargs);

            T2 modifiedChildItem = evargs.ChildItem;
            this.child.Add(modifiedChildItem);
        }

        public void RemoveAt(int _indParent, int _indChild)
        {
            base.RemoveAt(_indParent);
            this.child.RemoveAt(_indChild);
        }

        public void ModifyAt(int _indParent, int _indChild)
        {
            ListWithChildEventArgs<T1, T2> evargs =
                new ListWithChildEventArgs<T1, T2>(this[_indParent], this.child[_indChild], _indParent);
            if (this.OnModify != null)
                OnModify.Invoke(this, evargs);

            this.child[_indChild] = evargs.ChildItem;
        }

        public T1 this[int _indParent, int _indChild]
        {
            get { return this[_indParent]; }
            set
            {
                this[_indParent] = value;
                this.ModifyAt(_indParent, _indChild);
            }
        }

    }
    #endregion

    #region List With Child Events
    public class ListWithChildEventArgs<T1, T2> : EventArgs
    {
        private readonly T1 parentItem;        
        private T2 childItem;
        private readonly int parentIndex;

        public ListWithChildEventArgs(T1 _parentItem, T2 _childItem, int _parentIndex)
        {
            this.parentItem = _parentItem;
            this.childItem = _childItem;
            this.parentIndex = _parentIndex;
        }
        public T1 ParentItem
        {
            get { return this.parentItem; }
        }
        public T2 ChildItem
        {
            get { return this.childItem; }
            set { this.childItem = value; }
        }
        public int ParentIndex
        {
            get { return this.parentIndex; }
        }
    }

    public class Tester
    {
        private ListWithChild<Point3D, float> points;

        public Tester()
        {
            this.points = new ListWithChild<Point3D, float>();
            this.points.OnAdd += points_OnAdd;
            this.points.OnModify += points_OnModify;
        }

        public void PerformTests()
        {
            this.points.Add(new Point3D(4, 5, 6), 0f);
            this.points.Add(new Point3D(7, 5, 6), 0f);
            this.points.Add(new Point3D(19, 5, 6), 0f);

            this.points[0] = new Point3D(41, 5, 6);
            this.points[0, 0] = new Point3D(41, 5, 6);

            this.points.RemoveAt(1, 1);
        }

        void points_OnModify(object sender, EventArgs e)
        {
            ListWithChild<Point3D, float> list = sender as ListWithChild<Point3D, float>;
            ListWithChildEventArgs<Point3D, float> evargs = e as ListWithChildEventArgs<Point3D, float>;
            if (list == null || evargs == null)
                return;

            evargs.ChildItem = (float)evargs.ParentItem.X;
        }

        void points_OnAdd(object sender, EventArgs e)
        {
            ListWithChild<Point3D, float> list = sender as ListWithChild<Point3D, float>;
            ListWithChildEventArgs<Point3D, float> evargs = e as ListWithChildEventArgs<Point3D, float>;
            if (list == null || evargs == null)
                return;

            evargs.ChildItem = (float) evargs.ParentItem.X;
        }
    }
    #endregion

    #region Composite Key

    public class Composite4IntKey
    {
        public const string KEY_DELIMITER = "|";
        public int Key_1 { get; private set; }
        public int Key_2 { get; private set; }
        public int Key_3 { get; private set; }
        public int Key_4 { get; private set; }

        public Composite4IntKey(int _k1, int _k2, int _k3, int _k4)
        {
            this.Key_1 = _k1;
            this.Key_2 = _k2;
            this.Key_3 = _k3;
            this.Key_4 = _k4;
        }

        public override string ToString()
        {
            return this.Key_1 + KEY_DELIMITER + this.Key_2 + KEY_DELIMITER + this.Key_3 + KEY_DELIMITER + this.Key_4;
        }

        public static Composite4IntKey GetInvalidKey()
        {
            return new Composite4IntKey(-1, -1, -1, -1);
        }

        public static Composite4IntKey ParseKey(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return null;

            string[] key_comps = _input.Split(new string[] { Composite4IntKey.KEY_DELIMITER }, StringSplitOptions.RemoveEmptyEntries);
            if (key_comps == null || key_comps.Length < 4)
                return null;

            int[] ks = new int[4];
            for(int i = 0; i < 4; i++)
            {
                int k = -1;
                bool success = int.TryParse(key_comps[i], out k);
                if (!success)
                    return null;
                else
                    ks[i] = k;
            }

            return new Composite4IntKey(ks[0], ks[1], ks[2], ks[3]);
        }

        private static int ShiftAndWrap(int _value, int _positions)
        {
            _positions = _positions & 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(_value), 0);
            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - _positions);
            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << _positions) | wrapped), 0);
        }

        public override int GetHashCode()
        {
            return Composite4IntKey.ShiftAndWrap(this.Key_1.GetHashCode(), 8) ^
                   Composite4IntKey.ShiftAndWrap(this.Key_2.GetHashCode(), 4) ^
                   Composite4IntKey.ShiftAndWrap(this.Key_3.GetHashCode(), 2) ^
                   this.Key_4.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Composite4IntKey)) return false;

            Composite4IntKey ck = obj as Composite4IntKey;
            return (this.Key_1 == ck.Key_1 && this.Key_2 == ck.Key_2 &&
                    this.Key_3 == ck.Key_3 && this.Key_4 == ck.Key_4);
        }

        public static bool operator==(Composite4IntKey _ck1, Composite4IntKey _ck2)
        {
            if (Object.ReferenceEquals(_ck1, null) && !Object.ReferenceEquals(_ck2, null)) return false;
            if (!Object.ReferenceEquals(_ck1, null) && Object.ReferenceEquals(_ck2, null)) return false;
            if (Object.ReferenceEquals(_ck1, null) && Object.ReferenceEquals(_ck2, null)) return true;

            if (_ck1.GetHashCode() != _ck2.GetHashCode()) return false;

            return (_ck1.Equals(_ck2));
        }

        public static bool operator!=(Composite4IntKey _ck1, Composite4IntKey _ck2)
        {
            return !(_ck1 == _ck2);
        }
    }

    #endregion

}
