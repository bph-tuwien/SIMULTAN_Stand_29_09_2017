using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.HelixToolkitCustomization;

namespace GeometryViewer.TestAlgs
{
    public class PolygonDisplay : GroupModel3Dext
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ STATIC DATA =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC DATA FOR TESTING

        private const int NR_SLOTS_FOR_SUBPOLYS = 20;

        #region MONOTONE

        // MONOTONE POLYGON
        private static List<Point3D> PolyBig = new List<Point3D>()
        {
            new Point3D( 0, 0, 0), // [ 1]: 1, both
            new Point3D( 2, 0, 3), // [ 2]: 2, top
            new Point3D( 8, 0, 5), // [ 3]: 7, top
            new Point3D( 9, 0,-2), // [ 4]: 8, top
            new Point3D(12, 0,-1), // [ 5]: 9, top
            new Point3D(14, 0, 1), // [ 6]:10, top
            new Point3D(16, 0, 7), // [ 7]:11, top
            new Point3D(17, 0, 7), // [ 8]:12, top
            new Point3D(18, 0,-3), // [ 9]:13, both
            new Point3D( 6, 0,-3), // [10]: 6, bot
            new Point3D( 5, 0, 0), // [11]: 5, bot
            new Point3D( 4, 0, 1), // [12]: 4, bot
            new Point3D( 3, 0, 1), // [13]: 3, bot
        };
        // ordered:
        // 1-2-13-12-11-10-3-4-5-6-7-8-9 (one-based)
        // 0-1-12-11-10-9-2-3-4-5-6-7-8 (zero-based)
        // upper chain: [1]-2-3-4-5-6-7-8-[9]
        // lower chain: [9]-10-11-12-13-[1]

        #endregion

        #region NON-MONOTONE
        // NON-MONOTONE polygon
        private static List<Point3D> PolyBig_1 = new List<Point3D>()
        {
            new Point3D( 2, 0, 3),
            new Point3D( 4.5, 0, 2.1),
            new Point3D( 6, 0, 3),
            new Point3D( 7, 0, 2),
            new Point3D( 6.5, 0, 0.5),
            new Point3D( 4.5, 0, 1.9),
            new Point3D( 4, 0, 1),
            new Point3D( 5, 0, 0),
            new Point3D( 6, 0,-3),
            new Point3D( 9, 0,-2),
            new Point3D( 8, 0, 5),
        };
        #endregion

        #region Rectangle w extra points
        // rectangle with extra points
        private static List<Point3D> PolyBig_2 = new List<Point3D>()
        {
            new Point3D( 0, 2, 1),
            new Point3D( 0, 2, 0),
            new Point3D( 4, 2, 0),
            new Point3D( 4, 2, 3),
            new Point3D( 0, 2, 3),
        };
        #endregion

        #region Decomposition in Monotone polygons: MY EXAMPLE
        // polygon for testing decomposition in monotone polygons
        private static List<Point3D> PolyBig_3 = new List<Point3D>()
        {
            new Point3D( 0, 0, 5),
            new Point3D( 2, 0, 3),
            new Point3D( 5, 0, 4),
            new Point3D(10, 0, 2),
            new Point3D(13, 0, 3),
            new Point3D(14, 0, 5),
            new Point3D(13, 0, 6),
            new Point3D(11, 0, 7),
            new Point3D(11, 0, 9),
            new Point3D(13, 0,11),
            new Point3D(15, 0,10),
            new Point3D(16, 0, 9),
            new Point3D(16, 0, 7),
            new Point3D(18, 0, 7),
            new Point3D(19, 0,10),
            new Point3D(18, 0,14),
            new Point3D(16, 0,18),
            new Point3D(15, 0,18),
            new Point3D(14, 0,17),
            new Point3D(12, 0,16),
            new Point3D(11, 0,13),
            new Point3D(10, 0,15),
            new Point3D( 7, 0,17),
            new Point3D( 5, 0,17),
            new Point3D( 3, 0,16),
            new Point3D( 2, 0,14),
            new Point3D( 3, 0,12),
            new Point3D( 4, 0,11),
            new Point3D( 6, 0,12),
            new Point3D( 7, 0,10),
            new Point3D( 6, 0, 7),
            new Point3D( 3, 0, 6),
            new Point3D( 1, 0, 7),
        };

        private static List<Point3D> PolyBig_3_HOLE_1 = new List<Point3D>()
        {
            new Point3D( 5, 0,13),
            new Point3D( 7, 0,13),
            new Point3D( 8, 0,14),
            new Point3D( 6, 0,15),
            new Point3D( 7, 0,16),
            new Point3D( 5, 0,16),
            new Point3D( 4, 0,15),
            new Point3D( 4, 0,14),
        };

        private static List<Point3D> PolyBig_3_HOLE_2 = new List<Point3D>()
        {
            new Point3D( 4, 0, 6),
            new Point3D( 5, 0, 5),
            new Point3D( 8, 0, 4),
            new Point3D(10, 0, 5),
            new Point3D(11, 0, 6),
            new Point3D(10, 0, 7),
            new Point3D(10, 0,10),
            new Point3D(13, 0,12),
            new Point3D(15, 0,12),
            new Point3D(16, 0,11),
            new Point3D(17, 0,12),
            new Point3D(15, 0,13),
            new Point3D(14, 0,14),
            new Point3D(16, 0,14),
            new Point3D(15, 0,15),
            new Point3D(13, 0,14),
            new Point3D(11, 0,12),
            new Point3D( 9, 0,11),
            new Point3D( 8, 0,10),
            new Point3D( 9, 0, 9),
            new Point3D( 8, 0, 7),
            new Point3D( 6, 0, 6),
        };
        #endregion

        #region Decomposition in Monotone polygons: EXAMPLE FROM LECTURE
        // polygon for testing decomposition in monotone polygons
        private static List<Point3D> PolyBig_4 = new List<Point3D>()
        {
            new Point3D( 0, 0, 8),
            new Point3D( 3, 0, 4),
            new Point3D( 7, 0, 2),
            new Point3D(10, 0, 4),
            new Point3D(11, 0, 6),
            new Point3D( 7, 0, 7),
            new Point3D(11, 0, 8),
            new Point3D(15, 0, 7),
            new Point3D(16, 0, 3),
            new Point3D(19, 0, 1),
            new Point3D(22, 0, 4),
            new Point3D(18, 0, 6),
            new Point3D(26, 0, 7),
            new Point3D(24, 0, 4),
            new Point3D(27, 0, 3),
            new Point3D(29, 0, 0),
            new Point3D(28, 0, 5),
            new Point3D(30, 0, 9),
            new Point3D(22, 0, 8),
            new Point3D(24, 0,13),
            new Point3D(26, 0,12),
            new Point3D(27, 0,15),
            new Point3D(23, 0,15),
            new Point3D(19, 0,14),
            new Point3D(20, 0, 9),
            new Point3D(17, 0, 8),
            new Point3D(13, 0, 9),
            new Point3D(16, 0,11),
            new Point3D(12, 0,13),
            new Point3D(15, 0,14),
            new Point3D(17, 0,16),
            new Point3D(15, 0,18),
            new Point3D( 9, 0,16),
            new Point3D(10, 0,13),
            new Point3D( 8, 0,10),
            new Point3D( 5, 0,11),
            new Point3D( 7, 0,14),
            new Point3D( 8, 0,17),
            new Point3D( 2, 0,15),
        };
        #endregion

        #region Decomposition in Monotone polygons: ARCHITECTURAL EXAMPLE
        // polygon for testing decomposition in monotone polygons of a typical architectural polygon
        private static List<Point3D> PolyBig_ARC1 = new List<Point3D>()
        {
            new Point3D( 0, 0, 0),
            new Point3D( 5, 0, 0),
            new Point3D( 5, 0, 1),
            new Point3D( 6, 0, 1),
            new Point3D( 6, 0, 0),
            new Point3D( 7, 0, 0),
            new Point3D( 7, 0, 1),
            new Point3D(11, 0, 1),
            new Point3D(11, 0, 0),
            new Point3D(12, 0, 0),
            new Point3D(12, 0, 3),
            new Point3D( 9, 0, 3),
            new Point3D( 9, 0, 5),
            new Point3D( 8, 0, 5),
            new Point3D( 8, 0, 2),
            new Point3D( 2, 0, 2),
            new Point3D( 2, 0, 4),
            new Point3D( 7, 0, 4),
            new Point3D( 7, 0, 5),
            new Point3D( 5, 0, 5),
            new Point3D( 5, 0, 9),
            new Point3D( 6, 0, 9),
            new Point3D( 6, 0, 7),
            new Point3D( 9, 0, 7),
            new Point3D( 9, 0, 9),
            new Point3D( 8, 0, 9),
            new Point3D( 8, 0, 8),
            new Point3D( 7, 0, 8),
            new Point3D( 7, 0,11),
            new Point3D( 8, 0,11),
            new Point3D( 8, 0,10),
            new Point3D( 9, 0,10),
            new Point3D( 9, 0,12),
            new Point3D( 3, 0,12),
            new Point3D( 3, 0, 6),
            new Point3D( 0, 0, 6),
            new Point3D( 0, 0, 4),
            new Point3D( 0, 0, 4),
        };
        #endregion

        #region Connecting Contained Holes w Polygon: ARCHITECTURAL EXAMPLE
        // polygon for testing decomposition in monotone polygons of a typical architectural polygon
        private static List<Point3D> PolyBig_ARC2 = new List<Point3D>()
        {
            new Point3D( 0, 0, 0),
            new Point3D( 7, 0, 0),
            new Point3D( 7, 0, 6),
            new Point3D( 4, 0, 6),
            new Point3D( 4, 0, 9),
            new Point3D( 0, 0, 9),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_1 = new List<Point3D>()
        {
            new Point3D( 1, 0, 7),
            new Point3D( 2, 0, 7),
            new Point3D( 2, 0, 8),
            new Point3D( 1, 0, 8),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_1_REV = new List<Point3D>()
        {
            new Point3D( 1, 0, 8),
            new Point3D( 2, 0, 8),
            new Point3D( 2, 0, 7),
            new Point3D( 1, 0, 7),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_2 = new List<Point3D>()
        {
            new Point3D( 1, 0, 5),
            new Point3D( 2, 0, 5),
            new Point3D( 2, 0, 6),
            new Point3D( 1, 0, 6),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_3 = new List<Point3D>()
        {
            new Point3D( 1, 0, 3),
            new Point3D( 2, 0, 3),
            new Point3D( 2, 0, 4),
            new Point3D( 1, 0, 4),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_4 = new List<Point3D>()
        {
            new Point3D( 1, 0, 1),
            new Point3D( 2, 0, 1),
            new Point3D( 2, 0, 2),
            new Point3D( 1, 0, 2),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_5 = new List<Point3D>()
        {
            new Point3D( 3, 0, 2),
            new Point3D( 4, 0, 2),
            new Point3D( 4, 0, 5),
            new Point3D( 3, 0, 5),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_6 = new List<Point3D>()
        {
            new Point3D( 5, 0, 3),
            new Point3D( 6, 0, 3),
            new Point3D( 6, 0, 4),
            new Point3D( 5, 0, 4),
        };

        private static List<Point3D> PolyBig_ARC2_HOLE_7 = new List<Point3D>()
        {
            new Point3D( 5, 0, 1),
            new Point3D( 6, 0, 1),
            new Point3D( 6, 0, 2),
            new Point3D( 5, 0, 2),
        };
        #endregion

        #region Connecting Contained Holes w Polygon + Decomposition: EXAMPLE PAPER
        private static List<Point3D> PolyBig_Paper = new List<Point3D>()
        {
            new Point3D( 0.0, 0, 1.0), //  0
            new Point3D( 3.0, 0, 1.5), //  1
            new Point3D( 2.0, 0, 0.5), //  2
            new Point3D( 4.0, 0, 0.0), //  3
            new Point3D( 8.0, 0, 3.0), //  4
            new Point3D(10.5, 0, 3.0), //  5
            new Point3D(14.0, 0, 1.0), //  6
            new Point3D(13.0, 0, 3.0), //  7
            new Point3D(16.0, 0, 1.0), //  8
            new Point3D(17.0, 0, 3.0), //  9
            new Point3D(16.5, 0, 1.0), // 10
            new Point3D(17.0, 0, 0.5), // 11
            new Point3D(20.0, 0, 2.0), // 12
            new Point3D(19.0, 0, 1.0), // 13
            new Point3D(19.5, 0, 0.5), // 14
            new Point3D(22.0, 0, 3.0), // 15
            new Point3D(21.0, 0, 1.5), // 16
            new Point3D(21.5, 0, 0.5), // 17
            new Point3D(24.0, 0, 4.0), // 18
            new Point3D(36.0, 0, 7.0), // 19
            new Point3D(33.0, 0, 7.5), // 20
            new Point3D(34.0, 0, 9.0), // 21
            new Point3D(32.0, 0, 8.5), // 22
            new Point3D(33.0, 0,11.0), // 23
            new Point3D(33.5, 0,14.0), // 24
            new Point3D(31.5, 0,14.5), // 25
            new Point3D(33.0, 0,16.0), // 26
            new Point3D(30.0, 0,18.0), // 27
            new Point3D(29.0, 0,17.5), // 28
            new Point3D(28.5, 0,18.5), // 29
            new Point3D(27.5, 0,20.0), // 30
            new Point3D(27.0, 0,19.5), // 31
            new Point3D(27.5, 0,17.0), // 32
            new Point3D(26.0, 0,20.0), // 33
            new Point3D(25.0, 0,20.5), // 34
            new Point3D(25.0, 0,19.5), // 35
            new Point3D(25.5, 0,17.5), // 36
            new Point3D(22.5, 0,21.5), // 37
            new Point3D(22.5, 0,20.0), // 38
            new Point3D(23.0, 0,18.0), // 39
            new Point3D(20.0, 0,22.0), // 40
            new Point3D(20.0, 0,21.0), // 41
            new Point3D(20.5, 0,19.0), // 42
            new Point3D(18.5, 0,22.0), // 43
            new Point3D(15.0, 0,19.5), // 44
            new Point3D(12.0, 0,18.5), // 45
            new Point3D(13.5, 0,17.5), // 46
            new Point3D(10.0, 0,17.0), // 47
            new Point3D(10.5, 0,16.0), // 48
            new Point3D(11.5, 0,15.5), // 49
            new Point3D(10.5, 0,15.0), // 50
            new Point3D(10.5, 0,14.0), // 51
            new Point3D(14.0, 0,14.0), // 52
            new Point3D(11.0, 0,13.0), // 53
            new Point3D( 5.0, 0,17.0), // 54
            new Point3D( 6.0, 0,15.0), // 55
            new Point3D( 4.0, 0,15.5), // 56
            new Point3D( 4.0, 0,14.5), // 57
            new Point3D( 5.0, 0,13.5), // 58
            new Point3D( 2.0, 0,13.0), // 59
            new Point3D( 2.0, 0,12.0), // 60
            new Point3D( 3.5, 0,11.5), // 61
            new Point3D( 2.0, 0,10.0), // 62
            new Point3D(12.0, 0, 7.5), // 63
            new Point3D(12.0, 0, 8.5), // 64
            new Point3D(11.0, 0, 9.0), // 65
            new Point3D(12.0, 0, 9.5), // 66
            new Point3D( 8.0, 0,11.0), // 67
            new Point3D(10.0, 0,12.0), // 68
            new Point3D(14.0, 0,10.0), // 69
            new Point3D(13.0, 0,12.0), // 70
            new Point3D(18.0, 0,11.0), // 71
            new Point3D(20.0, 0,13.0), // 72
            new Point3D(18.5, 0,14.5), // 73
            new Point3D(20.5, 0,15.0), // 74
            new Point3D(22.0, 0,14.0), // 75
            new Point3D(19.0, 0, 9.0), // 76
            new Point3D(16.0, 0, 9.0), // 77
            new Point3D(18.0, 0, 7.5), // 78
            new Point3D(16.0, 0, 6.0), // 79
            new Point3D( 7.0, 0, 7.0), // 80
            new Point3D( 9.0, 0, 6.0), // 81
            new Point3D( 6.0, 0, 6.0), // 82
            new Point3D( 7.0, 0, 5.5), // 83
            new Point3D( 4.0, 0, 5.0), // 84
            new Point3D( 4.0, 0, 4.0), // 85
            new Point3D( 6.0, 0, 4.0), // 86
        };

        private static List<Point3D> PolyBig_Paper_HOLE_1 = new List<Point3D>()
        {
            new Point3D(10.0, 0, 4.0),
            new Point3D(15.0, 0, 4.0),
            new Point3D(11.0, 0, 4.5),
            new Point3D(11.5, 0, 5.0),
            new Point3D(12.5, 0, 5.5),
            new Point3D(11.0, 0, 6.0),
        };

        private static List<Point3D> PolyBig_Paper_HOLE_2 = new List<Point3D>()
        {
            new Point3D(23.5, 0, 5.5),
            new Point3D(25.0, 0, 6.0),
            new Point3D(26.0, 0, 8.0),
            new Point3D(23.0, 0,10.0),
            new Point3D(22.5, 0, 8.5),
            new Point3D(21.0, 0, 8.0),
            new Point3D(23.0, 0, 7.5),
            new Point3D(22.0, 0, 6.0),
            new Point3D(23.5, 0, 7.0),
            new Point3D(24.0, 0, 6.5),
        };

        private static List<Point3D> PolyBig_Paper_HOLE_3 = new List<Point3D>()
        {
            new Point3D(25.0, 0,11.0),
            new Point3D(27.5, 0,10.5),
            new Point3D(31.0, 0,13.0),
            new Point3D(29.5, 0,14.5),
            new Point3D(30.0, 0,13.0),
            new Point3D(27.0, 0,12.0),
            new Point3D(27.5, 0,14.0),
            new Point3D(26.5, 0,13.0),
            new Point3D(26.5, 0,15.0),
            new Point3D(25.5, 0,15.5),
            new Point3D(26.0, 0,13.0),
            new Point3D(25.5, 0,12.5),
            new Point3D(25.0, 0,14.0),
            new Point3D(25.0, 0,15.0),
            new Point3D(24.5, 0,15.0),
            new Point3D(24.0, 0,16.5),
            new Point3D(23.0, 0,17.0),
            new Point3D(23.5, 0,14.5),
        };

        #endregion

        #region ARC: Split-Level

        private static List<Point3D> EG_POLY_1 = new List<Point3D>()
        {
            new Point3D(1, 0, 2),
            new Point3D(9, 0, 2),
            new Point3D(9, 0, 5),
            new Point3D(8, 0, 5),
            new Point3D(8, 0, 8),
            new Point3D(9, 0, 8),
            new Point3D(9, 0,10),
            new Point3D(3, 0,10),
        };

        private static List<Point3D> EG_POLY_1_HOLE_1 = new List<Point3D>()
        {
            new Point3D(5, 0, 8),
            new Point3D(7, 0, 8),
            new Point3D(7, 0, 9),
            new Point3D(5, 0, 9),
        };

        private static List<Point3D> EG_POLY_1_HOLE_2 = new List<Point3D>()
        {
            new Point3D(4, 0, 6),
            new Point3D(6, 0, 6),
            new Point3D(6, 0, 7),
            new Point3D(4, 0, 7),
        };

        private static List<Point3D> EG_POLY_1_HOLE_3 = new List<Point3D>()
        {
            new Point3D(5, 0, 4),
            new Point3D(7, 0, 4),
            new Point3D(7, 0, 5),
            new Point3D(5, 0, 5),
        };

        // ---------------

        private static List<Point3D> OG2_POLY_1 = new List<Point3D>()
        {
            new Point3D( 0, 7, 0),
            new Point3D( 9, 7, 0),
            new Point3D(16, 7, 0),
            new Point3D(19, 7,12),
            new Point3D( 9, 7,12),
            new Point3D( 0, 7,12),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_1 = new List<Point3D>()
        {
            new Point3D(5, 7, 8),
            new Point3D(7, 7, 8),
            new Point3D(7, 7, 9),
            new Point3D(5, 7, 9),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_2 = new List<Point3D>()
        {
            new Point3D(4, 7, 6),
            new Point3D(6, 7, 6),
            new Point3D(6, 7, 7),
            new Point3D(4, 7, 7),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_3 = new List<Point3D>()
        {
            new Point3D(5, 7, 4),
            new Point3D(7, 7, 4),
            new Point3D(7, 7, 5),
            new Point3D(5, 7, 5),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_4 = new List<Point3D>()
        {
            new Point3D(13, 7, 7),
            new Point3D(14, 7, 7),
            new Point3D(14, 7, 9),
            new Point3D(13, 7, 9),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_5 = new List<Point3D>()
        {
            new Point3D(12, 7, 3),
            new Point3D(13, 7, 3),
            new Point3D(13, 7, 4),
            new Point3D(12, 7, 4),
        };

        private static List<Point3D> OG2_POLY_1_HOLE_6 = new List<Point3D>()
        {
            new Point3D( 8, 7, 5),
            new Point3D( 9, 7, 5),
            new Point3D(11, 7, 5),
            new Point3D(11, 7, 8),
            new Point3D( 9, 7, 8),
            new Point3D( 8, 7, 8),
        };

        #endregion

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== INITIALIZATION ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region STATIC

        private static PhongMaterial VolumeMat;
        private static List<SharpDX.Color> SubpolyColors;
        
        static PolygonDisplay()
        {
            VolumeMat = new PhongMaterial();
            VolumeMat.DiffuseColor = new Color4(1f, 1f, 1f, 0.8f);
            VolumeMat.AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            VolumeMat.EmissiveColor = new Color4(0.25f, 0.25f, 0.25f, 0f);
            VolumeMat.SpecularColor = new Color4(1f, 1f, 1f, 1f);
            VolumeMat.SpecularShininess = 3;

            SubpolyColors = new List<Color>();
            SubpolyColors.Add(Color.Red);
            SubpolyColors.Add(Color.Blue);
            SubpolyColors.Add(Color.Orange);
            SubpolyColors.Add(Color.Violet);
            SubpolyColors.Add(Color.Yellow);
            SubpolyColors.Add(Color.Green);    
        }

        #endregion

        #region INSTANCE

        private List<Point3D> polygonDef;
        private List<List<Point3D>> holesDef;
        private LineGeometryModel3D polygonEdges;
        private List<LineGeometryModel3D> subpolygonEdges;
        private MeshGeometryModel3D polygonFill;
        private LineGeometryModel3D polygonFillEdges;

        public PolygonDisplay()
        {
            //// CASE: Extra points
            //this.polygonDef = PolygonDisplay.PolyBig_2;
            //this.holesDef = null;

            //// CASE: PPT Lecture
            //this.polygonDef = PolygonDisplay.PolyBig_4;
            //this.holesDef = null;

            //// CASE: My Test
            //this.polygonDef = PolygonDisplay.PolyBig_3;
            //this.holesDef = new List<List<Point3D>>();
            //this.holesDef.Add(PolygonDisplay.PolyBig_3_HOLE_1);
            //this.holesDef.Add(PolygonDisplay.PolyBig_3_HOLE_2);

            //// CASE: Chaos
            //this.polygonDef = PolygonDisplay.PolyBig_Paper;
            //this.holesDef = new List<List<Point3D>>();
            //this.holesDef.Add(PolygonDisplay.PolyBig_Paper_HOLE_1);
            //this.holesDef.Add(PolygonDisplay.PolyBig_Paper_HOLE_2);
            //this.holesDef.Add(PolygonDisplay.PolyBig_Paper_HOLE_3);

            //// CASE: ARCHITECTURE w/o holes 
            //this.polygonDef = PolygonDisplay.PolyBig_ARC1;
            //this.holesDef = null; 

            // CASE: ARCHITECTURE 1 w HOLEs
            this.polygonDef = PolygonDisplay.PolyBig_ARC2;
            this.holesDef = new List<List<Point3D>>();
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_1);
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_2);
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_3);
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_4);
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_5);
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_6);
            this.holesDef.Add(PolygonDisplay.PolyBig_ARC2_HOLE_7);

            //// CASE: ARCHITECTURE 2
            //this.polygonDef = PolygonDisplay.EG_POLY_1;
            //this.holesDef = new List<List<Point3D>>();
            //this.holesDef.Add(PolygonDisplay.EG_POLY_1_HOLE_1);
            //this.holesDef.Add(PolygonDisplay.EG_POLY_1_HOLE_2);
            //this.holesDef.Add(PolygonDisplay.EG_POLY_1_HOLE_3);

            //// CASE: ARCHITECTURE 3
            //this.polygonDef = PolygonDisplay.OG2_POLY_1;
            //this.holesDef = new List<List<Point3D>>();
            //this.holesDef.Add(PolygonDisplay.OG2_POLY_1_HOLE_1);
            //this.holesDef.Add(PolygonDisplay.OG2_POLY_1_HOLE_2);
            //this.holesDef.Add(PolygonDisplay.OG2_POLY_1_HOLE_3);
            //this.holesDef.Add(PolygonDisplay.OG2_POLY_1_HOLE_4);
            //this.holesDef.Add(PolygonDisplay.OG2_POLY_1_HOLE_5);
            //this.holesDef.Add(PolygonDisplay.OG2_POLY_1_HOLE_6);

            CreateSubpolygonGeometry();

            this.polygonEdges = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color.Indigo,
                Thickness = 1,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.polygonEdges);

            this.polygonFill = new MeshGeometryModel3D()
            {
                Geometry = null,
                Material = PolygonDisplay.VolumeMat,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.polygonFill);

            this.polygonFillEdges = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color.Indigo,
                Thickness = 0.3,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(this.polygonFillEdges);

            this.UpdateGeometry();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================================== GEOMETRY DEFINITIONS ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GEOMETRY DEFINITIONS

        private void CreateSubpolygonGeometry()
        {
            int nC = PolygonDisplay.SubpolyColors.Count;
            this.subpolygonEdges = new List<LineGeometryModel3D>();

            for (int i = 0; i < NR_SLOTS_FOR_SUBPOLYS; i++)
            {
                this.subpolygonEdges.Add(new LineGeometryModel3D()
                {
                    Geometry = null,
                    Color = PolygonDisplay.SubpolyColors[i % nC],
                    Thickness = 0.5,
                    Visibility = Visibility.Visible,
                    IsHitTestVisible = false,
                    Transform = new MatrixTransform3D(Matrix3D.Identity),
                    Instances = new List<SharpDX.Matrix>(),
                });
                this.Children.Add(this.subpolygonEdges[i]);
            }
        }

        private void UpdateGeometry()
        {
            // show the polygon edges
            LineBuilder b = new LineBuilder();
            int n = this.polygonDef.Count;
            if (n > 0)
            {
                for (int i = 0; i < n; i++)
                {
                    b.AddLine(this.polygonDef[i].ToVector3(), this.polygonDef[(i + 1) % n].ToVector3());
                }
            }

            // and the hole edges...
            if (this.holesDef != null)
            {
                foreach (List<Point3D> hole in this.holesDef)
                {
                    int h = hole.Count;
                    for (int j = 0; j < h; j++)
                    {
                        b.AddLine(hole[j].ToVector3(), hole[(j + 1) % h].ToVector3());
                    }
                }
            }

            this.polygonEdges.Geometry = b.ToLineGeometry3D();

            // 1. decompose the polygon with holes into simple polygons
            List<List<Point3D>> simplePolys = Utils.MeshesCustom.DecomposeInSimplePolygons(this.polygonDef, this.holesDef);
            List<List<Point3D>> monPolys = new List<List<Point3D>>();

            if (simplePolys.Count > 0)
            {
                
                int counter = 0;
                foreach (List<Point3D> spoly in simplePolys)
                {
                    // draw it
                    LineBuilder bmp = new LineBuilder();
                    counter++;
                    int m = spoly.Count;
                    for (int i = 0; i < m; i++)
                    {
                        Vector3 start = new Vector3((float)spoly[i].X, (float)spoly[i].Y - counter, (float)spoly[i].Z);
                        Vector3 end = new Vector3((float)spoly[(i + 1) % m].X, (float)spoly[(i + 1) % m].Y - counter, (float)spoly[(i + 1) % m].Z);
                        bmp.AddLine(start, end);
                        
                    }
                    // 2. decompose it in monotone polygons
                    List<List<Point3D>> mPolys = Utils.MeshesCustom.DecomposeInMonotonePolygons(spoly);
                    monPolys.AddRange(mPolys);

                    int counterM = 0;
                    foreach(List<Point3D> mpoly in mPolys)
                    {
                        // and draw them
                        counterM++;
                        int k = mpoly.Count;
                        for(int j = 0; j < k; j++)
                        {
                            Vector3 start = new Vector3((float)mpoly[j].X, (float)mpoly[j].Y - counter * 1.2f - counterM * 0.05f, (float)mpoly[j].Z);
                            Vector3 end = new Vector3((float)mpoly[(j + 1) % k].X, (float)mpoly[(j + 1) % k].Y - counter * 1.2f - counterM * 0.05f, (float)mpoly[(j + 1) % k].Z);
                            bmp.AddLine(start, end);
                        }
                    }
                    
                    if (counter > 0 && this.subpolygonEdges.Count > counter - 1)
                        this.subpolygonEdges[counter - 1].Geometry = bmp.ToLineGeometry3D();
                }
                
            }
            

            // 3. triangulate each of the monotone polygons
            List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D> mFills = new List<HelixToolkit.SharpDX.Wpf.MeshGeometry3D>();
            foreach(List<Point3D> mpoly in monPolys)
            {
                List<Vector3> mpoly_V3 = Utils.CommonExtensions.ConvertPoints3DListToVector3List(mpoly);
                HelixToolkit.SharpDX.Wpf.MeshGeometry3D fill = Utils.MeshesCustom.PolygonFillMonotone(mpoly_V3);
                mFills.Add(fill);
            }

            HelixToolkit.SharpDX.Wpf.MeshGeometry3D fillAll = Utils.MeshesCustom.CombineMeshes(mFills);
            this.polygonFill.Geometry = fillAll;
            this.polygonFillEdges.Geometry = Utils.MeshesCustom.GetEdgesAsLines(fillAll);


            
        }

        #endregion

    }
}
