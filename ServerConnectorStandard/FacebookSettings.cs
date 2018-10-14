using System.Collections.Generic;

namespace IServerConnectorStandard
{
    public class FacebookSettings
    {
        public bool Autologin { get; set; }
        public List<string> FbPerms { get; set; }
        public string ServerUserid { get; set; }
        public string FbUserid { get; set; }
        public string FbToken { get; set; }
		public bool WithProfilePicture { get; set; }
        [Newtonsoft.Json.JsonConstructor]
        public FacebookSettings()
        {
            Autologin = true;
            FbPerms = new List<string>();
            ServerUserid = "";
            FbUserid = "";
            FbToken = "";
			WithProfilePicture = false;
        }

        public static FacebookSettings LoadSettingsFromFile(string filename)
        {
            return JsonHelper.JsonObjFromFile<FacebookSettings>(filename);
        }
        public static void SaveSettingsFromFile(string filename,FacebookSettings obj)
        {
            JsonHelper.JsonObjToFile<FacebookSettings>(filename,obj);
        }
    }
}