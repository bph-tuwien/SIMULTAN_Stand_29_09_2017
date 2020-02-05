using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.Utils
{
    #region MESH SORTING
    public struct MeshGeometry3DWMeasure
    {
        public MeshGeometry3D mesh;
        public float measure;

        public static int CompareMeshesByMeasure(MeshGeometry3DWMeasure x, MeshGeometry3DWMeasure y)
        {
            bool equal = Math.Abs(x.measure - y.measure) < CommonExtensions.GENERAL_CALC_TOLERANCE;
            if (equal)
                return 0;
            else if (x.measure > y.measure)
                return 1;
            else
                return -1;
        }
    }
    #endregion
    public static class MeshesCustom
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================= CURSTOM MESH DEFINITIONS ===================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region CHAMFER BOX AS MESH GEOMETRY
        public static MeshGeometry3D GetChamferedBox(Vector3 _center, float _sideLenX, float _sideLenY, float _sideLenZ, 
                                                                                float _chamfDistCorner, float _chamfDistEdge)
        {

            float xP = _center.X + _sideLenX * 0.5f;
            float xN = _center.X - _sideLenX * 0.5f;
            float yP = _center.Y + _sideLenY * 0.5f;
            float yN = _center.Y - _sideLenY * 0.5f;
            float zP = _center.Z + _sideLenZ * 0.5f;
            float zN = _center.Z - _sideLenZ * 0.5f;
            float a = _chamfDistCorner;
            float b = _chamfDistEdge;
            int i = 0;

            // ------------------------------ TOP -------------------------------- //
            Vector3[] positionsT = new Vector3[12];
            Vector3[] normalsT = new Vector3[12];
            int[] indicesT = new int[14 * 3]; // 14 triangles in an octagon
         
            // positions
            positionsT[0]  = new Vector3(xN + a, yP, zN + b);
            positionsT[1]  = new Vector3(xP - a, yP, zN + b);
            positionsT[2]  = new Vector3(xN + b, yP, zN + a);
            positionsT[3]  = new Vector3(xN + a, yP, zN + a);
            positionsT[4]  = new Vector3(xP - a, yP, zN + a);
            positionsT[5]  = new Vector3(xP - b, yP, zN + a);
            positionsT[6]  = new Vector3(xN + b, yP, zP - a);
            positionsT[7]  = new Vector3(xN + a, yP, zP - a);
            positionsT[8]  = new Vector3(xP - a, yP, zP - a);
            positionsT[9]  = new Vector3(xP - b, yP, zP - a);
            positionsT[10] = new Vector3(xN + a, yP, zP - b);
            positionsT[11] = new Vector3(xP - a, yP, zP - b);

            // normals
            normalsT = Enumerable.Repeat(Vector3.UnitY, 12).ToArray();

            // triangles
            indicesT[0] = 3; indicesT[1] = 0; indicesT[2] = 2;
            indicesT[3] = 1; indicesT[4] = 0; indicesT[5] = 3;
            indicesT[6] = 4; indicesT[7] = 1; indicesT[8] = 3;
            indicesT[9] = 5; indicesT[10] = 1; indicesT[11] = 4;

            indicesT[12] = 6; indicesT[13] = 3; indicesT[14] = 2;
            indicesT[15] = 7; indicesT[16] = 3; indicesT[17] = 6;
            indicesT[18] = 8; indicesT[19] = 3; indicesT[20] = 7;
            indicesT[21] = 4; indicesT[22] = 3; indicesT[23] = 8;
            indicesT[24] = 5; indicesT[25] = 4; indicesT[26] = 8;
            indicesT[27] = 9; indicesT[28] = 5; indicesT[29] = 8;

            indicesT[30] = 10; indicesT[31] = 7; indicesT[32] = 6;
            indicesT[33] = 8; indicesT[34] = 7; indicesT[35] = 10;
            indicesT[36] = 11; indicesT[37] = 8; indicesT[38] = 10;
            indicesT[39] = 9; indicesT[40] = 8; indicesT[41] = 11;

            // ---------------------------- BOTTOM ------------------------------- //

            SharpDX.Matrix M_A = Matrix.Translation(-_center);
            SharpDX.Matrix Mb_ROT = Matrix.RotationZ((float) Math.PI);
            SharpDX.Matrix M_B = Matrix.Translation(_center);
            SharpDX.Matrix Mb = M_A * Mb_ROT * M_B;

            Vector4[] positionsTh = CommonExtensions.ConvertVector3ArToVector4Ar(positionsT);
            Vector4.Transform(positionsTh, ref Mb, positionsTh);
            Vector3[] positionsB = CommonExtensions.ConvertVector4ArToVector3Ar(positionsTh);

            Vector3[] normalsB = Enumerable.Repeat(-Vector3.UnitY, 12).ToArray();

            int[] indicesB = new int[14 * 3];
            for(i = 0; i < 14 * 3; i++)
            {
                indicesB[i] = indicesT[i] + 12;
            }

            // ----------------------------- LEFT -------------------------------- //

            SharpDX.Matrix Ml_ROT = Matrix.RotationZ((float)Math.PI / 2f);
            SharpDX.Matrix Ml = M_A * Ml_ROT * M_B;

            positionsTh = CommonExtensions.ConvertVector3ArToVector4Ar(positionsT);
            Vector4.Transform(positionsTh, ref Ml, positionsTh);
            Vector3[] positionsL = CommonExtensions.ConvertVector4ArToVector3Ar(positionsTh);

            Vector3[] normalsL = Enumerable.Repeat(-Vector3.UnitX, 12).ToArray();

            int[] indicesL = new int[14 * 3];
            for (i = 0; i < 14 * 3; i++)
            {
                indicesL[i] = indicesT[i] + 12 * 2;
            }

            // ---------------------------- RIGHT -------------------------------- //

            SharpDX.Matrix Mr_ROT = Matrix.RotationZ((float)Math.PI * 3f / 2f);
            SharpDX.Matrix Mr = M_A * Mr_ROT * M_B;

            positionsTh = CommonExtensions.ConvertVector3ArToVector4Ar(positionsT);
            Vector4.Transform(positionsTh, ref Mr, positionsTh);
            Vector3[] positionsR = CommonExtensions.ConvertVector4ArToVector3Ar(positionsTh);

            Vector3[] normalsR = Enumerable.Repeat(Vector3.UnitX, 12).ToArray();

            int[] indicesR = new int[14 * 3];
            for (i = 0; i < 14 * 3; i++)
            {
                indicesR[i] = indicesT[i] + 12 * 3;
            }

            // ---------------------------- FRONT -------------------------------- //

            SharpDX.Matrix Mf_ROT = Matrix.RotationX((float)Math.PI * 3f / 2f);
            SharpDX.Matrix Mf = M_A * Mf_ROT * M_B;

            positionsTh = CommonExtensions.ConvertVector3ArToVector4Ar(positionsT);
            Vector4.Transform(positionsTh, ref Mf, positionsTh);
            Vector3[] positionsF = CommonExtensions.ConvertVector4ArToVector3Ar(positionsTh);

            Vector3[] normalsF = Enumerable.Repeat(-Vector3.UnitZ, 12).ToArray();

            int[] indicesF = new int[14 * 3];
            for (i = 0; i < 14 * 3; i++)
            {
                indicesF[i] = indicesT[i] + 12 * 4;
            }

            // ----------------------------- BACK -------------------------------- //

            SharpDX.Matrix Mba_ROT = Matrix.RotationX((float)Math.PI / 2f);
            SharpDX.Matrix Mba = M_A * Mba_ROT * M_B;

            positionsTh = CommonExtensions.ConvertVector3ArToVector4Ar(positionsT);
            Vector4.Transform(positionsTh, ref Mba, positionsTh);
            Vector3[] positionsBA = CommonExtensions.ConvertVector4ArToVector3Ar(positionsTh);

            Vector3[] normalsBA = Enumerable.Repeat(Vector3.UnitZ, 12).ToArray();

            int[] indicesBA = new int[14 * 3];
            for (i = 0; i < 14 * 3; i++)
            {
                indicesBA[i] = indicesT[i] + 12 * 5;
            }

            // -------------------- CONNECTING EDGE SEGMENTS --------------------- //

            // top -> front
            Vector3[] positionsCE_1 = new Vector3[4]; // 1 quad per edge
            Vector3[] normalsCE_1 = new Vector3[4];
            int[] indicesCE_1 = new int[6];

            positionsCE_1[0] = positionsT[0]; positionsCE_1[1] = positionsT[1];
            positionsCE_1[2] = positionsF[10]; positionsCE_1[3] = positionsF[11];

            Vector3 nCE_1 = Vector3.UnitY + (-Vector3.UnitZ);
            Vector3.Normalize(nCE_1);
            normalsCE_1 = Enumerable.Repeat(nCE_1, 4).ToArray();

            indicesCE_1[0] = 12 * 6 + 0; indicesCE_1[1] = 12 * 6 + 1; indicesCE_1[2] = 12 * 6 + 3;
            indicesCE_1[3] = 12 * 6 + 0; indicesCE_1[4] = 12 * 6 + 3; indicesCE_1[5] = 12 * 6 + 2;

            // top -> back
            Matrix Mce_R = Matrix.RotationY((float)Math.PI);
            Matrix Mce = M_A * Mce_R * M_B;

            Vector4[] positionsCE_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_1);
            Vector4.Transform(positionsCE_1h, ref Mce, positionsCE_1h);
            Vector3[] positionsCE_2 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_1h);

            Vector3 nCE_2 = Vector3.UnitY + Vector3.UnitZ;
            Vector3.Normalize(nCE_2);
            Vector3[] normalsCE_2 = Enumerable.Repeat(nCE_2, 4).ToArray();

            int[] indicesCE_2 = new int[6];
            for(i = 0; i < 6; i++)
            {
                indicesCE_2[i] = indicesCE_1[i] + 4;
            }

            // top -> left
            Mce_R = Matrix.RotationY(-(float)Math.PI / 2f);
            Mce = M_A * Mce_R * M_B;
            SharpDX.Matrix Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            positionsCE_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_1);
            Vector4.Transform(positionsCE_1h, ref Mce, positionsCE_1h);
            Vector3[] positionsCE_3 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_1h);

            Vector4[] normalsCE_1h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_1);
            Vector4.Transform(normalsCE_1h, ref Mce_Normal, normalsCE_1h);
            Vector3[] normalsCE_3 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_1h);

            int[] indicesCE_3 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_3[i] = indicesCE_1[i] + 4 * 2;
            }

            // top -> right
            Mce_R = Matrix.RotationY((float)Math.PI / 2f);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            positionsCE_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_1);
            Vector4.Transform(positionsCE_1h, ref Mce, positionsCE_1h);
            Vector3[] positionsCE_4 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_1h);

            normalsCE_1h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_1);
            Vector4.Transform(normalsCE_1h, ref Mce_Normal, normalsCE_1h);
            Vector3[] normalsCE_4 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_1h);

            int[] indicesCE_4 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_4[i] = indicesCE_1[i] + 4 * 3;
            }

            // -------------------------------------------------------------------- //

            // bottom -> front
            Vector3[] positionsCE_5 = new Vector3[4];
            Vector3[] normalsCE_5 = new Vector3[4];
            int[] indicesCE_5 = new int[6];

            positionsCE_5[0] = positionsF[0]; positionsCE_5[1] = positionsF[1];
            positionsCE_5[2] = positionsB[0]; positionsCE_5[3] = positionsB[1];

            Vector3 nCE_5 = -Vector3.UnitY + (-Vector3.UnitZ);
            Vector3.Normalize(nCE_5);
            normalsCE_5 = Enumerable.Repeat(nCE_5, 4).ToArray();

            indicesCE_5[0] = 12 * 6 + 4 * 4 + 0; indicesCE_5[1] = 12 * 6 + 4 * 4 + 1; indicesCE_5[2] = 12 * 6 + 4 * 4 + 2;
            indicesCE_5[3] = 12 * 6 + 4 * 4 + 2; indicesCE_5[4] = 12 * 6 + 4 * 4 + 3; indicesCE_5[5] = 12 * 6 + 4 * 4 + 0;

            // bottom -> back
            Mce_R = Matrix.RotationY((float)Math.PI);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            Vector4[] positionsCE_5h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_5);
            Vector4.Transform(positionsCE_5h, ref Mce, positionsCE_5h);
            Vector3[] positionsCE_6 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_5h);

            Vector4[] normalsCE_5h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_5);
            Vector4.Transform(normalsCE_5h, ref Mce_Normal, normalsCE_5h);
            Vector3[] normalsCE_6 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_5h);

            int[] indicesCE_6 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_6[i] = indicesCE_5[i] + 4;
            }

            // bottom -> left
            Mce_R = Matrix.RotationY(-(float)Math.PI / 2f);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            positionsCE_5h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_5);
            Vector4.Transform(positionsCE_5h, ref Mce, positionsCE_5h);
            Vector3[] positionsCE_7 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_5h);

            normalsCE_5h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_5);
            Vector4.Transform(normalsCE_5h, ref Mce_Normal, normalsCE_5h);
            Vector3[] normalsCE_7 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_5h);

            int[] indicesCE_7 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_7[i] = indicesCE_5[i] + 4 * 2;
            }

            // bottom -> right
            Mce_R = Matrix.RotationY((float)Math.PI / 2f);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            positionsCE_5h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_5);
            Vector4.Transform(positionsCE_5h, ref Mce, positionsCE_5h);
            Vector3[] positionsCE_8 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_5h);

            normalsCE_5h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_5);
            Vector4.Transform(normalsCE_5h, ref Mce_Normal, normalsCE_5h);
            Vector3[] normalsCE_8 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_5h);

            int[] indicesCE_8 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_8[i] = indicesCE_5[i] + 4 * 3;
            }

            // -------------------------------------------------------------------- //

            // front -> left
            Vector3[] positionsCE_9 = new Vector3[4];
            Vector3[] normalsCE_9 = new Vector3[4];
            int[] indicesCE_9 = new int[6];

            positionsCE_9[0] = positionsF[2]; positionsCE_9[1] = positionsF[6];
            positionsCE_9[2] = positionsL[0]; positionsCE_9[3] = positionsL[1];

            Vector3 nCE_9 = (-Vector3.UnitZ) + (-Vector3.UnitX);
            Vector3.Normalize(nCE_9);
            normalsCE_9 = Enumerable.Repeat(nCE_9, 4).ToArray();

            indicesCE_9[0] = 12 * 6 + 4 * 8 + 3; indicesCE_9[1] = 12 * 6 + 4 * 8 + 1; indicesCE_9[2] = 12 * 6 + 4 * 8 + 0;
            indicesCE_9[3] = 12 * 6 + 4 * 8 + 2; indicesCE_9[4] = 12 * 6 + 4 * 8 + 3; indicesCE_9[5] = 12 * 6 + 4 * 8 + 0;

            // left -> back
            Mce_R = Matrix.RotationY(-(float)Math.PI / 2f);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            Vector4[] positionsCE_9h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_9);
            Vector4.Transform(positionsCE_9h, ref Mce, positionsCE_9h);
            Vector3[] positionsCE_10 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_9h);

            Vector4[] normalsCE_9h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_9);
            Vector4.Transform(normalsCE_9h, ref Mce_Normal, normalsCE_9h);
            Vector3[] normalsCE_10 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_9h);

            int[] indicesCE_10 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_10[i] = indicesCE_9[i] + 4;
            }

            // back -> right
            Mce_R = Matrix.RotationY((float)Math.PI);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            positionsCE_9h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_9);
            Vector4.Transform(positionsCE_9h, ref Mce, positionsCE_9h);
            Vector3[] positionsCE_11 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_9h);

            normalsCE_9h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_9);
            Vector4.Transform(normalsCE_9h, ref Mce_Normal, normalsCE_9h);
            Vector3[] normalsCE_11 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_9h);

            int[] indicesCE_11 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_11[i] = indicesCE_9[i] + 4 * 2;
            }

            // right ->front
            Mce_R = Matrix.RotationY((float)Math.PI / 2f);
            Mce = M_A * Mce_R * M_B;
            Mce_Normal = Matrix.Transpose(Matrix.Invert(Mce));

            positionsCE_9h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCE_9);
            Vector4.Transform(positionsCE_9h, ref Mce, positionsCE_9h);
            Vector3[] positionsCE_12 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCE_9h);

            normalsCE_9h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCE_9);
            Vector4.Transform(normalsCE_9h, ref Mce_Normal, normalsCE_9h);
            Vector3[] normalsCE_12 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCE_9h);

            int[] indicesCE_12 = new int[6];
            for (i = 0; i < 6; i++)
            {
                indicesCE_12[i] = indicesCE_9[i] + 4 * 3;
            }

            // ------------------- CONNECTING CORNER SEGMENTS -------------------- //

            // top - front - left
            Vector3[] positionsCC_1 = new Vector3[9];
            Vector3[] normalsCC_1 = new Vector3[9];
            int[] indicesCC_1 = new int[30]; // 10 triangles

            positionsCC_1[0] = positionsT[2]; positionsCC_1[1] = positionsT[0];
            positionsCC_1[2] = positionsF[6]; positionsCC_1[3] = positionsF[10];
            positionsCC_1[4] = positionsL[5]; positionsCC_1[5] = positionsL[1];

            Vector3 positionsCC_center = (positionsCC_1[0] + positionsCC_1[1] + positionsCC_1[2] +
                                          positionsCC_1[3] + positionsCC_1[4] + positionsCC_1[5]) / 6f;

            positionsCC_1[6] = (positionsCC_1[0] + positionsCC_1[1]) / 4f + positionsCC_center / 2f;
            positionsCC_1[7] = (positionsCC_1[2] + positionsCC_1[3]) / 4f + positionsCC_center / 2f;
            positionsCC_1[8] = (positionsCC_1[4] + positionsCC_1[5]) / 4f + positionsCC_center / 2f;


            Vector3 nCC_1 = Vector3.UnitY + (-Vector3.UnitZ) + (-Vector3.UnitX);
            Vector3.Normalize(nCC_1);
            normalsCC_1 = Enumerable.Repeat(nCC_1, 9).ToArray();

            positionsCC_1[6] = positionsCC_1[6] + nCC_1 * b / 2f;
            positionsCC_1[7] = positionsCC_1[7] + nCC_1 * b / 2f;
            positionsCC_1[8] = positionsCC_1[8] + nCC_1 * b / 2f;

            indicesCC_1[0] = 12 * 6 + 4 * 12 + 6; indicesCC_1[1] = 12 * 6 + 4 * 12 + 7; indicesCC_1[2] = 12 * 6 + 4 * 12 + 8;

            indicesCC_1[3] = 12 * 6 + 4 * 12 + 0; indicesCC_1[4] = 12 * 6 + 4 * 12 + 1; indicesCC_1[5] = 12 * 6 + 4 * 12 + 6;
            indicesCC_1[6] = 12 * 6 + 4 * 12 + 3; indicesCC_1[7] = 12 * 6 + 4 * 12 + 2; indicesCC_1[8] = 12 * 6 + 4 * 12 + 7;
            indicesCC_1[9] = 12 * 6 + 4 * 12 + 5; indicesCC_1[10] = 12 * 6 + 4 * 12 + 4; indicesCC_1[11] = 12 * 6 + 4 * 12 + 8;

            indicesCC_1[12] = 12 * 6 + 4 * 12 + 1; indicesCC_1[13] = 12 * 6 + 4 * 12 + 3; indicesCC_1[14] = 12 * 6 + 4 * 12 + 7;
            indicesCC_1[15] = 12 * 6 + 4 * 12 + 1; indicesCC_1[16] = 12 * 6 + 4 * 12 + 7; indicesCC_1[17] = 12 * 6 + 4 * 12 + 6;

            indicesCC_1[18] = 12 * 6 + 4 * 12 + 2; indicesCC_1[19] = 12 * 6 + 4 * 12 + 5; indicesCC_1[20] = 12 * 6 + 4 * 12 + 8;
            indicesCC_1[21] = 12 * 6 + 4 * 12 + 2; indicesCC_1[22] = 12 * 6 + 4 * 12 + 8; indicesCC_1[23] = 12 * 6 + 4 * 12 + 7;

            indicesCC_1[24] = 12 * 6 + 4 * 12 + 4; indicesCC_1[25] = 12 * 6 + 4 * 12 + 0; indicesCC_1[26] = 12 * 6 + 4 * 12 + 6;
            indicesCC_1[27] = 12 * 6 + 4 * 12 + 4; indicesCC_1[28] = 12 * 6 + 4 * 12 + 6; indicesCC_1[29] = 12 * 6 + 4 * 12 + 8;

            // top - left - back
            Matrix Mcc_R = Matrix.RotationY(-(float)Math.PI / 2f);
            Matrix Mcc = M_A * Mcc_R * M_B;
            Matrix Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            Vector4[] positionsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_1);
            Vector4.Transform(positionsCC_1h, ref Mcc, positionsCC_1h);
            Vector3[] positionsCC_2 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_1h);

            Vector4[] normalsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_1);
            Vector4.Transform(normalsCC_1h, ref Mcc_Normal, normalsCC_1h);
            Vector3[] normalsCC_2 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_1h);

            int[] indicesCC_2 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_2[i] = indicesCC_1[i] + 9;
            }

            // top - back - right
            Mcc_R = Matrix.RotationY((float)Math.PI);
            Mcc = M_A * Mcc_R * M_B;
            Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            positionsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_1);
            Vector4.Transform(positionsCC_1h, ref Mcc, positionsCC_1h);
            Vector3[] positionsCC_3 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_1h);

            normalsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_1);
            Vector4.Transform(normalsCC_1h, ref Mcc_Normal, normalsCC_1h);
            Vector3[] normalsCC_3 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_1h);

            int[] indicesCC_3 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_3[i] = indicesCC_1[i] + 9 * 2;
            }

            // top - right - front
            Mcc_R = Matrix.RotationY((float)Math.PI / 2f);
            Mcc = M_A * Mcc_R * M_B;
            Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            positionsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_1);
            Vector4.Transform(positionsCC_1h, ref Mcc, positionsCC_1h);
            Vector3[] positionsCC_4 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_1h);

            normalsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_1);
            Vector4.Transform(normalsCC_1h, ref Mcc_Normal, normalsCC_1h);
            Vector3[] normalsCC_4 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_1h);

            int[] indicesCC_4 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_4[i] = indicesCC_1[i] + 9 * 3;
            }

            // ------------------------------------------------------------------- //

            // bottom - front - left
            Mcc_R = Matrix.RotationZ((float)Math.PI / 2f);
            Mcc = M_A * Mcc_R * M_B;
            Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            positionsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_1);
            Vector4.Transform(positionsCC_1h, ref Mcc, positionsCC_1h);
            Vector3[] positionsCC_5 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_1h);

            normalsCC_1h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_1);
            Vector4.Transform(normalsCC_1h, ref Mcc_Normal, normalsCC_1h);
            Vector3[] normalsCC_5 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_1h);

            int[] indicesCC_5 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_5[i] = indicesCC_1[i] + 9 * 4;
            }

            // bottom - left - back
            Mcc_R = Matrix.RotationY(-(float)Math.PI / 2f);
            Mcc = M_A * Mcc_R * M_B;
            Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            Vector4[] positionsCC_5h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_5);
            Vector4.Transform(positionsCC_5h, ref Mcc, positionsCC_5h);
            Vector3[] positionsCC_6 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_5h);

            Vector4[] normalsCC_5h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_5);
            Vector4.Transform(normalsCC_5h, ref Mcc_Normal, normalsCC_5h);
            Vector3[] normalsCC_6 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_5h);

            int[] indicesCC_6 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_6[i] = indicesCC_1[i] + 9 * 5;
            }

            // bottom - back - right
            Mcc_R = Matrix.RotationY((float)Math.PI);
            Mcc = M_A * Mcc_R * M_B;
            Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            positionsCC_5h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_5);
            Vector4.Transform(positionsCC_5h, ref Mcc, positionsCC_5h);
            Vector3[] positionsCC_7 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_5h);

            normalsCC_5h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_5);
            Vector4.Transform(normalsCC_5h, ref Mcc_Normal, normalsCC_5h);
            Vector3[] normalsCC_7 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_5h);

            int[] indicesCC_7 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_7[i] = indicesCC_1[i] + 9 * 6;
            }

            // bottom - right - front
            Mcc_R = Matrix.RotationY((float)Math.PI / 2f);
            Mcc = M_A * Mcc_R * M_B;
            Mcc_Normal = Matrix.Transpose(Matrix.Invert(Mcc));

            positionsCC_5h = CommonExtensions.ConvertVector3ArToVector4Ar(positionsCC_5);
            Vector4.Transform(positionsCC_5h, ref Mcc, positionsCC_5h);
            Vector3[] positionsCC_8 = CommonExtensions.ConvertVector4ArToVector3Ar(positionsCC_5h);

            normalsCC_5h = CommonExtensions.ConvertVector3ArToVector4Ar(normalsCC_5);
            Vector4.Transform(normalsCC_5h, ref Mcc_Normal, normalsCC_5h);
            Vector3[] normalsCC_8 = CommonExtensions.ConvertVector4ArToVector3Ar(normalsCC_5h);

            int[] indicesCC_8 = new int[30];
            for (i = 0; i < 30; i++)
            {
                indicesCC_8[i] = indicesCC_1[i] + 9 * 7;
            }

            // -------------- ASSEMBLE ALL IN SINGLE GEOMETRY ARRAY -------------- //

            // positions
            Vector3[] positions = new Vector3[12 * 6 + 4 * 12 + 9 * 8];

            positionsT.CopyTo(positions, 0);
            positionsB.CopyTo(positions, 12);
            positionsL.CopyTo(positions, 12 * 2);
            positionsR.CopyTo(positions, 12 * 3);
            positionsF.CopyTo(positions, 12 * 4);
            positionsBA.CopyTo(positions, 12 * 5);

            positionsCE_1.CopyTo(positions, 12 * 6);
            positionsCE_2.CopyTo(positions, 12 * 6 + 4);
            positionsCE_3.CopyTo(positions, 12 * 6 + 4 * 2);
            positionsCE_4.CopyTo(positions, 12 * 6 + 4 * 3);
            positionsCE_5.CopyTo(positions, 12 * 6 + 4 * 4);
            positionsCE_6.CopyTo(positions, 12 * 6 + 4 * 5);
            positionsCE_7.CopyTo(positions, 12 * 6 + 4 * 6);
            positionsCE_8.CopyTo(positions, 12 * 6 + 4 * 7);
            positionsCE_9.CopyTo(positions, 12 * 6 + 4 * 8);
            positionsCE_10.CopyTo(positions, 12 * 6 + 4 * 9);
            positionsCE_11.CopyTo(positions, 12 * 6 + 4 * 10);
            positionsCE_12.CopyTo(positions, 12 * 6 + 4 * 11);

            positionsCC_1.CopyTo(positions, 12 * 6 + 4 * 12);
            positionsCC_2.CopyTo(positions, 12 * 6 + 4 * 12 + 9);
            positionsCC_3.CopyTo(positions, 12 * 6 + 4 * 12 + 9 * 2);
            positionsCC_4.CopyTo(positions, 12 * 6 + 4 * 12 + 9 * 3);
            positionsCC_5.CopyTo(positions, 12 * 6 + 4 * 12 + 9 * 4);
            positionsCC_6.CopyTo(positions, 12 * 6 + 4 * 12 + 9 * 5);
            positionsCC_7.CopyTo(positions, 12 * 6 + 4 * 12 + 9 * 6);
            positionsCC_8.CopyTo(positions, 12 * 6 + 4 * 12 + 9 * 7);

            // normals
            Vector3[] normals = new Vector3[12 * 6 + 4 * 12 + 9 * 8];

            normalsT.CopyTo(normals, 0);
            normalsB.CopyTo(normals, 12);
            normalsL.CopyTo(normals, 12 * 2);
            normalsR.CopyTo(normals, 12 * 3);
            normalsF.CopyTo(normals, 12 * 4);
            normalsBA.CopyTo(normals, 12 * 5);

            normalsCE_1.CopyTo(normals, 12 * 6);
            normalsCE_2.CopyTo(normals, 12 * 6 + 4);
            normalsCE_3.CopyTo(normals, 12 * 6 + 4 * 2);
            normalsCE_4.CopyTo(normals, 12 * 6 + 4 * 3);
            normalsCE_5.CopyTo(normals, 12 * 6 + 4 * 4);
            normalsCE_6.CopyTo(normals, 12 * 6 + 4 * 5);
            normalsCE_7.CopyTo(normals, 12 * 6 + 4 * 6);
            normalsCE_8.CopyTo(normals, 12 * 6 + 4 * 7);
            normalsCE_9.CopyTo(normals, 12 * 6 + 4 * 8);
            normalsCE_10.CopyTo(normals, 12 * 6 + 4 * 9);
            normalsCE_11.CopyTo(normals, 12 * 6 + 4 * 10);
            normalsCE_12.CopyTo(normals, 12 * 6 + 4 * 11);

            normalsCC_1.CopyTo(normals, 12 * 6 + 4 * 12);
            normalsCC_2.CopyTo(normals, 12 * 6 + 4 * 12 + 9);
            normalsCC_3.CopyTo(normals, 12 * 6 + 4 * 12 + 9 * 2);
            normalsCC_4.CopyTo(normals, 12 * 6 + 4 * 12 + 9 * 3);
            normalsCC_5.CopyTo(normals, 12 * 6 + 4 * 12 + 9 * 4);
            normalsCC_6.CopyTo(normals, 12 * 6 + 4 * 12 + 9 * 5);
            normalsCC_7.CopyTo(normals, 12 * 6 + 4 * 12 + 9 * 6);
            normalsCC_8.CopyTo(normals, 12 * 6 + 4 * 12 + 9 * 7);

            // trangle face indices
            int[] indices = new int[14 * 3 * 6 + 6 * 12 + 30 * 8];

            indicesT.CopyTo(indices, 0);
            indicesB.CopyTo(indices, 14 * 3);
            indicesL.CopyTo(indices, 14 * 3 * 2);
            indicesR.CopyTo(indices, 14 * 3 * 3);
            indicesF.CopyTo(indices, 14 * 3 * 4);
            indicesBA.CopyTo(indices, 14 * 3 * 5);

            indicesCE_1.CopyTo(indices, 14 * 3 * 6);
            indicesCE_2.CopyTo(indices, 14 * 3 * 6 + 6);
            indicesCE_3.CopyTo(indices, 14 * 3 * 6 + 6 * 2);
            indicesCE_4.CopyTo(indices, 14 * 3 * 6 + 6 * 3);
            indicesCE_5.CopyTo(indices, 14 * 3 * 6 + 6 * 4);
            indicesCE_6.CopyTo(indices, 14 * 3 * 6 + 6 * 5);
            indicesCE_7.CopyTo(indices, 14 * 3 * 6 + 6 * 6);
            indicesCE_8.CopyTo(indices, 14 * 3 * 6 + 6 * 7);
            indicesCE_9.CopyTo(indices, 14 * 3 * 6 + 6 * 8);
            indicesCE_10.CopyTo(indices, 14 * 3 * 6 + 6 * 9);
            indicesCE_11.CopyTo(indices, 14 * 3 * 6 + 6 * 10);
            indicesCE_12.CopyTo(indices, 14 * 3 * 6 + 6 * 11);

            indicesCC_1.CopyTo(indices, 14 * 3 * 6 + 6 * 12);
            indicesCC_2.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30);
            indicesCC_3.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30 * 2);
            indicesCC_4.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30 * 3);
            indicesCC_5.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30 * 4);
            indicesCC_6.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30 * 5);
            indicesCC_7.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30 * 6);
            indicesCC_8.CopyTo(indices, 14 * 3 * 6 + 6 * 12 + 30 * 7);

            MeshGeometry3D g = new MeshGeometry3D
            {
                Positions = positions,
                Indices = indices,
                Normals = normals
            };

            return g;

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================ EXTRACTING LINE GEOMETRY FROM MESH GEOMETRY =============================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region EXTRACT LINE GEOMETRY FROM A MESH GEOMETRY OBJECT
        public static LineGeometry3D GetEdgesAsLines(MeshGeometry3D _meshG)
        {
            if (_meshG == null)
                return null;

            Vector3[] linePositions = _meshG.Positions;
            int[] indices = _meshG.Indices;
            int[] lineIndices = new int[indices.Count() * 2];

            for(int i = 0, index = 0 ; i < indices.Count(); i += 3, index += 6)
            {
                lineIndices[index]     = indices[i];
                lineIndices[index + 1] = indices[i + 1];

                lineIndices[index + 2] = indices[i + 1];
                lineIndices[index + 3] = indices[i + 2];

                lineIndices[index + 4] = indices[i + 2];
                lineIndices[index + 5] = indices[i];
            }

            return new LineGeometry3D()
            {
                Positions= linePositions,
                Indices = lineIndices
            };

        }

        public static LineGeometry3D GetFaceNormalsAsLines(MeshGeometry3D _meshG, float _scale = 1f)
        {
            if (_meshG == null)
                return null;

            Vector3[] positions = _meshG.Positions;
            Vector3[] normals = _meshG.Normals;
            int[] indices = _meshG.Indices;

            Vector3[] linePositions = new Vector3[indices.Count() * 2];
            int[] lineIndices = new int[indices.Count() * 2];
            int index = 0;

            for (int i = 0; i < indices.Count(); i += 3)
            {
                Vector3 p0 = positions[indices[i]];
                Vector3 p1 = positions[indices[i + 1]];
                Vector3 p2 = positions[indices[i + 2]];
                Vector3 n0 = (p0 + p1 + p2) / 3f;

                Vector3 q0 = normals[indices[i]];
                Vector3 q1 = normals[indices[i + 1]];
                Vector3 q2 = normals[indices[i + 2]];
                Vector3 n1 = n0 + (q0 + q1 + q2) * _scale/ 3f;

                linePositions[index] = n0;
                linePositions[index + 1] = n1;
                lineIndices[index] = index;
                lineIndices[index + 1] = index + 1;
                index += 2;

            }

            return new LineGeometry3D()
            {
                Positions= linePositions,
                Indices = lineIndices
            };

        }

        public static LineGeometry3D GetVertexNormalsAsLines(MeshGeometry3D _meshG, float _scale = 1f)
        {
            if (_meshG == null)
                return null;

            Vector3[] positions = _meshG.Positions;
            Vector3[] normals = _meshG.Normals;
            if (positions.Count() != normals.Count())
                return null;

            Vector3[] linePositions = new Vector3[positions.Count() * 2];
            int[] lineIndices = new int[positions.Count() * 2];
            int index = 0;

            for (int i = 0; i < positions.Count(); i++)
            {
                Vector3 n0 = positions[i];
                Vector3 n1 = n0 + normals[i] * _scale;

                linePositions[index] = n0;
                linePositions[index + 1] = n1;
                lineIndices[index] = index;
                lineIndices[index + 1] = index + 1;
                index += 2;
            }

            return new LineGeometry3D()
            {
                Positions = linePositions,
                Indices = lineIndices
            };

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== OPERATIONS ON MESH GEOMETRY ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MESH OPERATIONS - Data Compression and Normal Interpolation
        // removes duplicate position and normal entries and rearranges the indices
        // if there is more than one normal for the same position,
        // the resulting normal is a linear interpolation of all normals at this position
        public static void CompressMesh(ref MeshGeometry3D _meshG, float _tolerance = 0.001f)
        {
            if (_meshG == null)
                return;

            Vector3[] positions = _meshG.Positions;
            Vector3[] normals = _meshG.Normals;
            int[] indices = _meshG.Indices;

            Vector3Comparer v3cmp = new Vector3Comparer(_tolerance);
            HashSet<Vector3> posUnique = new HashSet<Vector3>(v3cmp);
            List<Vector3> posNew = new List<Vector3>();

            List<List<Vector3>> normalsPerPos = new List<List<Vector3>>();

            int[] indicesNew = new int[indices.Count()];

            for(int i = 0, index = 0; i < indices.Count(); i++)
            {
                Vector3 p0 = positions[indices[i]];
                Vector3 n0 = normals[indices[i]];

                //// DEBUG
                //if (p0.X == -0.45f && p0.Y == -0.5f && p0.Z == 0.3f)
                //{
                //    int h21 = v3cmp.GetHashCode(posNew[21]);
                //    int hNew = v3cmp.GetHashCode(p0);
                //    bool eq = v3cmp.Equals(posNew[21], p0);

                //    HashSet<Vector3> testSet = new HashSet<Vector3>(v3cmp);
                //    testSet.Add(posNew[21]);
                //    bool test1 = testSet.Contains(p0);
                //    bool test2 = posNew.Contains(p0, v3cmp);
                //}

                bool posIsUnique = posUnique.Add(p0);
                if (posIsUnique)
                {
                    posNew.Add(p0);
                    normalsPerPos.Add(new List<Vector3>() { n0 });
                    indicesNew[i] = index;
                    index++;
                }
                else
                {
                    int indexFound = posNew.IndexOf(p0);
                    if (indexFound > -1 && indexFound < normalsPerPos.Count)
                    {
                        normalsPerPos[indexFound].Add(n0);
                        indicesNew[i] = indexFound;
                    }
                } 
            }

            // extract positions and interpolate normals
            Vector3[] posCompressed = posNew.ToArray();
            int nrPosCompr = posCompressed.Count();
            Vector3[] normCompressed = new Vector3[nrPosCompr];

            for(int j = 0; j < nrPosCompr; j++)
            {
                Vector3[] ns = normalsPerPos[j].ToArray();
                Vector3 normalInterp = Vector3.Zero;
                for(int k = 0; k < ns.Count(); k++)
                {
                    normalInterp += ns[k];
                }
                normalInterp.Normalize();
                normCompressed[j] = normalInterp;
            }

            // redefine mesh
            _meshG.Positions = posCompressed;
            _meshG.Normals = normCompressed;
            _meshG.Indices = indicesNew;

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =================================== MESH GENERATION FROM USER DATA ===================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MESH GENERATION: for Zone Representation (Building Physics)

        public static MeshGeometry3D MeshFromQuads(List<List<Vector3>> _quads)
        {
            if (_quads == null || _quads.Count < 1)
                return null;

            List<Vector3> pos = new List<Vector3>();
            List<Vector3> norm = new List<Vector3>();
            List<int> ind = new List<int>();
            List<Vector2> texc = new List<Vector2>();

            int n = _quads.Count;
            for(int i = 0; i < n; i++)
            {
                List<Vector3> quad = _quads[i];
                if (quad.Count != 4)
                    continue;

                Vector3 normal;
                bool goodTriangle = TriangleIsWellDefined(quad[0], quad[1], quad[2], out normal);
                if (goodTriangle)
                {
                    pos.Add(quad[0]);
                    pos.Add(quad[1]);
                    pos.Add(quad[2]);

                    norm.Add(normal);
                    norm.Add(normal);
                    norm.Add(normal);

                    texc.Add(new Vector2(0, 0));
                    texc.Add(new Vector2(1, 0));
                    texc.Add(new Vector2(1, 1));

                    int nrInd = ind.Count;
                    ind.Add(nrInd);
                    ind.Add(nrInd + 1);
                    ind.Add(nrInd + 2);
                }
                goodTriangle = TriangleIsWellDefined(quad[0], quad[2], quad[3], out normal);
                if (goodTriangle)
                {
                    pos.Add(quad[0]);
                    pos.Add(quad[2]);
                    pos.Add(quad[3]);

                    norm.Add(normal);
                    norm.Add(normal);
                    norm.Add(normal);

                    texc.Add(new Vector2(0, 0));
                    texc.Add(new Vector2(1, 1));
                    texc.Add(new Vector2(0, 1));

                    int nrInd = ind.Count;
                    ind.Add(nrInd);
                    ind.Add(nrInd + 1);
                    ind.Add(nrInd + 2);
                }
            }

            return new MeshGeometry3D()
            {
                Positions = pos.ToArray(),
                Normals = norm.ToArray(),
                Indices = ind.ToArray(),
                TextureCoordinates = texc.ToArray(),
            };
        }

        // uses the triangulation algorithm for arbitrary polygons (incl. concave polygon with any number of concave holes)
        public static MeshGeometry3D PolygonComplexFill(List<Point3D> _poly, List<List<Point3D>> _holes, bool _reverse = false)
        {
            if (_poly == null || _poly.Count < 3)
                return null;

            if (PolygonIsConvexXZ(_poly) && _holes == null)
            {
                // use the simpler algorithm
                List<Vector3> poly_V3 = CommonExtensions.ConvertPoints3DListToVector3List(_poly);
                return PolygonFill(poly_V3, _reverse);
            }

            // reverse the winding direction of the polygon and holes
            List<Point3D> polyIN = new List<Point3D>(_poly);
            List<List<Point3D>> holesIN = null;
            if (_holes != null)
                holesIN = new List<List<Point3D>>(_holes);

            if (_reverse)
            {
                polyIN = ReversePolygon(_poly);
                holesIN = new List<List<Point3D>>();
                if (_holes != null)
                {
                    foreach (List<Point3D> hole in _holes)
                    {
                        holesIN.Add(ReversePolygon(hole));
                    }
                }
            }

            // perform actual algorithm
            // List<List<Point3D>> simplePolys = DecomposeInSimplePolygons(polyIN, holesIN); // xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
            List<List<Point3D>> simplePolys = DecomposeInSimplePolygons_Improved(polyIN, holesIN); // ........................................................
            List<List<Point3D>> monotonePolys = new List<List<Point3D>>();
            foreach(List<Point3D> spoly in simplePolys)
            {
                List<List<Point3D>> mpolys = DecomposeInMonotonePolygons(spoly);
                monotonePolys.AddRange(mpolys);
            }

            List<MeshGeometry3D> triangulation = new List<MeshGeometry3D>();
            foreach(List<Point3D> mpoly in monotonePolys)
            {
                List<Vector3> mpoly_V3 = CommonExtensions.ConvertPoints3DListToVector3List(mpoly);
                MeshGeometry3D fill = PolygonFillMonotone(mpoly_V3);
                if (fill != null)
                    triangulation.Add(fill);
            }

            MeshGeometry3D finalTriangulation = CombineMeshes(triangulation);
            return finalTriangulation;
        }

        // for vertical surfaces
        public static MeshGeometry3D PolygonComplexFillAfterRotation(List<Point3D> _poly, List<List<Point3D>> _holes, bool _reverse = false)
        {
            List<Point3D> poly_rotated = new List<Point3D>();
            if (_poly != null)
            {
                foreach(Point3D p in _poly)
                {
                    poly_rotated.Add(new Point3D(p.Z, p.X, p.Y));
                }
            }
            List<List<Point3D>> holes_rotated = null;
            if (_holes != null)
            {
                holes_rotated = new List<List<Point3D>>();
                foreach(List<Point3D> h in _holes)
                {
                    List<Point3D> h_rotated = new List<Point3D>();
                    foreach(Point3D p in h)
                    {
                        h_rotated.Add(new Point3D(p.Z, p.X, p.Y));
                    }
                    holes_rotated.Add(h_rotated);
                }
            }

            MeshGeometry3D rotated_triangulation = PolygonComplexFill(poly_rotated, holes_rotated, _reverse);
            if (rotated_triangulation == null)
                return null;
            
            // rotate back
            Vector3[] pos = rotated_triangulation.Positions;
            Vector3[] norm = rotated_triangulation.Normals;
            Vector3[] tang = rotated_triangulation.Tangents;
            Vector3[] btang = rotated_triangulation.BiTangents;

            Vector3[] pos_rot = null;
            if (pos != null)
                pos_rot = pos.Select(p => new Vector3(p.Y, p.Z, p.X)).ToArray();

            Vector3[] norm_rot = null;
            if (norm != null)
                norm_rot = norm.Select(p => new Vector3(p.Y, p.Z, p.X)).ToArray();

            Vector3[] tang_rot = null;
            if (tang != null)
                tang_rot = tang.Select(p => new Vector3(p.Y, p.Z, p.X)).ToArray();

            Vector3[] btang_rot = null;
            if (btang != null)
                btang_rot = btang.Select(p => new Vector3(p.Y, p.Z, p.X)).ToArray();

            rotated_triangulation.Positions = pos_rot;
            rotated_triangulation.Normals = norm_rot;
            rotated_triangulation.Tangents = tang_rot;
            rotated_triangulation.BiTangents = btang_rot;

            return rotated_triangulation;
        }

        public static MeshGeometry3D PolygonComplexFillAfterReOrientation(List<Point3D> _poly, List<List<Point3D>> _holes, bool _reverse = false)
        {
            Orientation or_original = MeshesCustom.CalculatePolygonOrientation(CommonExtensions.ConvertPoints3DListToVector3List(_poly));
            List<Point3D> poly_ro = MeshesCustom.ReOrientPolygon(_poly, Orientation.XZ);

            List<List<Point3D>> holes_ro = null;
            if (_holes != null)
            {
                holes_ro = new List<List<Point3D>>();
                foreach (List<Point3D> h in _holes)
                {
                    List<Point3D> h_ro = MeshesCustom.ReOrientPolygon(h, Orientation.XZ);
                    holes_ro.Add(h_ro);
                }                
            }

            MeshGeometry3D rotated_triangulation = PolygonComplexFill(poly_ro, holes_ro, _reverse);
            MeshesCustom.ReOrientMesh(ref rotated_triangulation, Orientation.XZ, or_original);
            return rotated_triangulation;
        }

        #endregion

        #region MESH GENERATION: General

        public static MeshGeometry3D MeshFrom2Polygons(List<Vector3> _poly1, List<Vector3> _poly2)
        {
            if (_poly1 == null || _poly2 == null)
                return null;

            int n1 = _poly1.Count;
            int n2 = _poly2.Count;

            if ((n1 + n2) < 3)
                return null;

            List<Vector3> pos = new List<Vector3>();
            List<Vector3> norm = new List<Vector3>();
            List<int> ind = new List<int>();

            Vector3 p0, p1, p2, normal;

            // main loop btw the two polygons
            bool odd = true;
            int i = 0, j = 0;
            while(i < n1 && j < n2)
            {
                // extract next positions points
                if (odd)
                {
                    p0 = _poly1[i % n1];
                    p1 = _poly2[j % n2];
                    p2 = _poly2[(j + 1) % n2];
                    j++;
                }
                else
                {
                    p0 = _poly2[j % n2];
                    p1 = _poly1[(i + 1) % n1];
                    p2 = _poly1[i % n1];
                    i++;
                }
                odd = !odd;

                // check if triangle is well defined
                bool goodTriangle = TriangleIsWellDefined(p0, p1, p2, out normal);
                if (!goodTriangle)
                    continue;

                // build triangle
                pos.Add(p0);
                pos.Add(p1);
                pos.Add(p2);

                norm.Add(normal);
                norm.Add(normal);
                norm.Add(normal);

                int nrInd = ind.Count;
                ind.Add(nrInd);
                ind.Add(nrInd + 1);
                ind.Add(nrInd + 2);
                
            }

            // close gaps with a triangle fan
            if (i < n1)
            {
                while (i < n1)
                {
                    p0 = _poly2[0];
                    p1 = _poly1[(i + 1) % n1];
                    p2 = _poly1[i % n1];
                    i++;

                    // check if triangle is well defined
                    bool goodTriangle = TriangleIsWellDefined(p0, p1, p2, out normal);
                    if (!goodTriangle)
                        continue;

                    // build triangle
                    pos.Add(p0);
                    pos.Add(p1);
                    pos.Add(p2);

                    norm.Add(normal);
                    norm.Add(normal);
                    norm.Add(normal);

                    int nrInd = ind.Count;
                    ind.Add(nrInd);
                    ind.Add(nrInd + 1);
                    ind.Add(nrInd + 2);
                }
            }
            else if (j < n2)
            {
                while (j < n2)
                {
                    p0 = _poly1[0];
                    p1 = _poly2[j % n2];
                    p2 = _poly2[(j + 1) % n2];
                    j++;

                    // check if triangle is well defined
                    bool goodTriangle = TriangleIsWellDefined(p0, p1, p2, out normal);
                    if (!goodTriangle)
                        continue;

                    // build triangle
                    pos.Add(p0);
                    pos.Add(p1);
                    pos.Add(p2);

                    norm.Add(normal);
                    norm.Add(normal);
                    norm.Add(normal);

                    int nrInd = ind.Count;
                    ind.Add(nrInd);
                    ind.Add(nrInd + 1);
                    ind.Add(nrInd + 2);
                }
            }

            return new MeshGeometry3D()
            {
                Positions = pos.ToArray(),
                Normals = norm.ToArray(),
                Indices = ind.ToArray()
            };
        }

        // very simple triangulation: from the pivot (only for convex polygons w/o holes)
        public static MeshGeometry3D PolygonFill(List<Vector3> _poly, bool _reverse = false)
        {
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();

            if (_poly == null || _poly.Count < 3)
                return null;
   
            int n = _poly.Count;
            int i;
            
            // get pivot of polygon
            Vector3 center = Vector3.Zero;
            for(i = 0; i < n; i++)
            {
                center += _poly[i];
            }
            center /= n;

            // get face normal of polygon
            Vector3 normal;
            bool goodPolygon;
            if (_reverse)
                goodPolygon = TriangleIsWellDefined(center, _poly[0], _poly[1], out normal);
            else     
                goodPolygon = TriangleIsWellDefined(center, _poly[1], _poly[0], out normal);
            if (!goodPolygon)
                return null;

            // save positions
            positions = new List<Vector3>{center};
            positions.AddRange(_poly);

            // save normals
            normals = Enumerable.Repeat(normal, n + 1).ToList();

            // save indices
            for(i = 0; i < n; i++)
            {
                if (_reverse)
                {
                    indices.Add(0);
                    indices.Add(i + 1);
                    indices.Add((i + 1) % n + 1);
                }
                else
                {   
                    indices.Add(0);
                    indices.Add((i + 1) % n + 1);
                    indices.Add(i + 1);
                }
            }

            return new MeshGeometry3D()
            {
                Positions = positions.ToArray(),
                Normals = normals.ToArray(),
                Indices = indices.ToArray()
            };

        }

        public static MeshGeometry3D MeshFrom2Polygons(List<Vector3> _poly1, List<Vector3> _poly2, bool _cap1, bool _cap2,
                                                        bool _reverse1 = false, bool _reverse2 = false)
        {
            if (_poly1 == null || _poly2 == null)
                return null;

            if (_reverse1)
                _poly1.Reverse();
            if (_reverse2)
                _poly2.Reverse();

            MeshGeometry3D mgConnecting = MeshFrom2Polygons(_poly1, _poly2);
            if (mgConnecting == null)
                return null;

            MeshGeometry3D mg1 = null;
            if (_cap1)
                mg1 = PolygonFill(_poly1, true);
            MeshGeometry3D mg2 = null;
            if (_cap2)
                mg2 = PolygonFill(_poly2, false);


            return CombineMeshes(new List<MeshGeometry3D> { mgConnecting, mg1, mg2 });
        }

        public static MeshGeometry3D MeshFromNPolygons(List<List<Vector3>> _polys, List<bool> _reverse, bool _capS, bool _capE)
        {
            if (_polys == null)
                return null;

            int nrPolygons = _polys.Count;
            if (nrPolygons < 2)
                return null;

            int i;

            // reverse polygon direction, if necessary
            if (_reverse != null && _reverse.Count() == nrPolygons)
            {
                for (i = 0; i < nrPolygons; i++)
                {
                    if (_reverse[i])
                        _polys[i].Reverse();
                }
            }

            // build the connecting mesh segments
            List<MeshGeometry3D> allMeshes = new List<MeshGeometry3D>();
            for(i = 0; i < nrPolygons - 1; i++)
            {
                allMeshes.Add(MeshFrom2Polygons(_polys[i], _polys[i + 1]));
            }

            // build the capping mesh segments
            MeshGeometry3D mgS = null;
            if (_capS)
                mgS = PolygonFill(_polys[0], true);
            MeshGeometry3D mgE = null;
            if (_capE)
                mgE = PolygonFill(_polys[nrPolygons - 1], false);

            // combine the meshes
            allMeshes.Add(mgS);
            allMeshes.Add(mgE);
            return CombineMeshes(allMeshes);

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================== ARBITRARY POLYGON TRIANGULATION ALGORITHM =============================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Decomposition of Polygon w Holes into Simple Polygons

        public static List<List<Point3D>> DecomposeInSimplePolygons_Improved(List<Point3D> _polygon, List<List<Point3D>> _holes)
        {
            if (_polygon == null)
                return null;

            if (_holes == null || _holes.Count < 1)
                return new List<List<Point3D>> { _polygon };

            int n = _polygon.Count;
            if (n < 3)
                return new List<List<Point3D>> { _polygon };

            // make sure the winding direction of the polygon and all contained holes is the same!
            bool figure_is_valid;
            bool polygon_cw = CalculateIfPolygonClockWise(_polygon, CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
            int nrH = _holes.Count;
            for (int i = 0; i < nrH; i++)
            {
                bool hole_cw = CalculateIfPolygonClockWise(_holes[i], CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
                if (polygon_cw != hole_cw)
                {
                    _holes[i] = ReversePolygon(_holes[i]);
                }
            }

            // create connections btw the polygon and the holes contained in it
            // this method assumes that the polygon is not self-intersecting
            // and that the holes are disjunct and completely inside the polygon
            List<Vector4> connectingLines;
            // ConnectPolygonWContainedHoles(_polygon, _holes, out connectingLines); // xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
            ConnectPolygonWContainedHolesTwice(_polygon, _holes, out connectingLines); // ........................................................
            int nrCL = connectingLines.Count;
            if (nrCL < 2)
                return new List<List<Point3D>> { _polygon };

            // perform decomposition (no duplicates in the connecting lines)
            List<int> holes_toSplit = Enumerable.Range(0, nrH).ToList();
            List<bool> connectingLines_used = Enumerable.Repeat(false, nrCL).ToList();

            // find all SPLITTING PATHS
            List<List<Vector4>> splitting_paths = new List<List<Vector4>>();
            while (holes_toSplit.Count > 0)
            {
                // look for a connected path of connecting lines 
                // that STARTS at the polygon, goes THROUGH a not yet split hole, and ENDS at the polygon
                // or a hole that has been split already
                List<Vector4> splitting_path = new List<Vector4>();
                // START
                bool reached_other_end = false;
                List<int> holes_to_remove_from_toSplit = new List<int>(); // .......................................................................
                for (int i = 0; i < nrCL; i++)
                {
                    if (connectingLines_used[i])
                        continue;
                    // start at the polygon or a hole that has already been split
                    if (connectingLines[i].X == -1 || !holes_toSplit.Contains((int)connectingLines[i].X))
                    {
                        splitting_path.Add(connectingLines[i]);
                        connectingLines_used[i] = true;

                        int split_hole_ind = (int)splitting_path[0].Z;
                        if (holes_toSplit.Contains(split_hole_ind))
                        {
                            // holes_toSplit.Remove(split_hole_ind); // xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
                            if (!holes_to_remove_from_toSplit.Contains(split_hole_ind)) // ........................................................
                                holes_to_remove_from_toSplit.Add(split_hole_ind); 
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }
                }

                // HOLES and END
                int nrSP = splitting_path.Count;
                if (nrSP == 0)
                    break;

                int maxNrIter = connectingLines.Count;
                int counter_iterations = 0;

                List<int> holes_used_in_this_path = new List<int>(); // ...........................................................................
                while (!reached_other_end && counter_iterations <= maxNrIter)
                {
                    counter_iterations++;

                    for (int i = 0; i < nrCL; i++)
                    {
                        if (connectingLines_used[i])
                            continue;

                        if (connectingLines[i].X == splitting_path[nrSP - 1].Z && connectingLines[i].Y != splitting_path[nrSP - 1].W &&
                            !holes_used_in_this_path.Contains((int)connectingLines[i].Z)) // .......................................................
                        {
                            splitting_path.Add(connectingLines[i]);
                            holes_used_in_this_path.Add((int)connectingLines[i].X); // .............................................................
                        }
                        else if (connectingLines[i].Z == splitting_path[nrSP - 1].Z && connectingLines[i].W != splitting_path[nrSP - 1].W &&
                                !holes_used_in_this_path.Contains((int)connectingLines[i].X)) // ...................................................
                        {
                            Vector4 conn_new = new Vector4(connectingLines[i].Z, connectingLines[i].W,
                                                           connectingLines[i].X, connectingLines[i].Y);
                            splitting_path.Add(conn_new);
                            holes_used_in_this_path.Add((int)connectingLines[i].Z); // .............................................................
                        }
                        else
                            continue;

                        nrSP = splitting_path.Count;
                        connectingLines_used[i] = true;

                        int split_hole_ind = (int)splitting_path[nrSP - 1].Z;
                        if (holes_toSplit.Contains(split_hole_ind))
                        {
                            // holes_toSplit.Remove(split_hole_ind); // xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
                            if (!holes_to_remove_from_toSplit.Contains(split_hole_ind))  // ........................................................
                                holes_to_remove_from_toSplit.Add(split_hole_ind);
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }

                }
                foreach (int id in holes_to_remove_from_toSplit) // ................................................................................
                {
                    holes_toSplit.Remove(id);
                }
                splitting_paths.Add(splitting_path);
            }
            int d = splitting_paths.Count;

            // perform splitting
            List<List<Point3D>> list_before_Split_polys = new List<List<Point3D>>();
            List<List<Vector2>> list_before_Split_inds = new List<List<Vector2>>();

            List<List<Point3D>> list_after_Split_polys = new List<List<Point3D>>();
            List<List<Vector2>> list_after_Split_inds = new List<List<Vector2>>();

            list_before_Split_polys.Add(_polygon);
            list_before_Split_inds.Add(GenerateDoubleIndices(-1, 0, n));

            for (int j = 0; j < d; j++)
            {
                int nrToSplit = list_before_Split_polys.Count;
                for (int k = 0; k < nrToSplit; k++)
                {
                    List<Point3D> polyA, polyB;
                    List<Vector2> originalIndsA, originalIndsB;
                    bool inputValid;
                    SplitPolygonWHolesAlongPath(list_before_Split_polys[k], list_before_Split_inds[k],
                                                splitting_paths[j], true, _holes,
                                                out polyA, out polyB, out originalIndsA, out originalIndsB, out inputValid);

                    if (polyA.Count > 2 && polyB.Count > 2)
                    {
                        // successful split
                        list_after_Split_polys.Add(polyA);
                        list_after_Split_inds.Add(originalIndsA);
                        list_after_Split_polys.Add(polyB);
                        list_after_Split_inds.Add(originalIndsB);
                    }
                    else
                    {
                        // no split
                        list_after_Split_polys.Add(list_before_Split_polys[k]);
                        list_after_Split_inds.Add(list_before_Split_inds[k]);
                    }
                }
                // swap lists
                list_before_Split_polys = new List<List<Point3D>>(list_after_Split_polys);
                list_before_Split_inds = new List<List<Vector2>>(list_after_Split_inds);
                list_after_Split_polys = new List<List<Point3D>>();
                list_after_Split_inds = new List<List<Vector2>>();
            }


            return list_before_Split_polys;
        }

        public static List<List<Point3D>> DecomposeInSimplePolygons(List<Point3D> _polygon, List<List<Point3D>> _holes)
        {
            if (_polygon == null)
                return null;

            if (_holes == null || _holes.Count < 1)
                return new List<List<Point3D>> { _polygon };

            int n = _polygon.Count;
            if ( n < 3)
                return new List<List<Point3D>> { _polygon };

            // make sure the winding direction of the polygon and all contained holes is the same!
            bool figure_is_valid;
            bool polygon_cw = CalculateIfPolygonClockWise(_polygon, CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
            int nrH = _holes.Count;
            for (int i = 0; i < nrH; i++ )
            {
                bool hole_cw = CalculateIfPolygonClockWise(_holes[i], CommonExtensions.GENERAL_CALC_TOLERANCE, out figure_is_valid);
                if (polygon_cw != hole_cw)
                {
                    _holes[i] = ReversePolygon(_holes[i]);
                }
            }

            // create connections btw the polygon and the holes contained in it
            // this method assumes that the polygon is not self-intersecting
            // and that the holes are disjunct and completely inside the polygon
            List<Vector4> connectingLines;
            ConnectPolygonWContainedHoles(_polygon, _holes, out connectingLines);
            int nrCL = connectingLines.Count;
            if (nrCL < 2)
                return new List<List<Point3D>> { _polygon };

            // perform decomposition (no duplicates in the connecting lines)
            List<int> holes_toSplit = Enumerable.Range(0, nrH).ToList();
            List<bool> connectingLines_used = Enumerable.Repeat(false, nrCL).ToList();

            // find all SPLITTING PATHS
            List<List<Vector4>> splitting_paths = new List<List<Vector4>>();
            while (holes_toSplit.Count > 0)
            {
                // look for a connected path of connecting lines 
                // that STARTS at the polygon, goes THROUGH a not yet split hole, and ENDS at the polygon
                // or a hole that has been split already
                List<Vector4> splitting_path = new List<Vector4>();
                // START
                bool reached_other_end = false;
                for(int i = 0; i < nrCL; i++)
                {
                    if (connectingLines_used[i])
                        continue;
                    // start at the polygon or a hole that has already been split
                    if(connectingLines[i].X == -1 || !holes_toSplit.Contains((int) connectingLines[i].X))
                    {
                        splitting_path.Add(connectingLines[i]);
                        connectingLines_used[i] = true;

                        int split_hole_ind = (int)splitting_path[0].Z;
                        if (holes_toSplit.Contains(split_hole_ind))
                        {
                            holes_toSplit.Remove(split_hole_ind);
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }
                }

                // HOLES and END
                int nrSP = splitting_path.Count;
                if (nrSP == 0)
                    break;

                int maxNrIter = connectingLines.Count;
                int counter_iterations = 0;
                
                while (!reached_other_end && counter_iterations <= maxNrIter)
                {
                    counter_iterations++;
                    
                    for (int i = 0; i < nrCL; i++)
                    {
                        if (connectingLines_used[i])
                            continue;

                        if (connectingLines[i].X == splitting_path[nrSP - 1].Z && connectingLines[i].Y != splitting_path[nrSP - 1].W)
                            splitting_path.Add(connectingLines[i]);
                        else if (connectingLines[i].Z == splitting_path[nrSP - 1].Z && connectingLines[i].W != splitting_path[nrSP - 1].W)
                            splitting_path.Add(new Vector4(connectingLines[i].Z, connectingLines[i].W,
                                                           connectingLines[i].X, connectingLines[i].Y));
                        else
                            continue;

                        nrSP = splitting_path.Count;
                        connectingLines_used[i] = true;

                        int split_hole_ind = (int)splitting_path[nrSP - 1].Z;
                        if (holes_toSplit.Contains(split_hole_ind))
                        {
                            holes_toSplit.Remove(split_hole_ind);
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }
  
                }

                splitting_paths.Add(splitting_path);
            }
            int d = splitting_paths.Count;

            // perform splitting
            List<List<Point3D>> list_before_Split_polys = new List<List<Point3D>>();
            List<List<Vector2>> list_before_Split_inds = new List<List<Vector2>>();

            List<List<Point3D>> list_after_Split_polys = new List<List<Point3D>>();
            List<List<Vector2>> list_after_Split_inds = new List<List<Vector2>>();

            list_before_Split_polys.Add(_polygon);
            list_before_Split_inds.Add(GenerateDoubleIndices(-1, 0, n));

            for (int j = 0; j < d; j++)
            {
                int nrToSplit = list_before_Split_polys.Count;
                for (int k = 0; k < nrToSplit; k++)
                {
                    List<Point3D> polyA, polyB;
                    List<Vector2> originalIndsA, originalIndsB;
                    bool inputValid;
                    SplitPolygonWHolesAlongPath(list_before_Split_polys[k], list_before_Split_inds[k],
                                                splitting_paths[j], true, _holes,
                                                out polyA, out polyB, out originalIndsA, out originalIndsB, out inputValid);

                    if (polyA.Count > 2 && polyB.Count > 2)
                    {
                        // successful split
                        list_after_Split_polys.Add(polyA);
                        list_after_Split_inds.Add(originalIndsA);
                        list_after_Split_polys.Add(polyB);
                        list_after_Split_inds.Add(originalIndsB);
                    }
                    else
                    {
                        // no split
                        list_after_Split_polys.Add(list_before_Split_polys[k]);
                        list_after_Split_inds.Add(list_before_Split_inds[k]);
                    }
                }
                // swap lists
                list_before_Split_polys = new List<List<Point3D>>(list_after_Split_polys);
                list_before_Split_inds = new List<List<Vector2>>(list_after_Split_inds);
                list_after_Split_polys = new List<List<Point3D>>();
                list_after_Split_inds = new List<List<Vector2>>();
            }


            return list_before_Split_polys;
        }

        // assumes that the winding direction of the polygon and all holes is the same
        internal static void SplitPolygonWHolesAlongPath(List<Point3D> _polygon, List<Vector2> _polyIndices,                                                      
                                                        List<Vector4> _splitting_path_ind, bool _checkAdmissibility,
                                                        List<List<Point3D>> _holes,
                                                    out List<Point3D> _polyA, out List<Point3D> _polyB,
                                                    out List<Vector2> _originalIndsA, out List<Vector2> _originalIndsB,
                                                    out bool _inputValid)
        {
            _polyA = new List<Point3D>();
            _polyB = new List<Point3D>();
            _originalIndsA = new List<Vector2>();
            _originalIndsB = new List<Vector2>();
            _inputValid = false;

            if (_polygon == null || _polyIndices == null || _splitting_path_ind == null || _splitting_path_ind.Count < 1)
                return;

            int n = _polygon.Count;
            if (n != _polyIndices.Count)
                return;
            int nrH = _holes.Count;
            int nrSP = _splitting_path_ind.Count;

            // check validity of the given path

            List<Vector3> polygon_asV3 = CommonExtensions.ConvertPoints3DListToVector3List(_polygon);
            List<List<Vector3>> holes_asV3 = CommonExtensions.ConvertPoints3DListListToVector3ListList(_holes);

            for (int p = 0; p < nrSP; p++ )
            {
                Vector2 ind1 = new Vector2(_splitting_path_ind[p].X, _splitting_path_ind[p].Y);
                Vector2 ind2 = new Vector2(_splitting_path_ind[p].Z, _splitting_path_ind[p].W);
                
                // check if those are valid indices
                if(ind1.X != -1)
                {
                    if (ind1.X < 0 || ind1.X > nrH - 1)
                        return;
                    if (ind1.Y < 0 || ind1.Y > _holes[(int)ind1.X].Count - 1)
                        return;
                }
                else
                {
                    if (ind1.Y < 0 || ind1.Y > n - 1)
                        return;
                }
                if (ind2.X != -1)
                {
                    if (ind2.X < 0 || ind2.X > nrH - 1)
                        return;
                    if (ind2.Y < 0 || ind2.Y > _holes[(int)ind2.X].Count - 1)
                        return;
                }
                else
                {
                    if (ind2.Y < 0 || ind2.Y > n - 1)
                        return;
                }

                // check if the path segment is valid within the polygon and holes
                if(_checkAdmissibility)
                {
                    bool isAdmissible = false;
                    if (ind1.X == -1 && ind2.X == -1)
                        isAdmissible = DiagonalIsAdmissible(polygon_asV3, _polyIndices.IndexOf(ind1), _polyIndices.IndexOf(ind2));
                    else if (ind1.X == -1 && ind2.X != -1)
                        isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3, _polyIndices.IndexOf(ind1), (int)ind2.X, (int)ind2.Y);
                    else if (ind1.X != -1 && ind2.X == -1)
                        isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3, _polyIndices.IndexOf(ind2), (int)ind1.X, (int)ind1.Y);
                    else
                        isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3, (int)ind1.X, (int)ind1.Y, (int)ind2.X, (int)ind2.Y);

                    if (!isAdmissible)
                    {
                        _polyA = new List<Point3D>(_polygon);
                        _originalIndsA = new List<Vector2>(_polyIndices);
                        return;
                    }
                }
            }

            //perform actual split
            for (int p = 0; p < nrSP; p++)
            {
                // add subpolygons
                Vector2 split_ind1 = new Vector2(_splitting_path_ind[p].Z, _splitting_path_ind[p].W);
                Vector2 split_ind2 = new Vector2(_splitting_path_ind[(p + 1) % nrSP].X, _splitting_path_ind[(p + 1) % nrSP].Y);
                bool splittingOuterMost = _polyIndices.Contains(split_ind1);
                if (splittingOuterMost)
                {
                    if (!_polyIndices.Contains(split_ind2))
                        return;
                }
                else
                {
                    if (split_ind1.X != split_ind2.X)
                        return;
                }

                List<Point3D> toSplit_chain;
                List<Vector2> toSplit_chain_ind;                
                if (splittingOuterMost)
                {
                    toSplit_chain = new List<Point3D>(_polygon);
                    toSplit_chain_ind = new List<Vector2>(_polyIndices);
                }
                else
                {
                    toSplit_chain = new List<Point3D>(_holes[(int)split_ind1.X]);
                    toSplit_chain_ind = GenerateDoubleIndices((int)split_ind1.X, 0, toSplit_chain.Count);
                }

                int h = toSplit_chain.Count;
                int split_start_ind = toSplit_chain_ind.IndexOf(split_ind1);
                int split_end_ind = toSplit_chain_ind.IndexOf(split_ind2);

                List<Point3D> for_polyA = new List<Point3D>();
                List<Vector2> for_originalIndsA = new List<Vector2>();
                List<Point3D> for_polyB = new List<Point3D>();
                List<Vector2> for_originalIndsB = new List<Vector2>();

                int split_current_ind = split_start_ind;
                while(split_current_ind != split_end_ind)
                {
                    for_polyA.Add(toSplit_chain[split_current_ind]);
                    for_originalIndsA.Add(toSplit_chain_ind[split_current_ind]);
                    split_current_ind = (split_current_ind + 1) % h;
                }
                for_polyA.Add(toSplit_chain[split_end_ind]);
                for_originalIndsA.Add(toSplit_chain_ind[split_end_ind]);

                // subpolygon B
                split_current_ind = split_end_ind;
                while (split_current_ind != split_start_ind)
                {
                    for_polyB.Add(toSplit_chain[split_current_ind]);
                    for_originalIndsB.Add(toSplit_chain_ind[split_current_ind]);
                    split_current_ind = (split_current_ind + 1) % h;
                }
                for_polyB.Add(toSplit_chain[split_start_ind]);
                for_originalIndsB.Add(toSplit_chain_ind[split_start_ind]);

                if (!splittingOuterMost)
                {
                    _polyA.AddRange(for_polyA);
                    _originalIndsA.AddRange(for_originalIndsA);
                    _polyB.InsertRange(0, for_polyB);
                    _originalIndsB.InsertRange(0, for_originalIndsB);
                }
                else
                {
                    for_polyA.Reverse();
                    for_polyB.Reverse();
                    for_originalIndsA.Reverse();
                    for_originalIndsB.Reverse();

                    _polyA.AddRange(for_polyB);
                    _originalIndsA.AddRange(for_originalIndsB);
                    _polyB.InsertRange(0, for_polyA);
                    _originalIndsB.InsertRange(0, for_originalIndsA);
                }
                
            }

            _polyA.Reverse();
            _originalIndsA.Reverse();
            _polyB.Reverse();
            _originalIndsB.Reverse();
            _inputValid = true; 
        }

        #endregion

        #region MESH GENERATION: Decomposition of Simple Polygon in Monotone Polygons

        public static List<List<Point3D>> DecomposeInMonotonePolygons(List<Point3D> _polygon)
        {
            if (_polygon == null || _polygon.Count < 3)
                return new List<List<Point3D>>();

            List<Vector3> polygon_asV3 = CommonExtensions.ConvertPoints3DListToVector3List(_polygon);

            // order the vertices according to the X component
            int n = _polygon.Count;
            Vector3XComparer vec3Xcomp = new Vector3XComparer();
            SortedList<Vector3, int> vertices_ordered = new SortedList<Vector3, int>(vec3Xcomp);
            for (int i = 0; i < n; i++)
            {
                if (vertices_ordered.ContainsKey(_polygon[i].ToVector3()))
                    continue;

                try
                {
                    vertices_ordered.Add(_polygon[i].ToVector3(), i + 1);
                }
                catch (ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }

            // traverse the polygon in X-direction to determine the split diagonals
            // leave out the start and end points
            int m = vertices_ordered.Count;
            List<Vector2> splitIndices = new List<Vector2>();
            for(int j = 1; j < m - 1; j++)
            {
                Vector3 current_alongX = vertices_ordered.ElementAt(j).Key;
                int ind_current_alongX = vertices_ordered.ElementAt(j).Value - 1;

                Vector3 prev = _polygon[(n + ind_current_alongX - 1) % n].ToVector3();
                Vector3 next = _polygon[(ind_current_alongX + 1) % n].ToVector3();

                
                if (prev.X <= current_alongX.X && next.X <= current_alongX.X)
                {
                    // MERGE VERTEX -> split polygon along the current vertex and the NEXT one along the X-axis
                    Vector3 next_alongX;
                    int ind_next_alongX;
                    for (int c = 1; c < m - j; c++)
                    {
                        next_alongX = vertices_ordered.ElementAt(j + c).Key;
                        ind_next_alongX = vertices_ordered.ElementAt(j + c).Value - 1;
                        if (next_alongX.X > current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = DiagonalIsAdmissible(polygon_asV3, ind_current_alongX, ind_next_alongX);
                            if (isAdmissible)
                            {
                                splitIndices.Add(new Vector2(ind_current_alongX, ind_next_alongX));
                                break;
                            }
                        }
                    }                   
                }
                else if (prev.X >= current_alongX.X && next.X >= current_alongX.X)
                {
                    // SPLIT VERTEX -> split polygon along the current vertex and the PERVIOUS one along the X-axis
                    Vector3 prev_alongX;
                    int ind_prev_alongX;
                    for (int c = 1; c < j + 1; c++)
                    {
                        prev_alongX = vertices_ordered.ElementAt(j - c).Key;
                        ind_prev_alongX = vertices_ordered.ElementAt(j - c).Value - 1;
                        if (prev_alongX.X < current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = DiagonalIsAdmissible(polygon_asV3, ind_current_alongX, ind_prev_alongX);
                            if (isAdmissible)
                            {
                                splitIndices.Add(new Vector2(ind_current_alongX, ind_prev_alongX));
                                break;
                            }
                        }
                    }
                }

            }

            // split the polygon along the saved diagonals
            int d = splitIndices.Count;
            if (d == 0)
            {
                return new List<List<Point3D>> { _polygon };
            }

            // remove double split diagonal entries
            List<Vector2> splitInideces_optimized = new List<Vector2>();
            for(int a = 0; a < d; a++)
            {
                bool hasReversedDuplicate = false;
                for (int b = a + 1; b < d; b++)
                {
                    if (splitIndices[a].X == splitIndices[b].Y && splitIndices[a].Y == splitIndices[b].X)
                    {
                        hasReversedDuplicate = true;
                        break;
                    }                  
                }
                if (!hasReversedDuplicate)
                    splitInideces_optimized.Add(splitIndices[a]);
            }
            splitIndices = new List<Vector2>(splitInideces_optimized);
            d = splitIndices.Count;
            
            // perform the actual splitting of the polygon
            List<Point3D> poly = new List<Point3D>(_polygon);
            List<int> polyIndices = Enumerable.Range(0, n).ToList();

            List<List<Point3D>> list_before_Split_polys = new List<List<Point3D>>();
            list_before_Split_polys.Add(poly);
            List<List<int>> list_brefore_Split_inds = new List<List<int>>();
            list_brefore_Split_inds.Add(polyIndices);

            List<List<Point3D>> list_after_Split_polys = new List<List<Point3D>>();
            List<List<int>> list_after_Split_inds = new List<List<int>>();

            for (int j = 0; j < d; j++ )
            {
                int nrToSplit = list_before_Split_polys.Count;
                for(int k = 0; k < nrToSplit; k++)
                {
                    List<Point3D> polyA, polyB;
                    List<int> originalIndsA, originalIndsB;
                    SplitPolygonAlongDiagonal(list_before_Split_polys[k], list_brefore_Split_inds[k], 
                                              (int)splitIndices[j].X, (int)splitIndices[j].Y, true,
                                              out polyA, out polyB, out originalIndsA, out originalIndsB);

                    if (polyA.Count > 2 && polyB.Count > 2)
                    {
                        // successful split
                        list_after_Split_polys.Add(polyA);
                        list_after_Split_inds.Add(originalIndsA);
                        list_after_Split_polys.Add(polyB);
                        list_after_Split_inds.Add(originalIndsB);
                    }
                    else
                    {
                        // no split
                        list_after_Split_polys.Add(list_before_Split_polys[k]);
                        list_after_Split_inds.Add(list_brefore_Split_inds[k]);
                    }
                }
                // swap lists
                list_before_Split_polys = new List<List<Point3D>>(list_after_Split_polys);
                list_brefore_Split_inds = new List<List<int>>(list_after_Split_inds);
                list_after_Split_polys = new List<List<Point3D>>();
                list_after_Split_inds = new List<List<int>>();
            }


            return list_before_Split_polys;

        }

        private static void SplitPolygonAlongDiagonal(List<Point3D> _polygon, List<int> _polyIndices,
                                                      int _ind1, int _ind2, bool _checkAdmissibility,
                                                  out List<Point3D> _polyA, out List<Point3D> _polyB,
                                                  out List<int> _originalIndsA, out List<int> _originalIndsB)
        {
            _polyA = new List<Point3D>();
            _polyB = new List<Point3D>();
            _originalIndsA = new List<int>();
            _originalIndsB = new List<int>();

            if (_polygon == null || _polyIndices == null)
                return;

            int n = _polygon.Count;
            int ind1 = Math.Min(_ind1, _ind2);
            int ind2 = Math.Max(_ind1, _ind2);
            int test1 = _polyIndices.IndexOf(ind1);
            int test2 = _polyIndices.IndexOf(ind2);

            if (n < 4 || n != _polyIndices.Count || test1 == -1 || test2 == -1 || 
                ind1 < 0 || ind2 < 0 || ind1 == ind2 || ind1 + 1 == ind2)
            {
                _polyA = new List<Point3D>(_polygon);
                _originalIndsA = new List<int>(_polyIndices);
                return;
            }
            if (_checkAdmissibility)
            {
                List<Vector3> polygon_asV3 = CommonExtensions.ConvertPoints3DListToVector3List(_polygon);
                bool isAdmissible = DiagonalIsAdmissible(polygon_asV3, test1, test2);
                if (!isAdmissible)
                {
                    _polyA = new List<Point3D>(_polygon);
                    _originalIndsA = new List<int>(_polyIndices);
                    return;
                }
            }

            // perform actual split
            for(int i = 0; i < n; i++)
            {
                int index = _polyIndices[i];
                if (index <= ind1 || index >= ind2)
                {
                    _polyA.Add(_polygon[i]);
                    _originalIndsA.Add(index);
                }
                if (index >= ind1 && index <= ind2)
                {
                    _polyB.Add(_polygon[i]);
                    _originalIndsB.Add(index);
                }
            }
        }

        #endregion

        #region MESH GENERATION: Monotone Polygon Triangulation

        public static MeshGeometry3D PolygonFillMonotone(List<Vector3> _polygon)
        {
            if (_polygon == null || _polygon.Count < 3)
                return null;

            List<Vector3> pos = new List<Vector3>();
            List<Vector3> norm = new List<Vector3>();
            List<int> ind = new List<int>();

            // extract info about the polygon
            bool polygon_is_valid;
            bool isCW = CalculateIfPolygonClockWise(_polygon, CommonExtensions.GENERAL_CALC_TOLERANCE, out polygon_is_valid);
            if (!polygon_is_valid)
                return null;

            Vector3 poly_normal = GetPolygonNormalNewell(_polygon);

            // order the vertices according to the X component            
            int n = _polygon.Count;
            Vector3XComparer vec3Xcomp = new Vector3XComparer();
            SortedList<Vector3, int> vertices_ordered = new SortedList<Vector3, int>(vec3Xcomp);
            for (int i = 0; i < n; i++)
            {
                if (vertices_ordered.ContainsKey(_polygon[i]))
                    continue;

                try
                {
                    vertices_ordered.Add(_polygon[i], i + 1);
                }
                catch(ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }
            n = vertices_ordered.Count;

            // check if we actually have a triangle:
            if (n < 3)
                return null;
            else if (n == 3)
            {
                List<Vector3> tri = new List<Vector3>(vertices_ordered.Keys);
                
                bool tri_is_valid;
                bool tri_isCW = CalculateIfPolygonClockWise(tri, CommonExtensions.GENERAL_CALC_TOLERANCE, out tri_is_valid);
                if (!tri_is_valid)
                    return null;

                if (isCW != tri_isCW)
                    tri = ReversePolygon(tri);

                return PolygonFill(tri, true);
            }

            // and determine to which chain (upper = 1, lower = -1, both = 0) they belong            
            List<int> vertices_in_chain = Enumerable.Repeat(0, _polygon.Count).ToList();
            
            int chain_startInd = Math.Min(vertices_ordered.ElementAt(0).Value, vertices_ordered.ElementAt(n - 1).Value) - 1;
            int chain_endInd = Math.Max(vertices_ordered.ElementAt(0).Value, vertices_ordered.ElementAt(n - 1).Value) - 1;
            for (int i = 0; i < _polygon.Count; i++)
            {
                if (i < chain_startInd || i > chain_endInd)
                    vertices_in_chain[i] = 1;
                else if (chain_startInd < i && i < chain_endInd)
                    vertices_in_chain[i] = -1;
            }

            // ALGORITHM
            Stack<Vector3> to_process = new Stack<Vector3>();
            // 1. push first 2 vertices onto stack
            to_process.Push(vertices_ordered.ElementAt(0).Key);
            to_process.Push(vertices_ordered.ElementAt(1).Key);
            
            // 2. check the vertices moving along the X axis
            for (int i = 2; i < n; i ++ )
            {
                if (to_process.Count < 1)
                    break;

                Vector3 current = vertices_ordered.ElementAt(i).Key;
                int current_Ind = vertices_ordered.ElementAt(i).Value - 1;
                Vector3 topOfStack = to_process.Peek();
                int topOfStack_Ind = vertices_ordered[topOfStack] - 1;
                
                if (vertices_in_chain[current_Ind] == vertices_in_chain[topOfStack_Ind])
                {
                    // 3A. if on the SAME chain: add diagonals as long as they are admissible
                    while(to_process.Count > 1)
                    {
                        Vector3 last_on_stack = to_process.Pop();
                        int last_on_stack_Ind = vertices_ordered[last_on_stack] - 1;
                        Vector3 before_last_on_stack = to_process.Peek();
                        int before_last_on_stack_Ind = vertices_ordered[before_last_on_stack] - 1;
                        if (DiagonalIsAdmissible(_polygon, current_Ind, before_last_on_stack_Ind))
                        {
                            // 4AA. add triangle
                            bool triangle_is_valid;
                            bool tr_isCW = CalculateIfPolygonClockWise(new List<Vector3> {current, last_on_stack, before_last_on_stack },
                                CommonExtensions.GENERAL_CALC_TOLERANCE, out triangle_is_valid);

                            if (tr_isCW == isCW)
                            {
                                pos.Add(current);
                                pos.Add(last_on_stack);
                                pos.Add(before_last_on_stack);
                            }
                            else
                            {
                                pos.Add(current);
                                pos.Add(before_last_on_stack);
                                pos.Add(last_on_stack);
                            }

                            norm.Add(poly_normal);
                            norm.Add(poly_normal);
                            norm.Add(poly_normal);

                            int nrInd = ind.Count;
                            ind.Add(nrInd);
                            ind.Add(nrInd + 1);
                            ind.Add(nrInd + 2);
                        }
                        else
                        {
                            // 4AB. push the top of the stack back on
                            to_process.Push(last_on_stack);
                            break;
                        }
                    }
                    // 5. put current on the stack:
                    to_process.Push(current);
                }
                else
                {
                    // 3B. if on DIFFERENT chains:
                    // pop all vertices from the stack and add the corresponding diagonals
                    Vector3 top_of_stack = to_process.Peek();
                    while (to_process.Count > 1)
                    {
                        Vector3 last_on_stack = to_process.Pop();
                        Vector3 before_last_on_stack = to_process.Peek();
                        
                        // 4. add triangle   
                        bool triangle_is_valid;
                        bool tr_isCW = CalculateIfPolygonClockWise(new List<Vector3> { current, last_on_stack, before_last_on_stack },
                            CommonExtensions.GENERAL_CALC_TOLERANCE, out triangle_is_valid);

                        if (tr_isCW == isCW)
                        {
                            pos.Add(current);
                            pos.Add(last_on_stack);
                            pos.Add(before_last_on_stack);
                        }
                        else
                        {
                            pos.Add(current);
                            pos.Add(before_last_on_stack);
                            pos.Add(last_on_stack);
                        }

                        norm.Add(poly_normal);
                        norm.Add(poly_normal);
                        norm.Add(poly_normal);

                        int nrInd = ind.Count;
                        ind.Add(nrInd);
                        ind.Add(nrInd + 1);
                        ind.Add(nrInd + 2);                        
                    }
                    // 4B. save to stack: the previous top of the stack and the current point
                    to_process.Clear();
                    to_process.Push(top_of_stack);
                    to_process.Push(current);
                }
            }


            return new MeshGeometry3D()
            {
                Positions = pos.ToArray(),
                Normals = norm.ToArray(),
                Indices = ind.ToArray()
            };
        }

        


        

        

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================ MESH UTILITIES ============================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MESH UTILS: Triangles
        private static bool TriangleIsWellDefined(Vector3 _p0, Vector3 _p1, Vector3 _p2, out Vector3 normal, float _tolerance = 0.0001f)
        {
            Vector3 v1 = _p1 - _p0;
            v1.Normalize();
            Vector3 v2 = _p2 - _p0;
            v2.Normalize();
            double dp = Vector3.Dot(v1, v2);
            if (v1.Length() < _tolerance || v2.Length() < _tolerance || Math.Abs(dp) > (1f - _tolerance))
            {
                normal = Vector3.Zero;
                return false;
            }
            else
            {
                normal = Vector3.Cross(v1, v2);
                normal.Normalize();
                return true;
            }
        }


        public static bool PointsContainValidTriangle(List<Vector3> _points, float _tolerance = 0.0001f)
        {
            if (_points == null) return false;
            if (_points.Count < 3) return false;

            int nrP = _points.Count;

            for(int c1 = 0; c1 < nrP - 2; c1++)
            {
                for(int c2 = c1 + 1; c2 < nrP - 1; c2++)
                {
                    for(int c3 = c2 + 1; c3 < nrP; c3++)
                    {
                        Vector3 norm;
                        bool found_valid_triangle = MeshesCustom.TriangleIsWellDefined(_points[c1], _points[c2], _points[c3], out norm, _tolerance);
                        if (found_valid_triangle)
                            return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Polygon Info: Double Indices (used in the Decomposition into Simple Polygons)

        public static List<Vector2> GenerateDoubleIndices(int _firstIndex, int _secondIndexStart, int _nrIndices)
        {
            if (_nrIndices < 1)
                return new List<Vector2>();

            List<Vector2> indices = new List<Vector2>();
            for(int i = 0; i < _nrIndices; i++)
            {
                indices.Add(new Vector2(_firstIndex, _secondIndexStart + i));
            }

            return indices;
        }

        #endregion

        #region Polygon Info: List of Polygons -> big Polygon + contained Holes

        public static void ToPolygonWithHoles(List<List<Point3D>> _allPolys, Orientation _plane,
                                            out int indBiggestP, out List<Point3D> bigPoly, out List<List<Point3D>> containedHoles)
        {
            // init
            bigPoly = null;
            containedHoles = null;
            indBiggestP = -1;

            if (_allPolys == null)
                return;

            // determine which of the input polygons is the outermost one
            // take the one with the largest area (in the input plane)
            indBiggestP = FindOuterMostPolygon(_allPolys, _plane);
            int n = _allPolys.Count;

            if (n > 1 && indBiggestP > -1 && indBiggestP < n)
            {
                bigPoly = _allPolys[indBiggestP];

                // extract the holes
                containedHoles = new List<List<Point3D>>();
                for (int i = 0; i < n; i++)
                {
                    if (i == indBiggestP)
                        continue;

                   containedHoles.Add(_allPolys[i]);
                }
                if (containedHoles.Count < 1)
                    containedHoles = null;
            }
            else if (n == 1)
            {
                bigPoly = _allPolys[0];
            }
        }

        public static int FindOuterMostPolygon(List<List<Point3D>> _allPolys, Orientation _plane)
        {
            if (_allPolys == null)
                return -1;

            int n = _allPolys.Count;
            if (n > 1)
            {
                // determine which of the input polygons is the outermost one
                // take the one with the largest area (in the input plane)
                double maxArea = 0.0;
                int indBiggestP = -1;

                for (int i = 0; i < n; i++)
                {
                    List<Point3D> coords = _allPolys[i];
                    if (coords == null)
                        continue;

                    double area = Math.Abs(MeshesCustom.CalculatePolygonSignedArea(coords, _plane));
                    if (area > maxArea)
                    {
                        maxArea = area;
                        indBiggestP = i;
                    }
                }

                return indBiggestP;

            }
            else if (n == 1)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }


        #endregion

        #region MESH UTILS: Polygon reverse

        public static List<Point3D> ReversePolygon(List<Point3D> _original)
        {
            if (_original == null)
                return null;

            int n = _original.Count;
            List<Point3D> reversed = new List<Point3D>();
            reversed.Add(_original[0]);
            for(int i = n - 1; i > 0; i--)
            {
                reversed.Add(_original[i]);
            }

            return reversed;
        }

        public static List<Vector3> ReversePolygon(List<Vector3> _original)
        {
            if (_original == null)
                return null;

            int n = _original.Count;
            List<Vector3> reversed = new List<Vector3>();
            reversed.Add(_original[0]);
            for (int i = n - 1; i > 0; i--)
            {
                reversed.Add(_original[i]);
            }

            return reversed;
        }

        #endregion

        #region MESH UTILS: Polygon Info (Area, Normal, CW / CCW, Orientation)

        public static double CalculatePolygonSignedArea(List<Vector3> _polygon, Orientation _plane)
        {
            if (_polygon == null || _polygon.Count < 3)
                return 0.0;

            int n = _polygon.Count;
            double area = 0;
            
            for (int i = 0; i < n; i++)
            {
                if (_plane == Orientation.XZ)
                    area += (_polygon[(i + 1) % n].Z + _polygon[i].Z) * (_polygon[(i + 1) % n].X - _polygon[i].X);
                else if (_plane == Orientation.YZ)
                    area += (_polygon[(i + 1) % n].Y + _polygon[i].Y) * (_polygon[(i + 1) % n].Z - _polygon[i].Z);
                else
                    area += (_polygon[(i + 1) % n].Y + _polygon[i].Y) * (_polygon[(i + 1) % n].X - _polygon[i].X);

            }
            
            return (area * 0.5);
        }

        public static double CalculatePolygonSignedArea(List<Point3D> _polygon, Orientation _plane)
        {
            if (_polygon == null || _polygon.Count < 3)
                return 0.0;

            int n = _polygon.Count;
            double area = 0;

            for (int i = 0; i < n; i++)
            {
                if (_plane == Orientation.XZ)
                    area += (_polygon[(i + 1) % n].Z + _polygon[i].Z) * (_polygon[(i + 1) % n].X - _polygon[i].X);
                else if (_plane == Orientation.YZ)
                    area += (_polygon[(i + 1) % n].Y + _polygon[i].Y) * (_polygon[(i + 1) % n].Z - _polygon[i].Z);
                else
                    area += (_polygon[(i + 1) % n].Y + _polygon[i].Y) * (_polygon[(i + 1) % n].X - _polygon[i].X);

            }

            return (area * 0.5);
        }


        public static bool CalculateIfPolygonClockWise(List<Vector3> _polygon, double _tolerance, out bool polygon_is_valid)
        {
            if (_polygon == null || _polygon.Count < 3)
            {
                polygon_is_valid = false;
                return false;
            }

            int n = _polygon.Count;

            // projection onto XZ - plane
            double area = CalculatePolygonSignedArea(_polygon, Orientation.XZ);
            if (Math.Abs(area) > _tolerance)
            {
                polygon_is_valid = true;
                return (area > 0);
            }

            // projection onto XY - plane
            area = CalculatePolygonSignedArea(_polygon, Orientation.XY);
            if (Math.Abs(area) > _tolerance)
            {
                polygon_is_valid = true;
                return (area > 0);
            }

            // projection onto YZ - plane
            area = CalculatePolygonSignedArea(_polygon, Orientation.YZ);
            if (Math.Abs(area) > _tolerance)
                polygon_is_valid = true;
            else
                polygon_is_valid = false;

            return (area > 0);
        }

        public static double CalculatePolygonLargestSignedProjectedArea(List<Vector3> _polygon)
        {
            double areaXZ = CalculatePolygonSignedArea(_polygon, Orientation.XZ);
            double areaXY = CalculatePolygonSignedArea(_polygon, Orientation.XY);
            double areaYZ = CalculatePolygonSignedArea(_polygon, Orientation.YZ);

            double areaXZ_m = Math.Abs(areaXZ);
            double areaXY_m = Math.Abs(areaXY);
            double areaYZ_m = Math.Abs(areaYZ);

            if (areaXZ_m > areaXY_m && areaXZ_m > areaYZ_m)
                return areaXZ;
            else if (areaXY_m > areaXZ_m && areaXY_m > areaYZ_m)
                return areaXY;
            else
                return areaYZ;
        }

        public static Orientation CalculatePolygonOrientation(List<Vector3> _polygon)
        {
            double areaXZ = CalculatePolygonSignedArea(_polygon, Orientation.XZ);
            double areaXY = CalculatePolygonSignedArea(_polygon, Orientation.XY);
            double areaYZ = CalculatePolygonSignedArea(_polygon, Orientation.YZ);

            double areaXZ_m = Math.Abs(areaXZ);
            double areaXY_m = Math.Abs(areaXY);
            double areaYZ_m = Math.Abs(areaYZ);

            if (areaXZ_m > areaXY_m && areaXZ_m > areaYZ_m)
                return Orientation.XZ;
            else if (areaXY_m > areaXZ_m && areaXY_m > areaYZ_m)
                return Orientation.XY;
            else
                return Orientation.YZ;
        }

        public static List<Vector3> ReOrientPolygon(List<Vector3> _polygon_in, Orientation _to)
        {
            if (_polygon_in == null) return null;
            if (_polygon_in.Count == 0) return new List<Vector3>();

            Orientation from = MeshesCustom.CalculatePolygonOrientation(_polygon_in);
            if (from == _to)
                return new List<Vector3>(_polygon_in);

            List<Vector3> poly_reoriented = new List<Vector3>();

            if ((from == Orientation.XZ && _to == Orientation.XY) ||
                (from == Orientation.XY && _to == Orientation.XZ))
            {
                foreach (Vector3 v in _polygon_in)
                {
                    poly_reoriented.Add(new Vector3(v.X, v.Z, v.Y));
                }                
            }
            else if ((from == Orientation.XZ && _to == Orientation.YZ) ||
                     (from == Orientation.YZ && _to == Orientation.XZ))
            {
                foreach (Vector3 v in _polygon_in)
                {
                    poly_reoriented.Add(new Vector3(v.Y, v.X, v.Z));
                } 
            }
            else if ((from == Orientation.XY && _to == Orientation.YZ) ||
                     (from == Orientation.YZ && _to == Orientation.XY))
            {
                foreach (Vector3 v in _polygon_in)
                {
                    poly_reoriented.Add(new Vector3(v.Z, v.Y, v.X));
                }
            }

            return poly_reoriented;
        }

        public static List<Point3D> ReOrientPolygon(List<Point3D> _polygon_in, Orientation _to)
        {
            if (_polygon_in == null) return null;
            if (_polygon_in.Count == 0) return new List<Point3D>();

            Orientation from = MeshesCustom.CalculatePolygonOrientation(CommonExtensions.ConvertPoints3DListToVector3List(_polygon_in));
            if (from == _to)
                return new List<Point3D>(_polygon_in);

            List<Point3D> poly_reoriented = new List<Point3D>();

            if ((from == Orientation.XZ && _to == Orientation.XY) ||
                (from == Orientation.XY && _to == Orientation.XZ))
            {
                foreach (Point3D v in _polygon_in)
                {
                    poly_reoriented.Add(new Point3D(v.X, v.Z, v.Y));
                }
            }
            else if ((from == Orientation.XZ && _to == Orientation.YZ) ||
                     (from == Orientation.YZ && _to == Orientation.XZ))
            {
                foreach (Point3D v in _polygon_in)
                {
                    poly_reoriented.Add(new Point3D(v.Y, v.X, v.Z));
                }
            }
            else if ((from == Orientation.XY && _to == Orientation.YZ) ||
                     (from == Orientation.YZ && _to == Orientation.XY))
            {
                foreach (Point3D v in _polygon_in)
                {
                    poly_reoriented.Add(new Point3D(v.Z, v.Y, v.X));
                }
            }

            return poly_reoriented;
        }

        public static void ReOrientMesh(ref MeshGeometry3D _mesh_in, Orientation _from, Orientation _to)
        {
            if (_mesh_in == null)
                return;

            Vector3[] pos = _mesh_in.Positions;
            Vector3[] norm = _mesh_in.Normals;
            Vector3[] tang = _mesh_in.Tangents;
            Vector3[] btang = _mesh_in.BiTangents;

            Vector3[] pos_rot = null;
            Vector3[] norm_rot = null;
            Vector3[] tang_rot = null;
            Vector3[] btang_rot = null;

            if ((_from == Orientation.XZ && _to == Orientation.XY) ||
                (_from == Orientation.XY && _to == Orientation.XZ))
            {
                if (pos != null)
                    pos_rot = pos.Select(p => new Vector3(p.X, p.Z, p.Y)).ToArray();

                if (norm != null)
                    norm_rot = norm.Select(p => new Vector3(p.X, p.Z, p.Y)).ToArray();

                if (tang != null)
                    tang_rot = tang.Select(p => new Vector3(p.X, p.Z, p.Y)).ToArray();

                if (btang != null)
                    btang_rot = btang.Select(p => new Vector3(p.X, p.Z, p.Y)).ToArray();
            }
            else if ((_from == Orientation.XZ && _to == Orientation.YZ) ||
                     (_from == Orientation.YZ && _to == Orientation.XZ))
            {
                if (pos != null)
                    pos_rot = pos.Select(p => new Vector3(p.Y, p.X, p.Z)).ToArray();

                if (norm != null)
                    norm_rot = norm.Select(p => new Vector3(p.Y, p.X, p.Z)).ToArray();

                if (tang != null)
                    tang_rot = tang.Select(p => new Vector3(p.Y, p.X, p.Z)).ToArray();

                if (btang != null)
                    btang_rot = btang.Select(p => new Vector3(p.Y, p.X, p.Z)).ToArray();
            }
            else if ((_from == Orientation.XY && _to == Orientation.YZ) ||
                     (_from == Orientation.YZ && _to == Orientation.XY))
            {
                if (pos != null)
                    pos_rot = pos.Select(p => new Vector3(p.Z, p.Y, p.X)).ToArray();

                if (norm != null)
                    norm_rot = norm.Select(p => new Vector3(p.Z, p.Y, p.X)).ToArray();

                if (tang != null)
                    tang_rot = tang.Select(p => new Vector3(p.Z, p.Y, p.X)).ToArray();

                if (btang != null)
                    btang_rot = btang.Select(p => new Vector3(p.Z, p.Y, p.X)).ToArray();
            }

            _mesh_in.Positions = pos_rot;
            _mesh_in.Normals = norm_rot;
            _mesh_in.Tangents = tang_rot;
            _mesh_in.BiTangents = btang_rot;

        }

        public static double CalculateAreaOfPolygonWHoles(List<Point3D> _polygon, List<List<Point3D>> _holes)
        {
            double area = 0.0;
            if (_polygon == null)
                return area;

            area = Math.Abs(MeshesCustom.CalculatePolygonLargestSignedProjectedArea(CommonExtensions.ConvertPoints3DListToVector3List(_polygon)));
            if (_holes != null)
            {
                foreach (List<Point3D> hole in _holes)
                {
                    area -= Math.Abs(MeshesCustom.CalculatePolygonLargestSignedProjectedArea(CommonExtensions.ConvertPoints3DListToVector3List(hole)));
                }
            }

            return area;
        }

        public static bool CalculateIfPolygonClockWise(List<Point3D> _polygon, double _tolerance, out bool polygon_is_valid)
        {
            if (_polygon == null || _polygon.Count < 3)
            {
                polygon_is_valid = false;
                return false;
            }

            List<Vector3> poly_V3 = CommonExtensions.ConvertPoints3DListToVector3List(_polygon);
            return CalculateIfPolygonClockWise(poly_V3, _tolerance, out polygon_is_valid);
        }

        public static Vector3 GetPolygonNormalNewell(List<Vector3> _polygon)
        {
            if (_polygon == null)
                return Vector3.Zero;

            int n = _polygon.Count;
            if (n < 3)
                return Vector3.Zero;

            Vector3 normal = Vector3.Zero;
            for (int i = 0; i < n; i++)
            {
                //Nx += (Vny - V(n+1)y) * (Vnz + V(n+1)z);
                //Ny += (Vnz - V(n+1)z) * (Vnx + V(n+1)x);
                //Nz += (Vnx - V(n+1)x) * (Vny + V(n+1)y);

                normal.X -= (float)((_polygon[i].Z - _polygon[(i + 1) % n].Z) *
                                    (_polygon[i].Y + _polygon[(i + 1) % n].Y));
                normal.Y -= (float)((_polygon[i].X - _polygon[(i + 1) % n].X) *
                                    (_polygon[i].Z + _polygon[(i + 1) % n].Z));
                normal.Z -= (float)((_polygon[i].Y - _polygon[(i + 1) % n].Y) *
                                    (_polygon[i].X + _polygon[(i + 1) % n].X));
            }

            normal.Normalize();

            //if (_isCW)
            //    return normal;
            //else
            //    return -normal;

            return normal;
        }
        #endregion

        #region MESH UTILS: Polygon Info (Perimeter, Pivot, major Orientation)

        public static double CalculatePolygonPerimeter(List<Vector3> _polygon)
        {
            double perimeter = 0.0;
            if (_polygon == null)
                return perimeter;

            int n = _polygon.Count;
            for (int i = 0; i < n; i++ )
            {
                perimeter += Vector3.Distance(_polygon[i], _polygon[(i + 1) % n]);
            }

            return perimeter;
        }

        public static double CalculatePolygonPerimeter(List<Point3D> _polygon)
        {
            double perimeter = 0.0;
            if (_polygon == null)
                return perimeter;

            int n = _polygon.Count;
            for (int i = 0; i < n; i++)
            {
                perimeter += Vector3.Distance(_polygon[i].ToVector3(), _polygon[(i + 1) % n].ToVector3());
            }

            return perimeter;
        }

        public static double CalculateMultiPolygonPerimeter(List<List<Vector3>> _polygons)
        {
            double perimeter = 0.0;
            if (_polygons == null)
                return perimeter;

            foreach(List<Vector3> poly in _polygons)
            {
                perimeter += CalculatePolygonPerimeter(poly);
            }

            return perimeter;
        }

        public static double CalculateMultiPolygonPerimeter(List<List<Point3D>> _polygons)
        {
            double perimeter = 0.0;
            if (_polygons == null)
                return perimeter;

            foreach (List<Point3D> poly in _polygons)
            {
                perimeter += CalculatePolygonPerimeter(poly);
            }

            return perimeter;
        }


        public static Vector3 GetPolygonPivot(List<Vector3> _polygon)
        {
            Vector3 pivot = Vector3.Zero;
            if (_polygon == null)
                return pivot;

            int n = _polygon.Count;
            for (int i = 0; i < n; i++)
            {
                pivot += _polygon[i];
            }
            pivot /= n;

            return pivot;
        }

        public static Vector3 GetPolygonPivot(List<Point3D> _polygon)
        {
            Vector3 pivot = Vector3.Zero;
            if (_polygon == null)
                return pivot;

            int n = _polygon.Count;
            for (int i = 0; i < n; i++)
            {
                pivot += _polygon[i].ToVector3();
            }
            pivot /= n;

            return pivot;
        }

        public static Vector3 GetMultiPolygonPivot(List<List<Point3D>> _polygons)
        {
            Vector3 pivot = Vector3.Zero;
            if (_polygons == null)
                return pivot;

            int nrP = 0;
            int n = _polygons.Count;           
            for (int i = 0; i < n; i++)
            {
                if (_polygons[i] == null)
                    continue;
                
                int m = _polygons[i].Count;
                nrP += m;
                for (int j = 0; j < m; j++ )
                {
                    pivot += _polygons[i][j].ToVector3();
                }                    
            }
            pivot /= nrP;

            return pivot;
        }

        public static Vector3 GetUnitMajorOrientation(List<Point3D> _polygon)
        {
            if (_polygon == null || _polygon.Count == 0) return Vector3.Zero;

            // determine the longest side of the outer polygon
            int ind_start = -1;
            int nrP = _polygon.Count;
            double len_max = 0.0;
            for (int i = 0; i < nrP; i++)
            {
                double len = Vector3.Distance(_polygon[i].ToVector3(), _polygon[(i + 1) % nrP].ToVector3());
                if (len > len_max)
                {
                    len_max = len;
                    ind_start = i;
                }
            }

            // calculate the unit vector of the longest side
            Vector3 q0 = _polygon[ind_start].ToVector3();
            Vector3 q1 = _polygon[(ind_start + 1) % nrP].ToVector3();
            Vector3 unit_v = q1 - q0;
            unit_v.Normalize();

            return unit_v;
        }

        public static Vector3 GetHorizontalAxisOf(List<Vector3> _polygon)
        {
            if (_polygon == null || _polygon.Count == 0) return Vector3.Zero;

            int nrP = _polygon.Count;
            Vector3 best_fit = Vector3.UnitX;
            double min_dotP = 1.0;
            for(int i = 0; i < nrP; i++)
            {
                Vector3 side = _polygon[(i + 1) % nrP] - _polygon[i];
                if (side.Length() < CommonExtensions.LINEDISTCALC_TOLERANCE * 10) continue;

                side.Normalize();
                double dotP = Math.Abs(Vector3.Dot(side, Vector3.UnitY));
                if (dotP < min_dotP)
                {
                    min_dotP = dotP;
                    best_fit = side;
                }
            }

            return best_fit;
        }

        #endregion

        #region MESH UTILS: Polygon Info (Admissible Diagonals, Hit-Test for Points, Inside Test, Convex Test)

        private static bool DiagonalIsAdmissible(List<Vector3> _polygon, int _startInd, int _endInd)
        {
            if (_polygon == null || _polygon.Count < 3)
                return false;

            // index out of bounds
            int n = _polygon.Count;
            if (_startInd < 0 || _startInd > n - 1 || _endInd < 0 || _startInd > n - 1)
                return false;

            int startInd = Math.Min(_startInd, _endInd);
            int endInd = Math.Max(_startInd, _endInd);

            // consecutive indices -> not a diagonal
            if (startInd == (endInd - 1) % n)
                return false;

            // test for intersections
            Vector3 d1 = _polygon[startInd];
            Vector3 d2 = _polygon[endInd];
            bool intersects_polygon = false;
            for (int i = 0; i < n; i++)
            {
                if (i == startInd || i == endInd)
                    continue;
                if ((i + 1) % n == startInd || (i + 1) % n == endInd)
                    continue;

                // exclude points at another index that coincide with the start or end points (double points)
                float dist1 = DistV3Simple(_polygon[i], d1);
                float dist2 = DistV3Simple(_polygon[i], d2);
                float dist3 = DistV3Simple(_polygon[(i + 1) % n], d1);
                float dist4 = DistV3Simple(_polygon[(i + 1) % n], d2);
                if (dist1 < CommonExtensions.GENERAL_CALC_TOLERANCE || dist2 < CommonExtensions.GENERAL_CALC_TOLERANCE ||
                    dist3 < CommonExtensions.GENERAL_CALC_TOLERANCE || dist4 < CommonExtensions.GENERAL_CALC_TOLERANCE)
                {
                    continue;
                }

                Vector3 _colPos;
                intersects_polygon = CommonExtensions.LineWLineCollision3D_InclAtEnds(d1, d2, _polygon[i], _polygon[(i + 1) % n],
                                                CommonExtensions.GENERAL_CALC_TOLERANCE, out _colPos);

                if (intersects_polygon)
                    break;
            }

            if (intersects_polygon)
                return false;

            // if no intersection, check if the diagonal is inside or outside of the polygon
            // if inside  -> winding direction of both subpolygons the same as that of the big polygon
            // if outside -> winding directions of the subpolygons differ from each other

            List<Vector3> subpoly1 = new List<Vector3>();
            List<Vector3> subpoly2 = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                if (i <= startInd || i >= endInd)
                    subpoly1.Add(_polygon[i]);
                if (startInd <= i && i <= endInd)
                    subpoly2.Add(_polygon[i]);
            }

            bool subpoly1_is_valid, subpoly2_is_valid;
            bool subpoly1_cw = CalculateIfPolygonClockWise(subpoly1, CommonExtensions.GENERAL_CALC_TOLERANCE, out subpoly1_is_valid);
            bool subpoly2_cw = CalculateIfPolygonClockWise(subpoly2, CommonExtensions.GENERAL_CALC_TOLERANCE, out subpoly2_is_valid);

            if (!subpoly1_is_valid || !subpoly2_is_valid)
                return false;

            return (subpoly1_cw == subpoly2_cw);
        }

        // source: http://conceptual-misfire.awardspace.com/point_in_polygon.htm
        // for polygons in the XZ- Plane (works with areas)
        public static bool PointIsInsidePolygonXZ(List<Vector3> _polygon, Vector3 _p)
        {
            if (_polygon == null || _polygon.Count < 3)
                return false;

            Vector3 p1, p2;
            bool isInside = false;

            int n = _polygon.Count;
            Vector3 oldPoint = _polygon[n - 1];

            for (int i = 0; i < n; i++)
            {
                // my code: START
                // check for coinciding points first
                if (Math.Abs(oldPoint.X - _p.X) <= CommonExtensions.GENERAL_CALC_TOLERANCE &&
                    Math.Abs(oldPoint.Z - _p.Z) <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                {
                    return true;
                }
                // my code: END

                Vector3 newPoint = _polygon[i];
                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.X < _p.X) == (_p.X <= oldPoint.X) &&
                    (_p.Z - p1.Z)*(p2.X - p1.X) < (p2.Z - p1.Z)*(_p.X - p1.X))
                {
                    isInside = !isInside;
                }

                oldPoint = newPoint;
            }

            return isInside;
        }

        // method assumes that the polygons are in the same plane
        // it only checks the vertices of the polygons ->if no vertex is not contained => no containment
        public static bool PolygonIsContainedInPolygon(List<Vector3> _polygon1, List<Vector3> _polygon2, bool _fully)
        {            
            if (_polygon1 == null || _polygon2 == null) return false;
            if (_polygon1.Count < 3 || _polygon2.Count < 3) return false;

            // since the test only works for polygons that are more or less in the XZ plane
            // we might need to re-orient
            Orientation p1_or = MeshesCustom.CalculatePolygonOrientation(_polygon1);
            Orientation p2_or = MeshesCustom.CalculatePolygonOrientation(_polygon2);
            if (p1_or != p2_or) return false;

            List<Vector3> polygon1_xz = _polygon1;
            List<Vector3> polygon2_xz = _polygon2;
            if (!(p1_or == Orientation.XZ))
            {
                polygon1_xz = MeshesCustom.ReOrientPolygon(_polygon1, Orientation.XZ);
                polygon2_xz = MeshesCustom.ReOrientPolygon(_polygon2, Orientation.XZ);
            }

            for (int i = 0; i < polygon2_xz.Count; i++)
            {
                bool inside = MeshesCustom.PointIsInsidePolygonXZ(polygon1_xz, polygon2_xz[i]);
                if (_fully && !inside)
                    return false;
                else if (!_fully && inside)
                    return true;
            }
            if (_fully)
                return true;
            else
                return false;
            
        }       

        // works with the projection of the polygon onto the XZ-plane
        private static bool PolygonIsConvexXZ(List<Point3D> _polygon)
        {
            if (_polygon == null)
                return false;

            int n = _polygon.Count;
            if (n < 3)
                return false;

            Vector3 v1 = _polygon[0].ToVector3() - _polygon[n - 1].ToVector3();
            Vector3 v2 = _polygon[1].ToVector3() - _polygon[0].ToVector3();
            float crossY_prev = v1.X * v2.Z - v1.Z * v2.X;
            
            for(int i = 2; i < n; i++)
            {
                v1 = v2;
                v2 = _polygon[i].ToVector3() - _polygon[i - 1].ToVector3();               
                float crossY = v1.X * v2.Z - v1.Z * v2.X;

                if ((crossY < 0) != (crossY_prev < 0))
                    return false;

                crossY_prev = crossY;
            }

            return true;
        }

        #endregion

        #region MESH UTILS: Polygon intersection

        // method projects the second polygon onto the plane of the 1st
        public static bool PolygonIntersectsPolygon(List<Vector3> _polygon1, List<Vector3> _polygon2)
        {
            if (_polygon1 == null || _polygon2 == null) return false;
            if (_polygon1.Count < 3 || _polygon2.Count < 3) return false;

            // project 2nd onto 1st
            List<Vector3> polygon2_pr = new List<Vector3>();
            foreach(Vector3 v in _polygon2)
            {
                Vector3 v_pr = CommonExtensions.ProjectPointOnPlane(v, _polygon1[0], _polygon1[1], _polygon1[2]);
                polygon2_pr.Add(v_pr);
            }

            // try to find a separating line btw the 2 polygons
            Vector3 pL1, pL2;
            bool separating_line_exists_in_1 = MeshesCustom.SeparatingLineExists(_polygon1, polygon2_pr, out pL1, out pL2);
            if (separating_line_exists_in_1)
                return false;

            bool separating_line_exists_in_2 = MeshesCustom.SeparatingLineExists(polygon2_pr, _polygon1, out pL1, out pL2);
            if (separating_line_exists_in_2)
                return false;

            return true;
        }

        // method assumes non-intersection even if there is an overlap of up to _tolerance
        public static bool PolygonIntersectsPolygon(List<Vector3> _polygon1, List<Vector3> _polygon2, double _tolerance)
        {
            if (_polygon1 == null || _polygon2 == null) return false;
            if (_polygon1.Count < 3 || _polygon2.Count < 3) return false;


            // project 2nd onto 1st
            List<Vector3> polygon2_pr = new List<Vector3>();
            foreach (Vector3 v in _polygon2)
            {
                Vector3 v_pr = CommonExtensions.ProjectPointOnPlane(v, _polygon1[0], _polygon1[1], _polygon1[2]);
                polygon2_pr.Add(v_pr);
            }

            // try to find a separating line btw the 2 polygons
            Vector3 pL1, pL2;
            bool separating_line_exists_in_1 = MeshesCustom.SeparatingLineExists(_polygon1, polygon2_pr, out pL1, out pL2);
            if (separating_line_exists_in_1)
                return false;

            bool separating_line_exists_in_2 = MeshesCustom.SeparatingLineExists(polygon2_pr, _polygon1, out pL1, out pL2);
            if (separating_line_exists_in_2)
                return false;

            // the polygons overlap to some extent

            // try to find an 'almost' separating line btw the 2 polygons
            float min_overlap = float.MaxValue;
            MeshesCustom.FindLineOfMinimalOverlap(_polygon1, polygon2_pr, _tolerance, out pL1, out pL2, out min_overlap);
            if (min_overlap < 1f)
                return false;

            MeshesCustom.FindLineOfMinimalOverlap(polygon2_pr, _polygon1, _tolerance, out pL1, out pL2, out min_overlap);
            if (min_overlap < 1f)
                return false;

            // the polygons overlap significantly

            return true;
        }

        private static bool SeparatingLineExists(List<Vector3> _polygon_to_test, List<Vector3> _second_polygon, out Vector3 pL1, out Vector3 pL2)
        {
            pL1 = Vector3.Zero;
            pL2 = Vector3.Zero;

            int nr1 = _polygon_to_test.Count;
            int nr2 = _second_polygon.Count;

            for (int i = 0; i < nr1; i++)
            {
                // line
                Vector3 q0 = _polygon_to_test[i];
                Vector3 q1 = _polygon_to_test[(i + 1) % nr1];

                // test if the line is well defined
                Vector3 qV = q1 - q0;
                if (qV.LengthSquared() < CommonExtensions.LINEDISTCALC_TOLERANCE)
                    continue;

                // project the polygons onto it and look at he projecting lines
                List<Vector3> projections1 = new List<Vector3>();
                for (int j = 0; j < nr1; j++)
                {
                    if (j == i || j == ((i + 1) % nr1)) continue;

                    Vector3 p = _polygon_to_test[j];
                    Vector3 pP = CommonExtensions.NormalProject(p, q0, q1);
                    Vector3 vP = pP - p;
                    if (vP.LengthSquared() <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                        continue;

                    vP.Normalize();
                    projections1.Add(vP);
                }

                List<Vector3> projections2 = new List<Vector3>();
                for (int j = 0; j < nr2; j++)
                {
                    Vector3 p = _second_polygon[j];
                    Vector3 pP = CommonExtensions.NormalProject(p, q0, q1);
                    Vector3 vP = pP - p;
                    if (vP.LengthSquared() <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                        continue;

                    vP.Normalize();
                    projections2.Add(vP);
                }

                bool found_points_on_the_same_side = false;
                foreach (Vector3 v1 in projections1)
                {
                    foreach (Vector3 v2 in projections2)
                    {
                        float cos12 = Vector3.Dot(v1, v2);
                        if (cos12 > CommonExtensions.GENERAL_CALC_TOLERANCE)
                        {
                            // on the same side of the line ->
                            found_points_on_the_same_side = true;
                            break;
                        }
                    }
                    if (found_points_on_the_same_side)
                        break;
                }
                if (!found_points_on_the_same_side)
                {
                    pL1 = q0;
                    pL2 = q1;
                    return true;
                }
            }

            return false;
        }

        // assumesthat the test from above has already been performed and there is NO separating line
        // this method should detect polygons that overlap very little (i.e. 'by accident')
        private static void FindLineOfMinimalOverlap(List<Vector3> _polygon_to_test, List<Vector3> _second_polygon, double _tolerance, out Vector3 pL1, out Vector3 pL2, out float min_overlap)
        {
            pL1 = Vector3.Zero;
            pL2 = Vector3.Zero;

            int nr1 = _polygon_to_test.Count;
            int nr2 = _second_polygon.Count;

            min_overlap = float.MaxValue;
            for (int i = 0; i < nr1; i++)
            {
                // line
                Vector3 q0 = _polygon_to_test[i];
                Vector3 q1 = _polygon_to_test[(i + 1) % nr1];

                //HEURISTIC: this line should be 'almost' parallel to a line in
                // the second polygon
                Vector3 vQ = q1 - q0;
                if (vQ.LengthSquared() <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                    continue;

                vQ.Normalize();

                bool almost_parallel_lines_found = false;
                for (int j = 0; j < nr2; j++)
                {
                    // corresponding line candidate
                    Vector3 p0 = _second_polygon[j];
                    Vector3 p1 = _second_polygon[(j + 1) % nr2];
                    Vector3 vP = p1 - p0;
                    if (vP.LengthSquared() <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                        continue;

                    vP.Normalize();

                    float cosPQ = Vector3.Dot(vP, vQ);
                    if (Math.Abs(cosPQ) > 0.95f)
                    {
                        double dist = CommonExtensions.LineToLineDist3D(q0, q1, p0, p1);
                        if (double.IsNaN(dist))
                            dist = 0;
                        if (dist < _tolerance)
                        {
                            almost_parallel_lines_found = true;
                            break;
                        }                        
                    }
                }
                if (!almost_parallel_lines_found)
                    continue;


                // project the second polygons onto it and look at the NOT NORMALIZED projecting lines
                List<Vector3> projections2 = new List<Vector3>();
                for (int j = 0; j < nr2; j++)
                {
                    Vector3 p = _second_polygon[j];
                    Vector3 pP = CommonExtensions.NormalProject(p, q0, q1);
                    Vector3 vP = pP - p;
                    if (vP.LengthSquared() <= CommonExtensions.GENERAL_CALC_TOLERANCE)
                        continue;

                    projections2.Add(vP);
                }
                
                Vector3 sum = Vector3.Zero;
                float sum_lengths = 0f;
                foreach (Vector3 v2 in projections2)
                {
                    sum += v2;
                    sum_lengths += v2.Length();
                }
                
                if (sum_lengths > CommonExtensions.GENERAL_CALC_TOLERANCE)
                {
                    // if the line does not intersect the second polygon there should be no difference -> 1
                    float overlap = (sum_lengths - sum.Length()) / sum_lengths;
                    if (overlap < CommonExtensions.GENERAL_CALC_TOLERANCE)
                        continue;

                    // min for a line that is a symmetry axis for the second polygon
                    if (min_overlap > overlap)
                    {
                        min_overlap = overlap;
                        pL1 = q0;
                        pL2 = q1;
                    }
                }

            }
            
        }

        #endregion

        #region MESH UTILS: Polygon with Holes: Connect each Hole to the Polygon TWICE

        public static void ConnectPolygonWContainedHoles(List<Point3D> _polygon, List<List<Point3D>> _holes, 
            out List<Vector4> connectingLines)
        {
            // [X]:-1 for polygon / otherwise hole index [Y]: index in polygon / hole, [Z]:hole index, [W]:index in hole
            connectingLines = new List<Vector4>();
            // left to right
            List<Vector4> connectingLines_LR = new List<Vector4>();
            List<int> ind_connected_holes_LR = new List<int>();
            // right to left
            List<Vector4> connectingLines_RL = new List<Vector4>();
            List<int> ind_connected_holes_RL = new List<int>();

            if (_polygon == null || _holes == null)
                return;

            int n = _polygon.Count;
            int nrH = _holes.Count;
            if (n < 3 || nrH < 1)
                return;

            // order the vertices according to the X component
            Vector3XComparer vec3Xcomp = new Vector3XComparer();
            SortedList<Vector3, int> vertices_ordered = new SortedList<Vector3, int>(vec3Xcomp);
            for (int i = 0; i < n; i++)
            {
                if (vertices_ordered.ContainsKey(_polygon[i].ToVector3()))
                    continue;

                try
                {
                    vertices_ordered.Add(_polygon[i].ToVector3(), i + 1);
                }
                catch (ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }
            for (int j = 0; j < nrH; j++ )
            {
                List<Point3D> hole = _holes[j];
                if (hole == null || hole.Count < 3)
                    continue;
                int h = hole.Count;
                for (int i = 0; i < h; i++)
                {
                    if (vertices_ordered.ContainsKey(hole[i].ToVector3()))
                        continue;

                    try
                    {
                        vertices_ordered.Add(hole[i].ToVector3(), (j + 1) * 1000 + i + 1);
                    }
                    catch (ArgumentException)
                    {
                        // if the same vertex occurs more than once, just skip it
                        continue;
                    }
                }
            }
            int m = vertices_ordered.Count;

            // prepare polygon and holes for evaluating functions
            List<Vector3> polygon_asV3 = CommonExtensions.ConvertPoints3DListToVector3List(_polygon);
            List<List<Vector3>> holes_asV3 = CommonExtensions.ConvertPoints3DListListToVector3ListList(_holes);

            // -------------------------------- TRAVERSAL LEFT -> RIGHT ------------------------------------- //

            // traverse the polygon in X-direction to determine the admissible diagonals 
            // connecting the FIRST points of each hole with the polygon vertices to the LEFT of them
            // (if such do not exist -> try connecting to previous holes to the LEFT)
            for (int j = 1; j < m - 1; j++)
            {
                Vector3 current_alongX = vertices_ordered.ElementAt(j).Key;
                int ind_current_alongX = vertices_ordered.ElementAt(j).Value - 1;
                int ind_hole = ind_current_alongX / 1000 - 1;
                if (ind_hole < 0 || ind_hole > nrH - 1 || ind_connected_holes_LR.Contains(ind_hole))
                    continue;

                // get information of the neighbor vertices in the hole
                List<Point3D> hole = _holes[ind_hole];
                int nHole = hole.Count;
                int ind_in_hole = ind_current_alongX % 1000;
                Vector3 prev = hole[(nHole + ind_in_hole - 1) % nHole].ToVector3();
                Vector3 next = hole[(ind_in_hole + 1) % nHole].ToVector3();

                if (prev.X >= current_alongX.X && next.X >= current_alongX.X)
                {
                    // START VERTEX -> Connect to a polygon vertex that is before this one along the X axis
                    Vector3 prev_poly_alongX;
                    int ind_prev_poly_alongX;
                    for (int c = 1; c < j + 1; c++)
                    {
                        prev_poly_alongX = vertices_ordered.ElementAt(j - c).Key;
                        ind_prev_poly_alongX = vertices_ordered.ElementAt(j - c).Value - 1;
                        int ind_prev_hole = ind_prev_poly_alongX / 1000 - 1;
                        if (ind_prev_hole == ind_hole)
                            continue;

                        int ind_prev_in_hole = ind_prev_poly_alongX % 1000;


                        if (prev_poly_alongX.X < current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = false;
                            if (ind_prev_hole == -1)
                            {
                                // check admissibility in the polygon
                                isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                                    ind_prev_poly_alongX, ind_hole, ind_in_hole);
                            }
                            else
                            {
                                // check admissiblity w regard to two holes contained in the polygon
                                isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                          ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole);
                            }
                            if (isAdmissible)
                            {
                                connectingLines_LR.Add(new Vector4(ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole));
                                ind_connected_holes_LR.Add(ind_hole);
                                break;
                            }
                        }
                    }
                }

            }
            connectingLines.AddRange(connectingLines_LR);

            // -------------------------------- TRAVERSAL RIGHT -> LEFT ------------------------------------- //
            // traverse the polygon in X-direction to determine the admissible diagonals 
            // connecting the LAST points of each hole with the polygon vertices to the RIGHT of them
            // (if such do not exist -> try connecting to previous holes to the RIGHT)
            for (int j = m - 2; j > 0; j--)
            {
                Vector3 current_alongX = vertices_ordered.ElementAt(j).Key;
                int ind_current_alongX = vertices_ordered.ElementAt(j).Value - 1;
                int ind_hole = ind_current_alongX / 1000 - 1;
                if (ind_hole < 0 || ind_hole > nrH - 1 || ind_connected_holes_RL.Contains(ind_hole))
                    continue;

                // get information of the neighbor vertices in the hole
                List<Point3D> hole = _holes[ind_hole];
                int nHole = hole.Count;
                int ind_in_hole = ind_current_alongX % 1000;
                Vector3 prev = hole[(nHole + ind_in_hole - 1) % nHole].ToVector3();
                Vector3 next = hole[(ind_in_hole + 1) % nHole].ToVector3();

                if (prev.X <= current_alongX.X && next.X <= current_alongX.X)
                {
                    // END VERTEX -> Connect to a polygon vertex that is after this one along the X axis
                    Vector3 next_poly_alongX;
                    int ind_next_poly_alongX;
                    for (int c = 1; c < m - j; c++)
                    {
                        next_poly_alongX = vertices_ordered.ElementAt(j + c).Key;
                        ind_next_poly_alongX = vertices_ordered.ElementAt(j + c).Value - 1;
                        int ind_next_hole = ind_next_poly_alongX / 1000 - 1;
                        if (ind_next_hole == ind_hole)
                            continue;

                        int ind_next_in_hole = ind_next_poly_alongX % 1000;

                        if (next_poly_alongX.X > current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = false;
                            if (ind_next_hole == -1)
                            {
                                // check admissibility in the polygon
                                isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                                    ind_next_poly_alongX, ind_hole, ind_in_hole);
                            }
                            else
                            {
                                // check admissiblity w regard to two holes contained in the polygon
                                isAdmissible = LineIsValidInPolygonWHoles(polygon_asV3, holes_asV3,
                                                                          ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole);
                            }
                            // check if the diagonal intersects any diagonals from the previous traversal
                            if (isAdmissible)
                            {
                                Vector3 p1 = holes_asV3[ind_hole][ind_in_hole];
                                Vector3 p2;
                                if (ind_next_hole == -1)
                                    p2 = polygon_asV3[ind_next_poly_alongX];
                                else
                                    p2 = holes_asV3[ind_next_hole][ind_next_in_hole];

                                foreach (Vector4 entry in connectingLines_LR)
                                {
                                    Vector3 q1 = holes_asV3[(int)entry.Z][(int)entry.W];
                                    Vector3 q2;
                                    if (entry.X == -1)
                                        q2 = polygon_asV3[(int)entry.Y];
                                    else
                                        q2 = holes_asV3[(int)entry.X][(int)entry.Y];

                                    Vector3 _colPos;
                                    bool intersection = CommonExtensions.LineWLineCollision3D(p1, p2, q1, q2,
                                                                    CommonExtensions.GENERAL_CALC_TOLERANCE, out _colPos);
                                    if (intersection)
                                    {
                                        isAdmissible = false;
                                        break;
                                    }
                                }
                            }
                            if (isAdmissible)
                            {
                                connectingLines_RL.Add(new Vector4(ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole));
                                ind_connected_holes_RL.Add(ind_hole);
                                break;
                            }
                        }
                    }
                }

            }
            connectingLines.AddRange(connectingLines_RL);

            // remove duplicates
            List<Vector4> connectingLines_optimized = new List<Vector4>();
            int nrC = connectingLines.Count;
            for(int i = 0; i < nrC; i++)
            {
                bool hasReversedDuplicate = false;
                for (int j = i + 1; j < nrC; j++)
                {
                    if (connectingLines[i].X == connectingLines[j].Z && connectingLines[i].Y == connectingLines[j].W &&
                        connectingLines[i].Z == connectingLines[j].X && connectingLines[i].W == connectingLines[j].Y)
                    {
                        hasReversedDuplicate = true;
                        break;
                    }
                }
                if (!hasReversedDuplicate)
                    connectingLines_optimized.Add(connectingLines[i]);
            }
            connectingLines = new List<Vector4>(connectingLines_optimized);

        }

        public static void ConnectPolygonWContainedHolesTwice(List<Point3D> _polygon, List<List<Point3D>> _holes, 
            out List<Vector4> connectingLines)
        {
            connectingLines = new List<Vector4>();
            // use polygons as they are
            List<Vector4> connectingLines_original;
            ConnectPolygonWContainedHoles(_polygon, _holes, out connectingLines_original);

            // switch X and Z and try again
            List<Point3D> poly_swapped = _polygon.Select(x => new Point3D(x.Z, x.Y, x.X)).ToList();
            List<List<Point3D>> holes_swapped = new List<List<Point3D>>();
            foreach(List<Point3D> h in _holes)
            {
                holes_swapped.Add(h.Select(x => new Point3D(x.Z, x.Y, x.X)).ToList());
            }

            // try again
            List<Vector4> connectingLines_swapped;
            ConnectPolygonWContainedHoles(poly_swapped, holes_swapped, out connectingLines_swapped);

            // remove duplicates
            List<Vector4> all = connectingLines_original.Concat(connectingLines_swapped).ToList();
            int nrC = all.Count;
            for (int i = 0; i < nrC; i++)
            {
                bool hasDuplicate = false;
                for (int j = i + 1; j < nrC; j++)
                {
                    if ((all[i].X == all[j].X && all[i].Y == all[j].Y && all[i].Z == all[j].Z && all[i].W == all[j].W) ||
                        (all[i].X == all[j].Z && all[i].Y == all[j].W && all[i].Z == all[j].X && all[i].W == all[j].Y))
                    {
                        hasDuplicate = true;
                        break;
                    }
                }
                if (!hasDuplicate)
                    connectingLines.Add(all[i]);
            }

            // remove intersecting connections
            nrC = connectingLines.Count;
            List<Vector4> to_remove = new List<Vector4>();
            for (int i = 0; i < nrC; i++)
            {
                Vector3 p1, p2;
                if (connectingLines[i].X == -1)
                    p1 = _polygon[(int)connectingLines[i].Y].ToVector3();
                else
                    p1 = _holes[(int)connectingLines[i].X][(int)connectingLines[i].Y].ToVector3();
                
                if (connectingLines[i].Z == -1)
                    p2 = _polygon[(int)connectingLines[i].W].ToVector3();
                else
                    p2 = _holes[(int)connectingLines[i].Z][(int)connectingLines[i].W].ToVector3();


                for (int j = i + 1; j < nrC; j++)
                {
                    Vector3 q1, q2;
                    if (connectingLines[j].X == -1)
                        q1 = _polygon[(int)connectingLines[j].Y].ToVector3();
                    else
                        q1 = _holes[(int)connectingLines[j].X][(int)connectingLines[j].Y].ToVector3();

                    if (connectingLines[j].Z == -1)
                        q2 = _polygon[(int)connectingLines[j].W].ToVector3();
                    else
                        q2 = _holes[(int)connectingLines[j].Z][(int)connectingLines[j].W].ToVector3();

                    Vector3 p_inters;
                    bool inters = CommonExtensions.LineWLineCollision3D(p1, p2, q1, q2, CommonExtensions.GENERAL_CALC_TOLERANCE, out p_inters);
                    if (inters)
                    {
                        // delete line not connecting to polygon
                        if (connectingLines[i].X == -1 || connectingLines[i].Z == -1)
                            to_remove.Add(connectingLines[j]);
                        else
                            to_remove.Add(connectingLines[i]);
                    }
                }
            }
            foreach(Vector4 v in to_remove)
            {
                connectingLines.Remove(v);
            }
        }

        #endregion

        #region MESH UTILS: Calculating Admissiblity of diagonals in a Polygon with Holes
        // assumes that the holes do not overlap and that they are all inside of the polygon

        internal static bool LineIsValidInPolygonWHoles(List<Vector3> _polygon, List<List<Vector3>> _holes, 
                                                    int _indPolygon, int _indHole, int _indInHole)
        {
            if (_polygon == null || _holes == null)
                return false;

            int n = _polygon.Count;
            int nrH = _holes.Count;
            if (_indPolygon < 0 || _indPolygon > (n - 1) || _indHole < 0 || _indHole > (nrH - 1))
                return false;

            List<Vector3> hole = _holes[_indHole];
            if (hole == null)
                return false;

            int h = hole.Count;
            if (_indInHole < 0 || _indInHole > (h - 1))
                return false;

            // define the line
            Vector3 p1 = _polygon[_indPolygon];
            Vector3 p2 = hole[_indInHole];
            bool intersectsSomething = false;

            // check for intersections w the polygon
            intersectsSomething = LineInTersectsPolygon(p1, p2, _polygon, new List<int> { _indPolygon });
            if (intersectsSomething)
                return false;

            // if no intersection, check if the line is inside or outside of the polygon
            bool p1IsInside = PointIsInsidePolygonXZ(_polygon, p1);
            if (!p1IsInside)
                return false;

            // check for intersections w the hole
            intersectsSomething = LineInTersectsPolygon(p1, p2, hole, new List<int> { _indInHole });
            if (intersectsSomething)
                return false;

            // check for intersections w all other holes
            for (int c = 0; c < nrH; c++)
            {
                if (c == _indHole)
                    continue;

                intersectsSomething = LineInTersectsPolygon(p1, p2, _holes[c], null);
                if (intersectsSomething)
                    break;
            }

            return !intersectsSomething;
        }

        internal static bool LineIsValidInPolygonWHoles(List<Vector3> _polygon, List<List<Vector3>> _holes,
                                                    int _indHole1, int _indInHole1, int _indHole2, int _indInHole2)
        {
            if (_polygon == null || _holes == null)
                return false;

            int n = _polygon.Count;
            int nrH = _holes.Count;
            if (_indHole1 < 0 || _indHole1 > (nrH - 1) || _indHole2 < 0 || _indHole2 > (nrH - 1))
                return false;

            List<Vector3> hole1 = _holes[_indHole1];
            if (hole1 == null)
                return false;
            List<Vector3> hole2 = _holes[_indHole2];
            if (hole2 == null)
                return false;

            int h1 = hole1.Count;
            if (_indInHole1 < 0 || _indInHole1 > (h1 - 1))
                return false;
            int h2 = hole2.Count;
            if (_indInHole2 < 0 || _indInHole2 > (h2 - 1))
                return false;

            // define the line
            Vector3 p1 = hole1[_indInHole1];
            Vector3 p2 = hole2[_indInHole2];
            bool intersectsSomething = false;

            // check for intersections w the polygon
            intersectsSomething = LineInTersectsPolygon(p1, p2, _polygon, null);
            if (intersectsSomething)
                return false;

            // if no intersection, check if the line is inside or outside of the polygon
            bool p1IsInside = PointIsInsidePolygonXZ(_polygon, p1);
            bool p2IsInside = PointIsInsidePolygonXZ(_polygon, p2);
            if (!p1IsInside || !p2IsInside)
                return false;

            // check for intersections w  hole 1
            intersectsSomething = LineInTersectsPolygon(p1, p2, hole1, new List<int> { _indInHole1 });
            if (intersectsSomething)
                return false;

            // check for intersections w  hole 2
            intersectsSomething = LineInTersectsPolygon(p1, p2, hole2, new List<int> { _indInHole2 });
            if (intersectsSomething)
                return false;

            // check for intersections w all other holes
            for (int c = 0; c < nrH; c++)
            {
                if (c == _indHole1 || c == _indHole2)
                    continue;

                intersectsSomething = LineInTersectsPolygon(p1, p2, _holes[c], null);
                if (intersectsSomething)
                    break;
            }

            return !intersectsSomething;

        }


        private static bool LineInTersectsPolygon(Vector3 _p1, Vector3 _p2, List<Vector3> _polygon, List<int> _exclIndices)
        {
            if (_polygon == null)
                return false;

            bool checkIndices = (_exclIndices != null && _exclIndices.Count > 0);

            bool intersects_polygon = false;
            int n = _polygon.Count;
            for (int i = 0; i < n; i++)
            {
                if (checkIndices)
                {
                    if (_exclIndices.Contains(i) || _exclIndices.Contains((i + 1) % n))
                        continue;
                }

                Vector3 _colPos;
                intersects_polygon = CommonExtensions.LineWLineCollision3D(_p1, _p2, _polygon[i], _polygon[(i + 1) % n],
                                                CommonExtensions.GENERAL_CALC_TOLERANCE, out _colPos);
                if (intersects_polygon)
                    break;
            }
            return intersects_polygon;
        }

        #endregion

        #region Polygon Utils: Split

        public static void SplitPolygonAlongAnotherWIntersection(List<Vector3> _poly_to_split, List<Vector3> _poly_split_line, Vector3 _split_point_1, Vector3 _split_point_2, double _tolerance,
                                            out List<Vector3> poly_1, out List<Vector3> poly_2, out List<Vector3> poly_3, out List<Vector3> poly_4)
        {
            poly_1 = new List<Vector3>();
            poly_2 = new List<Vector3>();
            poly_3 = new List<Vector3>();
            poly_4 = new List<Vector3>();
            if (_poly_to_split == null || _poly_split_line == null) return;

            int nrP_poly = _poly_to_split.Count;
            int nrP_splitter = _poly_split_line.Count;
            if (nrP_poly < 3 || nrP_splitter < 3) return;

            // 0. make sure the polygons are both CCW
            bool polygon_is_valid;
            bool polygon_isCW = CalculateIfPolygonClockWise(_poly_to_split, CommonExtensions.GENERAL_CALC_TOLERANCE, out polygon_is_valid);
            if (!polygon_is_valid) return;
            List<Vector3> poly_to_split_r = _poly_to_split;
            if (!polygon_isCW)
                poly_to_split_r = ReversePolygon(_poly_to_split);

            bool splitter_is_valid;
            bool splitter_isCW = CalculateIfPolygonClockWise(_poly_split_line, CommonExtensions.GENERAL_CALC_TOLERANCE, out splitter_is_valid);
            if (!splitter_is_valid) return;
            List<Vector3> splitter_r = _poly_split_line;
            if (!splitter_isCW)
                splitter_r = ReversePolygon(_poly_split_line);

            // 1. intersect the polygons
            List<Vector3> intersections = new List<Vector3>();
            List<int> inters_index_poly = new List<int>();
            List<int> inters_index_splitter = new List<int>();
            for(int i = 0; i < nrP_poly; i++)
            {
                for(int j = 0; j < nrP_splitter; j++)
                {
                    Vector3 inters = new Vector3(0, 0, 0);
                    bool success = CommonExtensions.LineWLineCollision3D_InclAtEnds(poly_to_split_r[i], poly_to_split_r[(i + 1) % nrP_poly],
                                                                                    splitter_r[j], splitter_r[(j + 1) % nrP_splitter], CommonExtensions.LINEDISTCALC_TOLERANCE * 100, out inters);
                    if (success)
                    {
                        intersections.Add(new Vector3(inters.X, inters.Y, inters.Z));
                        inters_index_poly.Add(i);
                        inters_index_splitter.Add(j);
                    }                       
                }
            }

            if (intersections.Count < 2) return;

            // 2. find the intersections closest to the guiding points
            double min_dist_1 = double.MaxValue;
            double min_dist_2 = double.MaxValue;
            int inters_index_1 = -1; 
            int inters_index_2 = -1;
            for(int k = 0; k < intersections.Count; k++)
            {
                Vector3 v1 = _split_point_1 - intersections[k];
                double dist1 = v1.Length();
                if (dist1 < min_dist_1)
                {
                    min_dist_1 = dist1;
                    inters_index_1 = k;
                }

                Vector3 v2 = _split_point_2 - intersections[k];
                double dist2 = v2.Length();
                if (dist2 < min_dist_2)
                {
                    min_dist_2 = dist2;
                    inters_index_2 = k;
                }
            }

            if (inters_index_1 < 0 || inters_index_2 < 0) return;

            Vector3 split_p1 = intersections[inters_index_1];
            int split_ind1_poly = inters_index_poly[inters_index_1];
            int split_ind1_splitter = inters_index_splitter[inters_index_1];

            Vector3 split_p2 = intersections[inters_index_2];
            int split_ind2_poly = inters_index_poly[inters_index_2];
            int split_ind2_splitter = inters_index_splitter[inters_index_2];

            // 3. insert the new points into the polygons at the correct index
            List<Vector3> poly_to_split_refined = new List<Vector3>();
            int split_ind1_poly_refined = 0;
            int split_ind2_poly_refined = 0;
            for (int i = 0; i < nrP_poly; i++)
            {
                poly_to_split_refined.Add(poly_to_split_r[i]);
                if (i == split_ind1_poly)
                {
                    if ((poly_to_split_r[i] - split_p1).Length() >= _tolerance &&
                        (poly_to_split_r[(i + 1) % nrP_poly] - split_p1).Length() >= _tolerance)
                    {
                        poly_to_split_refined.Add(split_p1);
                        split_ind1_poly_refined = poly_to_split_refined.Count - 1;
                    }
                    else if ((poly_to_split_r[i] - split_p1).Length() < _tolerance)
                    {
                        split_ind1_poly_refined = poly_to_split_refined.Count - 1;
                    }
                    else
                    {
                        split_ind1_poly_refined = (i == nrP_poly - 1) ? 0 : poly_to_split_refined.Count;
                    }                   
                }
                if (i == split_ind2_poly)
                {
                    if ((poly_to_split_r[i] - split_p2).Length() >= _tolerance &&
                        (poly_to_split_r[(i + 1) % nrP_poly] - split_p2).Length() >= _tolerance)
                    {
                        poly_to_split_refined.Add(split_p2);
                        split_ind2_poly_refined = poly_to_split_refined.Count - 1;
                    }
                    else if ((poly_to_split_r[i] - split_p2).Length() < _tolerance)
                    {
                        split_ind2_poly_refined = poly_to_split_refined.Count - 1;
                    }
                    else
                    {
                        split_ind2_poly_refined = (i == nrP_poly - 1) ? 0 : poly_to_split_refined.Count;
                    }
                }
            }
            int split_ind1_poly_refined_ordered = Math.Min(split_ind1_poly_refined, split_ind2_poly_refined);
            int split_ind2_poly_refined_ordered = Math.Max(split_ind1_poly_refined, split_ind2_poly_refined);

            List<Vector3> splitter_refined = new List<Vector3>();
            int split_ind1_splitter_refined = 0;
            int split_ind2_splitter_refined = 0;
            for (int i = 0; i < nrP_splitter; i++)
            {
                splitter_refined.Add(splitter_r[i]);
                if (i == split_ind1_splitter)
                {
                    if ((splitter_r[i] - split_p1).Length() >= _tolerance &&
                        (splitter_r[(i + 1) % nrP_splitter] - split_p1).Length() >= _tolerance)
                    {
                        splitter_refined.Add(split_p1);
                        split_ind1_splitter_refined = splitter_refined.Count - 1;
                    }
                    else if ((splitter_r[i] - split_p1).Length() < _tolerance)
                    {
                        split_ind1_splitter_refined = splitter_refined.Count - 1;
                    }
                    else
                    {
                        split_ind1_splitter_refined = (i == nrP_splitter) ? 0 : splitter_refined.Count;
                    }
                }
                if (i == split_ind2_splitter)
                {
                    if ((splitter_r[i] - split_p2).Length() >= _tolerance &&
                        (splitter_r[(i + 1) % nrP_splitter] - split_p2).Length() >= _tolerance)
                    {
                        splitter_refined.Add(split_p2);
                        split_ind2_splitter_refined = splitter_refined.Count - 1;
                    }
                    else if ((splitter_r[i] - split_p2).Length() < _tolerance)
                    {
                        split_ind2_splitter_refined = splitter_refined.Count - 1;
                    }
                    else
                    {
                        split_ind2_splitter_refined = (i == nrP_splitter) ? 0 : splitter_refined.Count;
                    }
                }
            }
            int split_ind1_splitter_refined_ordered = Math.Min(split_ind1_splitter_refined, split_ind2_splitter_refined);
            int split_ind2_splitter_refined_ordered = Math.Max(split_ind1_splitter_refined, split_ind2_splitter_refined);

            //// DEBUG
            //poly_1 = poly_to_split_refined;
            //poly_2 = splitter_refined;

            // 4. split polygon
            List<Vector3> poly_12 = new List<Vector3>();
            List<Vector3> poly_21 = new List<Vector3>();
            List<Vector3> poly_21_start = new List<Vector3>();
            List<Vector3> poly_21_end = new List<Vector3>();
            for (int i = 0; i < poly_to_split_refined.Count; i++)
            {
                if (i < split_ind1_poly_refined_ordered)
                    poly_21_start.Add(poly_to_split_refined[i]);
                else if (i == split_ind1_poly_refined_ordered)
                {
                    poly_21_start.Add(poly_to_split_refined[i]);
                    poly_12.Add(poly_to_split_refined[i]);
                }
                else if (split_ind1_poly_refined_ordered < i && i < split_ind2_poly_refined_ordered)
                    poly_12.Add(poly_to_split_refined[i]);
                else if (i == split_ind2_poly_refined_ordered)
                {
                    poly_12.Add(poly_to_split_refined[i]);
                    poly_21_end.Add(poly_to_split_refined[i]);
                }
                if (i > split_ind2_poly_refined_ordered)
                    poly_21_end.Add(poly_to_split_refined[i]);
            }
            poly_21 = new List<Vector3>(poly_21_end);
            poly_21.AddRange(poly_21_start);

            // 5. split splitter
            List<Vector3> splitter_12 = new List<Vector3>();
            List<Vector3> splitter_21 = new List<Vector3>();
            List<Vector3> splitter_21_start = new List<Vector3>();
            List<Vector3> splitter_21_end = new List<Vector3>();
            for (int i = 0; i < splitter_refined.Count; i++)
            {
                if (i < split_ind1_splitter_refined_ordered)
                    splitter_21_start.Add(splitter_refined[i]);
                else if (i == split_ind1_splitter_refined_ordered)
                {
                    splitter_21_start.Add(splitter_refined[i]);
                    splitter_12.Add(splitter_refined[i]);
                }
                else if (split_ind1_splitter_refined_ordered < i && i < split_ind2_splitter_refined_ordered)
                    splitter_12.Add(splitter_refined[i]);
                else if (i == split_ind2_splitter_refined_ordered)
                {
                    splitter_12.Add(splitter_refined[i]);
                    splitter_21_end.Add(splitter_refined[i]);
                }
                if (i > split_ind2_splitter_refined_ordered)
                    splitter_21_end.Add(splitter_refined[i]);
            }
            splitter_21 = new List<Vector3>(splitter_21_end);
            splitter_21.AddRange(splitter_21_start);

            //// DEBUG
            //poly_1 = poly_12;
            //poly_2 = poly_21;
            //poly_3 = splitter_12;
            //poly_4 = splitter_21;

            // 6. combine the segments to new polygons
            // poly12 - splitter_12 (poly_1)
            // poly12 - splitter_21 (poly_2)
            // poly21 - splitter_12 (poly_3)
            // poly21 - splitter_21 (poly_4)

            if (poly_12.Count > 1)
            {
                poly_1 = new List<Vector3>(poly_12);
                if (splitter_12.Count > 1)
                {
                    if ((poly_12[0] - splitter_12[0]).Length() < _tolerance)
                    {
                        List<Vector3> splitter_12_r = new List<Vector3>(splitter_12);
                        splitter_12_r.Reverse();
                        poly_1.AddRange(splitter_12_r.Skip(1).Take(splitter_12.Count - 2));
                    }
                    else
                    {
                        poly_1.AddRange(splitter_12.Skip(1).Take(splitter_12.Count - 2));
                    }
                }

                poly_2 = new List<Vector3>(poly_12);
                if (splitter_21.Count > 1)
                {
                    if ((poly_12[0] - splitter_21[0]).Length() < _tolerance)
                    {
                        List<Vector3> splitter_21_r = new List<Vector3>(splitter_21);
                        splitter_21_r.Reverse();
                        poly_2.AddRange(splitter_21_r.Skip(1).Take(splitter_21.Count - 2));
                    }
                    else
                    {
                        poly_2.AddRange(splitter_21.Skip(1).Take(splitter_21.Count - 2));
                    }
                }
            }

            if (poly_21.Count > 1)
            {
                poly_3 = new List<Vector3>(poly_21);
                if (splitter_12.Count > 1)
                {
                    if ((poly_21[0] - splitter_12[0]).Length() < _tolerance)
                    {
                        List<Vector3> splitter_12_r = new List<Vector3>(splitter_12);
                        splitter_12_r.Reverse();
                        poly_3.AddRange(splitter_12_r.Skip(1).Take(splitter_12.Count - 2));
                    }
                    else
                    {
                        poly_3.AddRange(splitter_12.Skip(1).Take(splitter_12.Count - 2));
                    }
                }

                poly_4 = new List<Vector3>(poly_21);
                if (splitter_21.Count > 1)
                {
                    if ((poly_21[0] - splitter_21[0]).Length() < _tolerance)
                    {
                        List<Vector3> splitter_21_r = new List<Vector3>(splitter_21);
                        splitter_21_r.Reverse();
                        poly_4.AddRange(splitter_21_r.Skip(1).Take(splitter_21.Count - 2));
                    }
                    else
                    {
                        poly_4.AddRange(splitter_21.Skip(1).Take(splitter_21.Count - 2));
                    }
                }
            }
            
        }

        #endregion

        #region MESH UTILS: Calculating simple dist btw points

        public static float DistV3Simple(Vector3 _v1, Vector3 _v2)
        {
            float dX = Math.Abs(_v1.X - _v2.X);
            float dY = Math.Abs(_v1.Y - _v2.Y);
            float dZ = Math.Abs(_v1.Z - _v2.Z);

            float dMax = Math.Max(dX, Math.Max(dY, dZ));
            return dMax;
        }


        #endregion

        #region MESH UTILS: Combining and Sorting Meshes
        public static MeshGeometry3D CombineMeshes(List<MeshGeometry3D> _meshGs)
        {
            if (_meshGs == null)
                return null;

            int nrMeshes = _meshGs.Count;
            if (nrMeshes < 1)
                return null;
            if (nrMeshes == 1)
                return _meshGs[0];

            List<Vector3> allPositions = new List<Vector3>();
            List<Vector3> allNormals = new List<Vector3>();
            List<Vector2> allTexCoords = new List<Vector2>();
            List<int> allIndices = new List<int>();
            int posOffset = 0;

            for(int i = 0; i < nrMeshes; i++)
            {
                MeshGeometry3D mesh = _meshGs[i];
                if (mesh == null || mesh.Positions.Count() < 1)
                    continue;

                // copy positions and normals and texture coordinates
                allPositions.AddRange(mesh.Positions);
                allNormals.AddRange(mesh.Normals);
                if (mesh.TextureCoordinates != null)
                    allTexCoords.AddRange(mesh.TextureCoordinates);
                else
                    allTexCoords.AddRange(Enumerable.Repeat(Vector2.Zero, mesh.Positions.Count()).ToList());

                // copy and offset indices into the position and normal arrays               
                for(int j = 0; j < mesh.Indices.Count(); j++)
                {
                    allIndices.Add(mesh.Indices[j] + posOffset);
                }
                posOffset += mesh.Positions.Count();
            }

            return new MeshGeometry3D()
            {
                Positions = allPositions.ToArray(),
                Normals = allNormals.ToArray(),
                TextureCoordinates = (allTexCoords.Count > 0) ? allTexCoords.ToArray() : null,
                Indices = allIndices.ToArray()
            };

        }

        public static MeshGeometry3D SortAndCombineMeshes(List<MeshGeometry3D> _meshGs)
        {
            if (_meshGs == null)
                return null;

            int nrMeshes = _meshGs.Count;
            if (nrMeshes < 1)
                return null;
            if (nrMeshes == 1)
                return _meshGs[0];

            // exclude empty meshes
            List<MeshGeometry3D> to_process = new List<MeshGeometry3D>();
            foreach(var mesh in _meshGs)
            {
                if (mesh == null || mesh.Positions == null)
                    continue;

                if (mesh.Positions.Count() > 2)
                    to_process.Add(mesh);
            }
            nrMeshes = to_process.Count;

            // calculate all pivots
            List<Vector3> pivots = new List<Vector3>();
            Vector3 pivotAll = Vector3.Zero;
            foreach (var mesh in to_process)
            {
                Vector3 p = GetMeshPivot(mesh);
                pivots.Add(p);
                pivotAll += p;
            }
            pivotAll /= nrMeshes;

            // get the distances from the common Pivot and sort accordingly
            List<MeshGeometry3DWMeasure> sorted_meshes = new List<MeshGeometry3DWMeasure>();
            float maxDist = 0f;
            float minDist = float.MaxValue;
            for (int i = 0; i < nrMeshes; i++ )
            {
                float dist = Vector3.DistanceSquared(pivots[i], pivotAll);
                if (maxDist < dist)
                    maxDist = dist;
                if (minDist > dist)
                    minDist = dist;

                MeshGeometry3DWMeasure mm = new MeshGeometry3DWMeasure();
                mm.mesh = to_process[i];
                mm.measure = dist;
                sorted_meshes.Add(mm);
            }
            //sorted_meshes.Sort(MeshGeometry3DWMeasure.CompareMeshesByMeasure);

            // assign the appropriate texture coordinates to the sorted meshes
            float distSpectrum = maxDist - minDist;
            float t1 = distSpectrum * 0.2f;
            float t2 = distSpectrum * 0.4f;
            float t3 = distSpectrum * 0.6f;
            float t4 = distSpectrum * 0.8f;

            foreach(var mm in sorted_meshes)
            {
                List<Vector2> tc = new List<Vector2>();
                if (mm.mesh.TextureCoordinates == null)
                    tc = Enumerable.Repeat(new Vector2(1f,0f), mm.mesh.Positions.Count()).ToList();
                else
                    tc = mm.mesh.TextureCoordinates.ToList();
                int nrTC = tc.Count;

                float offset = 0f;
                if (mm.measure < t1)
                    offset = 0f;
                else if (mm.measure < t2)
                    offset = 0.21f;
                else if (mm.measure < t3)
                    offset = 0.41f;
                else if (mm.measure < t4)
                    offset = 0.61f;
                else
                    offset = 0.81f;

                for (int i = 0; i < nrTC; i++)
                {
                    Vector2 old = tc[i];
                    tc[i] = new Vector2(0.18f + offset, old.Y);
                }
                mm.mesh.TextureCoordinates = tc.ToArray();
            }

            // extract the meshes and combine them
            List<MeshGeometry3D> to_be_combined = new List<MeshGeometry3D>();
            foreach(var mm in sorted_meshes)
            {
                to_be_combined.Add(mm.mesh);
            }

            return CombineMeshes(to_be_combined);

        }

        private static Vector3 GetMeshPivot(MeshGeometry3D _mesh)
        {
            if (_mesh == null || _mesh.Positions == null)
                return Vector3.Zero;

            Vector3 pivot = Vector3.Zero;
            int nrPos = _mesh.Positions.Count();
            if (nrPos == 0)
                return pivot;

            foreach(Vector3 pos in _mesh.Positions)
            {
                pivot += pos;
            }
            pivot /= nrPos;

            return pivot;
        }

        

        #endregion
    }
}
