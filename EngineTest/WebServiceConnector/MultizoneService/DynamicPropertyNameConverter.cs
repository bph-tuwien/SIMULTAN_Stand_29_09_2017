using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class DynamicPropertyNameConverter : JsonConverter
    {
        private bool canwrite = true;
        
        public override bool CanConvert(Type objectType)
        {
            // CanConvert is not called if a [JsonConverter] attribute is used
            return false;
        }

       
        /// <summary>
        /// uses bool canwrite if it is possible to write a JsonObject
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return canwrite;
            }
        }
        /// <summary>
        /// Write Json Object in the right structure
        /// For "Last" setProperties is called to rename propertyNames
        /// </summary>
        /// <param name="writer">Json writer</param>
        /// <param name="value">Value of the object </param>
        /// <param name="serializer">JsonSerializer</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //important otherwise JObject.FromObject(value) causes a StackOverflow Error
            canwrite = false;
            //to get all information set in [JsonProperty]
            JObject jo = JObject.FromObject(value);

            if(value is Last){
                Last elem = (Last) value;
                setPropertiesLast(jo, elem);
            }
            
            // Write out the JSON
            jo.WriteTo(writer);
            canwrite = true;
        }

        /// <summary>
        /// Changing the property name to a dynamic name of the variables of the class "Last"
        /// if the attribute  is null the property will be removed in the JObject
        /// </summary>
        /// <param name="jo">JObject to write the properties</param>
        /// <param name="elem">Element where the properties should be changed</param>
        private void setPropertiesLast(JObject jo, Last elem)
        {
            JProperty prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "strahlung_")
                           .First();
            if (elem.Strahlung != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Strahlung.Name_id,
                                            elem.Strahlung.Value));
            }            
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "strahlung2_")
                           .First();
            if (elem.Strahlung2 != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Strahlung2.Name_id,
                                            elem.Strahlung2.Value));
            }
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "temp_")
                           .First();
            if (elem.Temperatur != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Temperatur.Name_id,
                                            elem.Temperatur.Value));
            }
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "rlt_")
                           .First();
            if (elem.Raumlueftung != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Raumlueftung.Name_id,
                                            elem.Raumlueftung.Value));
            }            
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "infiltration_")
                           .First();
            if (elem.Infiltration != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Infiltration.Name_id,
                                            elem.Infiltration.Value));
            }
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "ilastGer_")
                           .First();
            if (elem.IlastenGeraete != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.IlastenGeraete.Name_id,
                                            elem.IlastenGeraete.Value));
            }            
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "ilastPer_")
                           .First();
            if (elem.IlastenPersonen != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.IlastenPersonen.Name_id,
                                            elem.IlastenPersonen.Value));
            }
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "lueftungsliste_")
                           .First();
            if (elem.Lueftung != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Lueftung.Name_id,
                                            elem.Lueftung.Value));
            }
            prop.Remove();
            prop = jo.Children<JProperty>()
                           .Where(p => p.Name == "wwbVol_")
                           .First();
            if (elem.Warmwasseverbrauch != null)
            {
                prop.AddAfterSelf(new JProperty(prop.Name + elem.Warmwasseverbrauch.Name_id,
                                            elem.Warmwasseverbrauch.Value));
            }            
            prop.Remove();

        }

        /// <summary>
        /// Always false because the converter is not called for deserialization
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Not implemeneted: no deserialization needed in this converter
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // ReadJson is not called if CanRead returns false.
            throw new NotImplementedException();
        }

    }
}
