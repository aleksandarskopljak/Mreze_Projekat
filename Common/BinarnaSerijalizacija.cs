using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Common
{
    public static class Serijalizator
    {
        public static byte[] Serijalizuj<T>(T objekat)
        {
            if (objekat == null)
                return null;

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, objekat);
                return stream.ToArray();
            }
        }

        public static T Deserijalizuj<T>(byte[] podaci)
        {
            if (podaci == null || podaci.Length == 0)
                return default(T);

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(podaci))
            {
                return (T)formatter.Deserialize(stream);
            }
        }

        public static string SerijalizujUString<T>(T objekat)
        {
            byte[] podaci = Serijalizuj(objekat);
            return Convert.ToBase64String(podaci);
        }

        public static T DeserijalizujIzStringa<T>(string tekst)
        {
            byte[] podaci = Convert.FromBase64String(tekst);
            return Deserijalizuj<T>(podaci);
        }
    }
}
