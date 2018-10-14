using System;
using System.IO;
using Newtonsoft.Json;

namespace IServerConnectorStandard
{
    public static class JsonHelper
    {
        public static T JsonObjFromFile<T>(string filename)
        {
            T deserializedObj; //=default(T);
            FileStream file = null;
            StreamReader reader = null;
            try
            {
                file = File.Open(filename, FileMode.Open);
                reader = new StreamReader(file);
                string jsonString = reader.ReadToEnd();
                deserializedObj = JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                if (file != null)
                {
                    file.Dispose();
                }
            }
            return deserializedObj;
        }


        public static T JsonObjFromString<T>(string input)
        {
            T deserializedObj;
            try
            {
                deserializedObj = JsonConvert.DeserializeObject<T>(input);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return deserializedObj;
        }

        public static bool JsonObjToFile<T>(string filename, T obj)
        {
            bool returnvalue = false;
            FileStream file = null;
            StreamWriter writer = null;
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                file = File.Open(filename, FileMode.Create);
                writer = new StreamWriter(file);
                writer.Write(json);
                writer.Flush();
                returnvalue = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
                if (file != null)
                {
                    file.Dispose();
                }
            }

            return returnvalue;
        }
    }
}