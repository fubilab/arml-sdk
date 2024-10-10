using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ARML.Saving
{
    public class JsonDataService : IDataService
    {
        private const string KEY = "2/Tfw4ecZMdRoaPmkGlNsmgTnPwdtqFfhaEJ/5fy9Os=";
        private const string IV = "qa72WZb9yXTvGak784jflg==";

        public bool SaveData<T>(string path, T Data, bool Encrypted)
        {
            try
            {
                if (File.Exists(path))
                {
                    Debug.Log(string.Format("Data exists at {0}. Overwriting.", path));
                    File.Delete(path);
                }
                else
                {
                    Debug.Log(string.Format("Creating file at {0}.", path));
                }

                using FileStream stream = File.Create(path);
                if (Encrypted)
                {
                    WriteEncryptedData(Data, stream);
                }
                else
                {
                    stream.Close();
                    File.WriteAllText(path, JsonConvert.SerializeObject(Data));
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to save data due to: {e.Message} {e.StackTrace}");
                return false;
            }
        }

        private void WriteEncryptedData<T>(T Data, FileStream Stream)
        {
            using Aes aesProvider = Aes.Create();
            aesProvider.Key = Convert.FromBase64String(KEY);
            aesProvider.IV = Convert.FromBase64String(IV);
            using ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
            using CryptoStream cryptoStream = new CryptoStream(
                Stream,
                cryptoTransform,
                CryptoStreamMode.Write
                );

            //You can uncomment the below to see a generated value for the IV and Key.
            //Debug.Log($"Initialization Vector: {Convert.ToBase64String(aesProvider.IV)}");
            //Debug.Log($"Key: {Convert.ToBase64String(aesProvider.Key)}");
            cryptoStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(Data)));
        }

        public T LoadData<T>(string path, bool Encrypted)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Cannot load file at {path}. File does not exist.");
                throw new FileNotFoundException($"{path} does not exist.");
            }

            try
            {
                T data;
                if (Encrypted)
                {
                    data = ReadEncryptedData<T>(path);
                }
                else
                {
                    data = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load data due to: {e.Message} {e.StackTrace}");
                throw e;
            }
        }

        private T ReadEncryptedData<T>(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            using Aes aesProvider = Aes.Create();

            aesProvider.Key = Convert.FromBase64String(KEY);
            aesProvider.IV = Convert.FromBase64String(IV);

            using ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor(
                aesProvider.Key,
                aesProvider.IV
                );

            using MemoryStream decryptionStream = new MemoryStream(fileBytes);
            using CryptoStream cryptoStream = new CryptoStream(
                decryptionStream,
                cryptoTransform,
                CryptoStreamMode.Read
                );
            using StreamReader reader = new StreamReader(cryptoStream);

            string result = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}