using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Helpers
{
    static class ObjectExtensions
    {
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
