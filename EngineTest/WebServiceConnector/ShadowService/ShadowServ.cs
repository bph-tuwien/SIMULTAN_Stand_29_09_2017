using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Media.Media3D;

namespace WebServiceConnector.ShadowService
{
    /// <summary>
    /// Class representing the Shadow Service
    /// </summary>
    public class ShadowServ
    {
        public enum FailureShadow
        {
            No,
            Request,
            ResponseConvert
        };

        private List<Surface> polys;
        /// <summary>
        /// List of all polygons for the shadow service
        /// </summary>
        [JsonProperty(PropertyName = "polys", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Surface> Polys
        {
            get { return polys; }
            set { polys = value; }
        }

        private PointVS sun;
        /// <summary>
        /// 3D Vector representing the solar beam
        /// </summary>
        [JsonProperty(PropertyName = "sonne")]
        public PointVS Sun
        {
            get { return sun; }
            set { sun = value; }
        }

        /// <summary>
        /// Default constructor:
        /// List of polygons is empty
        /// sun vector: 0 0 0
        /// </summary>
        public ShadowServ()
        {
            sun = new PointVS(new Vector3D(0, 0, 0));
            polys = new List<Surface>();

        }
        /// <summary>
        /// Full constructor for the shadow service
        /// </summary>
        /// <param name="sun">3D Vector representing the solar beam (x,y,z)</param>
        /// <param name="surfaces">List of polygons</param>
        public ShadowServ(Vector3D sun, List<Surface> surfaces)
        {
            Sun = new PointVS(sun);
            polys = new List<Surface>();
            polys.AddRange(surfaces);

        }

        /// <summary>
        /// Changing the sun vector
        /// </summary>
        /// <param name="sunVector">3D Vector representing the solar beam (x,y,z)</param>
        public void changeSun(Vector3D sunVector)
        {
            sun.change(sunVector);
        }

        /// <summary>
        /// Changing the surfaces and openings
        /// </summary>
        /// <param name="surfaces">List of polygons</param>
        public void changeSurfaces(List<Surface> surfaces)
        {
            polys = surfaces;
        }

        /// <returns></returns>
        /// <summary>
        /// Execution of the shadow service
        /// </summary>
        /// <param name="uricode">URI of the shadow service</param>
        /// <param name="failure">out parameter: NO if no failure occurs; otherwise: ResponseConvert</param>
        /// <param name="info">out parameter: Information regarding failure</param>
        /// <returns>List of values (id, svf (visibility of sky), and shadow (shading factor))</returns>
        public List<ShadowResult> executeShadowService(String uricode, out FailureShadow failure, out String info)
        {
            //Uri geocodeRequest = new Uri(string.Format("http://pc5.bph.tuwien.ac.at:8001/calcShadow"));
            //Uri geocodeRequest = new Uri(string.Format("http://128.130.183.105:8001/calcShadow"));
            Uri geocodeRequest = new Uri(string.Format(uricode));
            String response = SendRequest(geocodeRequest, generateJson(), out failure, out info);
            if (failure == FailureShadow.No)
            {
                return generateResult(response, out failure, out info);
            }
            return null;
        }

        /// <summary>
        /// Generates the json String out of the objects
        /// </summary>
        /// <returns>Json String for shadow service</returns>
        private String generateJson()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            //Console.WriteLine(json);
            return json;
        }


        /// <summary>
        /// Post Request to the shadow service
        /// </summary>
        /// <param name="uri">URI of the service</param>
        /// <param name="data">Json String</param>
        /// <param name="fail">out parameter: NO if no failure occurs; otherwise: ResponseConvert</param>
        /// <param name="information">out parameter: Information regarding failure</param>
        /// <returns>Response of the server</returns>
        private String SendRequest(Uri uri, string data, out FailureShadow fail, out String information)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);

                request.Method = "POST";
                request.ContentType = "application/json"; // json

                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] bytes = encoding.GetBytes(data);

                request.ContentLength = bytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    // Send the data.
                    requestStream.Write(bytes, 0, bytes.Length);
                }

                HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
                using (var sr = new StreamReader(resp.GetResponseStream()))
                {
                    fail = FailureShadow.No;
                    information = string.Format("Status Code: {0}, Status Description: {1}", resp.StatusCode, resp.StatusDescription);
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                fail = FailureShadow.Request;
                information = e.Message;
                return "";
            }

        }

        /// <summary>
        /// Use the response of the server for the svf and shading factor
        /// </summary>
        /// <param name="json">Json Response of the server</param>
        /// <param name="fail">out parameter: NO if no failure occurs; otherwise: ResponseConvert</param>
        /// <param name="information">out parameter: Information regarding failure</param>
        /// <returns>List of results (id, svf (visibility of sky), and shadow (shading factor)) </returns>
        private List<ShadowResult> generateResult(String json, out FailureShadow fail, out String information)
        {
            try
            {
                if (!json.Equals(""))
                {
                    List<ShadowResult> ergebnis = new List<ShadowResult>();
                    fail = FailureShadow.No;
                    information = "Converting successfully";
                    return ergebnis = JsonConvert.DeserializeObject<List<ShadowResult>>(json);
                }
                fail = FailureShadow.ResponseConvert;
                information = "Nothing to convert!";
                return null;
            }
            catch (Exception e)
            {
                fail = FailureShadow.ResponseConvert;
                information = e.Message;
                return null;
            }


        }

    }
}
