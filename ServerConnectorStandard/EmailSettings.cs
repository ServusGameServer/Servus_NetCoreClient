namespace IServerConnectorStandard
{
    public class EmailSettings
    {
        public bool Autologin { get; set; }
        public long ServerUserid { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }
        public string Nickname { get; set; }
        public bool AutoLoginAllowed { get; set; }
        [Newtonsoft.Json.JsonConstructor]
        public EmailSettings()
        {
            Autologin = false;
            ServerUserid = -1;
            Email = "";
            Password = "";
            Nickname = "";
            AutoLoginAllowed = false;
        }
        public static string GetMd5Hash(string pw)
        {
            if(string.IsNullOrEmpty(pw))
            {
                return string.Empty;
            }
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pw));
            System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();
            foreach (var t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }
            return sBuilder.ToString();
        } 
        public static EmailSettings LoadSettingsFromFile(string filename)
        {
            return JsonHelper.JsonObjFromFile<EmailSettings>(filename);
        }
        public static void SaveSettingsFromFile(string filename,EmailSettings obj)
        {
            JsonHelper.JsonObjToFile<EmailSettings>(filename,obj);
        }
    }
}
