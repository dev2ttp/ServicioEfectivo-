using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TPpagoL2.Helpers
{
    static class ObjectExtensions
    {
        /// <summary>
        /// Serializes an object to JSON.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string.</returns>
        public static string ToJson(this object value)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return JsonConvert.SerializeObject(value, Formatting.None, settings);
        }

        /// <summary>
        /// Clones an object via deep copy.
        /// </summary>
        /// <typeparam name="T">The type of the list to copy.</typeparam>
        /// <param name="list">The list to clone.</param>
        /// <returns>A clone of the list.</returns>
        public static List<T> DeepClone<T>(this List<T> list)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, list);
                stream.Position = 0;
                return (List<T>)formatter.Deserialize(stream);
            }
        }
    }
}
