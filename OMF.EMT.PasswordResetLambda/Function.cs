using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text;

using Amazon.Lambda.Core;
//using Newtonsoft.Json;

using OMF.Amazon.Lambda.LexV2Events;
using OMF.Amazon.Lambda.LexV2FlowControl;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OMF.EMT.PasswordResetLambda
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        //public static LexV2Response FunctionHandler(LexV2Event lexEvent, ILambdaContext context)
        public static LexV2Response FunctionHandler(LexV2Event lexEvent, ILambdaContext context)
        {
            //var x = new System.Text.Json.Serialization.JsonConverter();

            LambdaLogger.Log("lexEvent: " + ((lexEvent == null) ? "null" : JsonSerialize(lexEvent)));
            LambdaLogger.Log("context: " + ((context == null) ? "null" : JsonSerialize(context)));

            var response = IntentHandler.HandleEvent(lexEvent, context);


            LambdaLogger.Log("response: " + JsonSerialize(response));
            //LambdaLogger.Log("response: " + JsonSerializer.Serialize(response));

            return response;
        }

        public static string JsonSerialize(object obj)
        {
            //var json = new StringBuilder();
            //var writer = new StringWriter(json);

            //var serializer = new JsonSerializer();
            //serializer.Serialize(writer, obj);

            //return json.ToString();

            return JsonSerializer.Serialize(obj);
        }
    }
}
