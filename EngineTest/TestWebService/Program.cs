using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using WebServiceConnector;
using WebServiceConnector.MultizoneService;
using WebServiceConnector.ShadowService;

namespace TestWebService
{
    class Program
    {
        static void Main(string[] args)
        {
            //testVerschattung();
            testMultizonen();
        }

        private static void testVerschattung()
        {
            Vector3D sun = new Vector3D(-1, 0, -0.5);

            List<Point3D> points = new List<Point3D>();
            points.Add(new Point3D(0, 0, 0));
            points.Add(new Point3D(0, 5, 0));
            points.Add(new Point3D(0, 5, 3));
            points.Add(new Point3D(0, 0, 3));
            Surface fl1 = new Surface(false, "Test", points);


            List<Point3D> points1 = new List<Point3D>();
            points1.Add(new Point3D(2, 0, 0));
            points1.Add(new Point3D(2, 2.5, 0));
            points1.Add(new Point3D(2, 2.5, 3));
            points1.Add(new Point3D(2, 0, 3));
            Surface fl2 = new Surface(false, "halb", points1);

            List<Surface> surfaces = new List<Surface>();
            surfaces.Add(fl1);
            surfaces.Add(fl2);

            /*
            List<Point3D> pointsWindow = new List<Point3D>();
            pointsWindow.Add(new Point3D(1,0,0.8));
            pointsWindow.Add(new Point3D(2,0,0.8));
            pointsWindow.Add(new Point3D(2,0,1.8));
            pointsWindow.Add(new Point3D(1, 0, 1.8));
            Surface w1 = new Surface(true, "Window1",pointsWindow);
            List<Surface> openings = new List<Surface>();
            openings.Add(w1);
            fl1.Openings= openings;
             */

            ShadowServ test = new ShadowServ(sun, surfaces);
            List<ShadowResult> result = new List<ShadowResult>();
            String info;
            ShadowServ.FailureShadow f;

            result = test.executeShadowService(@"http://128.130.183.105:8001/calcShadow", out f, out info);

            foreach (var item in result)
            {
                Console.WriteLine(item.Verschattung);
            }
            Console.WriteLine(f.ToString() + ": " + info.ToString());
            Console.ReadKey();
        }

        private static void testMultizonen()
        {
            //1024,8*38*15 Kapazität Einrichtung 1024,8 * 38* Grundfläche
            Zone z1 = new Zone("z1", 15, 20, 584000, true, false, 22, false, 0, false, 25, false, 0, 23, 24);

            Schicht s1 = new Schicht("z1w1s1", "e", "z1w1s2", 0.01, 1500, 0.8, 1300, 6, 1, null, null, false, false);
            Schicht s2 = new Schicht("z1w1s2", "z1w1s1", "z1w1s3", 0.2, 1450, 0.04, 50, 6, 3, null, null, false, false);
            Schicht s3 = new Schicht("z1w1s3", "z1w1s2", "z1", 0.15, 1000, 2.3, 2300, 6, 1, null, null, false, false);

            Schicht s4 = new Schicht("z1w2s1", "z1", "z1", 0.15, 1000, 2.3, 2300, 15, 1, null, null, false, false);

            Schicht s5 = new Schicht("z1w3s1", "z1", "z1", 0.15, 1000, 2.3, 2300, 15, 1, null, null, false, false);

            Schicht s6 = new Schicht("z1w4s1", "z1", "z1", 0.15, 1000, 2.3, 2300, 15, 1, null, null, false, false);

            Schicht s7 = new Schicht("z1w5s1", "z1", "z1", 0.15, 1000, 2.3, 2300, 15, 1, null, null, true, false);

            Schicht s8 = new Schicht("z1w6s1", "z1", "z1", 0.15, 1000, 2.3, 2300, 15, 1, null, null, false, true);

            Fenster f1 = new Fenster("z1", 2, 1.5, 0.8, 0.5, 0.5);

            //mindestens 24 Werte
            //Infiltration: Gebäudevolumen*0,11
            Last l1 = new Last(0, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l2 = new Last(1, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l3 = new Last(2, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l4 = new Last(3, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l5 = new Last(4, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l6 = new Last(5, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);

            Last l7 = new Last(6, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l8 = new Last(7, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l9 = new Last(8, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l10 = new Last(9, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l11 = new Last(10, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l12 = new Last(11, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l13 = new Last(12, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l14 = new Last(13, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l15 = new Last(14, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l16 = new Last(15, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l17 = new Last(16, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);
            Last l18 = new Last(17, 0, new ParameterDouble("z1", 1000), null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), new ParameterDouble("z1", 100), new ParameterDouble("z1", 150), new ParameterDouble("z1", 0), null);

            Last l19 = new Last(18, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l20 = new Last(19, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l21 = new Last(20, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l22 = new Last(21, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l23 = new Last(22, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);
            Last l24 = new Last(23, 0, null, null, new ParameterDouble("z1w1s1", 0), null, new ParameterDouble("z1w1s1", 4.5), null, null, new ParameterDouble("z1", 0), null);

            Params Parameter = new Params(10, 25, 10000, 2.5, 5, 0.7, 5, 0.1, 0.8, 0.5, 1, 0.7, 0.5, 4182, 0.8, 0.25, 1, 24);

            List<Zone> zonen = new List<Zone>();
            zonen.Add(z1);
            List<Fenster> fenster = new List<Fenster>();
            fenster.Add(f1);
            List<Schicht> schichten = new List<Schicht>();
            schichten.Add(s1);
            schichten.Add(s2);
            schichten.Add(s3);
            schichten.Add(s4);
            schichten.Add(s5);
            schichten.Add(s6);
            schichten.Add(s7);
            schichten.Add(s8);
            List<Last> lasten = new List<Last>();
            lasten.Add(l1);
            lasten.Add(l2);
            lasten.Add(l3);
            lasten.Add(l4);
            lasten.Add(l5);
            lasten.Add(l6);
            lasten.Add(l7);
            lasten.Add(l8);
            lasten.Add(l9);
            lasten.Add(l10);
            lasten.Add(l11);
            lasten.Add(l12);
            lasten.Add(l13);
            lasten.Add(l14);
            lasten.Add(l15);
            lasten.Add(l16);
            lasten.Add(l17);
            lasten.Add(l18);
            lasten.Add(l19);
            lasten.Add(l20);
            lasten.Add(l21);
            lasten.Add(l22);
            lasten.Add(l23);
            lasten.Add(l24);

            MultizoneServ test = new MultizoneServ(zonen, schichten, fenster, lasten);
            String info;
            MultizoneServ.FailureMultizone f;
            test.executeMultizoneService(@"http://128.130.183.105:8001/calcShadow", out f, out info);

        }
    }
}
