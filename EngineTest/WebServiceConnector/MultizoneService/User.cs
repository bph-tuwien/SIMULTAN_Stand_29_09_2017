using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class User
    {
        private String user_id;
        private static Guid g;

        [JsonProperty(PropertyName = "userId")]
        public String User_id
        {
            get { return user_id; }
            set { user_id = value; }
        }

        public User()
        {
            if (g.ToString() == "00000000-0000-0000-0000-000000000000")
            {
                g = Guid.NewGuid();
            }
            user_id = g.ToString();
            
        }
    }
}