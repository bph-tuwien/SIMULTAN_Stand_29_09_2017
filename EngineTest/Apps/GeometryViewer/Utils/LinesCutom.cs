using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.Utils
{
    public static class LinesCutom
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================= CURSTOM LINE DEFINITIONS ===================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region OBJECT SNAP MARKERS

        public static LineGeometry3D GetEndPointMarker(Vector3 _point, float _size)
        {
            Vector3 topL = _point - Vector3.UnitX * _size * 0.5f + Vector3.UnitZ * _size * 0.5f;
            Vector3 topR = _point + Vector3.UnitX * _size * 0.5f + Vector3.UnitZ * _size * 0.5f;
            Vector3 bottomL = _point - Vector3.UnitX * _size * 0.5f - Vector3.UnitZ * _size * 0.5f;
            Vector3 bottomR = _point + Vector3.UnitX * _size * 0.5f - Vector3.UnitZ * _size * 0.5f;

            LineBuilder b = new LineBuilder();
            b.AddLine(bottomL, bottomR);
            b.AddLine(bottomR, topR);
            b.AddLine(topR, topL);
            b.AddLine(topL, bottomL);

            return b.ToLineGeometry3D();
        }

        public static LineGeometry3D GetIntersectionMarker(Vector3 _point, float _size)
        {
            Vector3 topL = _point - Vector3.UnitX * _size * 0.5f + Vector3.UnitZ * _size * 0.5f;
            Vector3 topR = _point + Vector3.UnitX * _size * 0.5f + Vector3.UnitZ * _size * 0.5f;
            Vector3 bottomL = _point - Vector3.UnitX * _size * 0.5f - Vector3.UnitZ * _size * 0.5f;
            Vector3 bottomR = _point + Vector3.UnitX * _size * 0.5f - Vector3.UnitZ * _size * 0.5f;

            LineBuilder b = new LineBuilder();
            b.AddLine(bottomL, topR);
            b.AddLine(bottomR, topL);

            return b.ToLineGeometry3D();
        }

        public static LineGeometry3D GetMidPointMarker(Vector3 _point, float _size)
        {
            Vector3 top = _point + Vector3.UnitZ * _size * 0.5f;
            Vector3 bottomL = _point - Vector3.UnitX * _size * 0.5f * (float)Math.Cos(Math.PI / 6)
                                     - Vector3.UnitZ * _size * 0.5f * (float)Math.Sin(Math.PI / 6);
            Vector3 bottomR = _point + Vector3.UnitX * _size * 0.5f * (float)Math.Cos(Math.PI / 6)
                                     - Vector3.UnitZ * _size * 0.5f * (float)Math.Sin(Math.PI / 6);

            LineBuilder b = new LineBuilder();
            b.AddLine(bottomL, bottomR);
            b.AddLine(bottomR, top);
            b.AddLine(top, bottomL);

            return b.ToLineGeometry3D();
        }

        #endregion
    }
}
