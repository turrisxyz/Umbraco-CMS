using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Umbraco.Web.Mvc
{
    /// <summary>
    /// Custom JSON result using Newtonsoft.Json.
    /// </summary>
    public class JsonNetResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }

        public string ContentType { get; set; }

        public object Data { get; set; }

        public JsonSerializerSettings SerializerSettings { get; set; }

        public Formatting Formatting { get; set; } = Formatting.None;

        public static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new DefaultNamingStrategy()
            }
        };

        public JsonNetResult()
            : this(DefaultJsonSerializerSettings)
        { }

        public JsonNetResult(JsonSerializerSettings jsonSerializerSettings)
            => SerializerSettings = jsonSerializerSettings;

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            HttpResponseBase response = context.HttpContext.Response;

            response.ContentType = string.IsNullOrEmpty(ContentType) == false
              ? ContentType
              : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            if (Data != null)
            {
                using var writer = new JsonTextWriter(response.Output)
                {
                    Formatting = Formatting
                };

                var serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);
            }
        }
    }
}
