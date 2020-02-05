using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class MultizoneServ
    {
        public enum FailureMultizone
        {
            No,
            Request,
            ResponseConvert
        };

        private List<Zone> zonen;
        /// <summary>
        /// List of all zones
        /// </summary>
        [JsonProperty(PropertyName = "Zonen", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Zone> Zonen
        {
            get { return zonen; }
            set { zonen = value; }
        }

        private List<Schicht> schichten;
        /// <summary>
        /// List of all layers of walls
        /// </summary>
        [JsonProperty(PropertyName = "Waende", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Schicht> Schichten
        {
            get { return schichten; }
            set { schichten = value; }
        }

        private List<Fenster> fenster;
        /// <summary>
        /// List of all windows
        /// </summary>
        [JsonProperty(PropertyName = "Fenster", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Fenster> Fenster
        {
            get { return fenster; }
            set { fenster = value; }
        }

        private List<Last> lasten;
        /// <summary>
        /// List of all loads
        /// </summary>
        [JsonProperty(PropertyName = "Lasten", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Last> Lasten
        {
            get { return lasten; }
            set { lasten = value; }
        }

        private Params parameter;
        /// <summary>
        /// Instructions for simulation
        /// </summary>
        [JsonProperty(PropertyName = "Params", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public Params Parameter
        {
            get { return parameter; }
            set { parameter = value; }
        }

        private List<Speicher> speicher;
        /// <summary>
        /// List of all heat storages
        /// </summary>
        [JsonProperty(PropertyName = "Speicher", ObjectCreationHandling = ObjectCreationHandling.Reuse)]
        public List<Speicher> Speicher
        {
            get { return speicher; }
            set { speicher = value; }
        }

        private User user;

        public User User
        {
            get { return user; }
            set { user = value; }
        }
        
        /// <summary>
        /// Full constructor for the multizone service
        /// </summary>
        public MultizoneServ(List<Zone> zonen, List<Schicht> schichten, List<Fenster> fenster=null, List<Last> lasten=null, List<Speicher> speicher =null)
        {
            User = new User();
            Parameter = new Params();
            Zonen = new List<Zone>();
            Zonen.AddRange(zonen);
            Schichten = new List<Schicht>();
            Schichten.AddRange(schichten);
            Fenster= new List<Fenster>();
            if (fenster != null)
            {
                Fenster.AddRange(fenster);
            }
            Lasten = new List<Last>();
            if (lasten != null)
            {
                Lasten.AddRange(lasten);
            }
            Speicher = new List<Speicher>();
            if (speicher != null)
            {
                Speicher.AddRange(speicher);
            }
            
        }

        /// <returns></returns>
        /// <summary>
        /// Execution of the multizone service
        /// </summary>
        /// <param name="uricode">URI of the multizone service</param>
        /// <param name="failure">out parameter: NO if no failure occurs; otherwise: ResponseConvert</param>
        /// <param name="info">out parameter: Information regarding failure</param>
        public void executeMultizoneService(String uricode, out FailureMultizone failure, out String info)
        {
            Uri geocodeRequest = new Uri(string.Format(uricode));
            String response = SendRequest(geocodeRequest, generateJson(), out failure, out info);
            if (failure == FailureMultizone.No)
            {
                generateResult(response, out failure, out info);
            }
        }

        /// <summary>
        /// Generates the json String out of the objects
        /// </summary>
        /// <returns>Json String for multizone service</returns>
        private String generateJson()
        {
            //this for the whole service
            
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Formatting = Formatting.Indented;
            var json = JsonConvert.SerializeObject(this, settings);
            Console.WriteLine(json);
            return json;
        }


        /// <summary>
        /// Post Request to the multizone service
        /// </summary>
        /// <param name="uri">URI of the service</param>
        /// <param name="data">Json String</param>
        /// <param name="fail">out parameter: NO if no failure occurs; otherwise: ResponseConvert</param>
        /// <param name="information">out parameter: Information regarding failure</param>
        /// <returns>Response of the server</returns>
        private String SendRequest(Uri uri, string data, out FailureMultizone fail, out String information)
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
                    fail = FailureMultizone.No;
                    information = string.Format("Status Code: {0}, Status Description: {1}", resp.StatusCode, resp.StatusDescription);
                    return sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                fail = FailureMultizone.Request;
                information = e.Message;
                return "";
            }

        }

        /// <summary>
        /// Use the response of the server for the multizones
        /// </summary>
        /// <param name="json">Json Response of the server</param>
        /// <param name="fail">out parameter: NO if no failure occurs; otherwise: ResponseConvert</param>
        /// <param name="information">out parameter: Information regarding failure</param>
        /// <returns>List of results</returns>
        private List<Fenster> generateResult(String json, out FailureMultizone fail, out String information)
        {
            try
            {
                if (!json.Equals(""))
                {
                    List<Fenster> ergebnis = new List<Fenster>();
                    fail = FailureMultizone.No;
                    information = "Converting successfully";
                    return ergebnis = JsonConvert.DeserializeObject<List<Fenster>>(json);
                }
                fail = FailureMultizone.ResponseConvert;
                information = "Nothing to convert!";
                return null;
            }
            catch (Exception e)
            {
                fail = FailureMultizone.ResponseConvert;
                information = e.Message;
                return null;
            }


        }

    }
}
