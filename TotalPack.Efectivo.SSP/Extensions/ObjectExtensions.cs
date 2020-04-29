using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalPack.Efectivo.SSP.Extensions
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
    }
}
