using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TMC.Workflows
{
    public class CallAzureFunction : BaseClass
    {
        [Input("Request Headers")]
        public InArgument<string> RequestHeaders { get; set; }

        [Input("Request Body")]
        public InArgument<string> RequestBody { get; set; }

        [RequiredArgument]
        [Input("Request Url")]
        public InArgument<string> RequestUrl { get; set; }

        [RequiredArgument]
        [Input("Request Method (GET, POST, PUT, DELETE, PATCH)")]
        public InArgument<string> RequestMethod { get; set; }

        [Output("Response Headers")]
        public OutArgument<string> ResponseHeaders { get; set; }

        [Output("Response Body")]
        public OutArgument<string> ResponseBody { get; set; }

        public override void ExecuteWorkflow()
        {
            tracingService.Trace($"Starting custom workflow {className}");

            string body = string.Empty;
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> response;
            RequestMethodEnum requestMethod;
            StringBuilder delimitedHeaders = new StringBuilder();

            try
            {
                // Check if there is a body to be sent
                if (!string.IsNullOrEmpty(RequestBody.Get<string>(context)))
                {
                    body = RequestBody.Get<string>(context);
                }

                tracingService.Trace($"body {body}");

                using (var client = new HttpClient())
                {
                    requestMethod = GetRequestMethod(RequestMethod.Get<string>(context));

                    tracingService.Trace($"requestMethod {requestMethod}");
                    tracingService.Trace($"Calling SendRequest...");

                    response = SendRequest(context, client, requestMethod, content);

                    tracingService.Trace($"SendRequest was called");

                    // Check if reponse was a success, otherwise thrown an exception
                    response.Result.EnsureSuccessStatusCode();

                    tracingService.Trace($"HTTP call was called successfully");

                    // Add a delimiter (;) for every header row returned
                    foreach (var header in response.Result.Headers)
                    {
                        if (delimitedHeaders.Length > 0)
                        {
                            delimitedHeaders.Append(";");
                        }

                        delimitedHeaders.Append($"{header.Key}:{header.Value}");
                    }

                    // Set Response Header output parameter
                    ResponseHeaders.Set(context, delimitedHeaders.ToString());

                    tracingService.Trace($"ResponseHeaders {ResponseHeaders.Get<string>(context)}");

                    // Set Response Body output parameter
                    var responseString = response.Result.Content.ReadAsStringAsync();
                    ResponseBody.Set(context, responseString.Result);

                    tracingService.Trace($"ResponseBody {ResponseBody.Get<string>(context)}");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Exception: {ex.Message}");
                tracingService.Trace($"StackTrace: {ex.StackTrace}");
                tracingService.Trace($"InnerException: {ex.InnerException}");

                // Set Response Header output parameter
                ResponseHeaders.Set(context, "HTTP call was failed");

                // Instead of throwing an error, sending back the response header with a texting indicating an error
                //throw new InvalidPluginExecutionException(ex.Message);
            }
            finally
            {
                tracingService.Trace($"ResponseHeaders {ResponseHeaders.Get<string>(context)}");
                tracingService.Trace($"ResponseBody {ResponseBody.Get<string>(context)}");
                tracingService.Trace($"Finished custom workflow {className}");
            }
        }

        private async Task<HttpResponseMessage> SendRequest(CodeActivityContext context, HttpClient client, RequestMethodEnum requestMethod, StringContent content)
        {
            switch (requestMethod)
            {
                case RequestMethodEnum.GET:
                    return await client.GetAsync(RequestUrl.Get<string>(context));
                case RequestMethodEnum.PATCH:
                    return await client.PatchAsync(RequestUrl.Get<string>(context), content);
                case RequestMethodEnum.PUT:
                    return await client.PutAsync(RequestUrl.Get<string>(context), content);
                case RequestMethodEnum.DELETE:
                    return await client.DeleteAsync(RequestUrl.Get<string>(context));
                case RequestMethodEnum.POST:
                    return await client.PostAsync(RequestUrl.Get<string>(context), content);
                default:
                    throw new InvalidPluginExecutionException("The Request Method supplied is not supported. Try using GET, PATCH, PUT, DELETE or POST");
            }
        }

        private RequestMethodEnum GetRequestMethod(string requestMethod)
        {
            switch (requestMethod)
            {
                case "GET":
                    return RequestMethodEnum.GET;
                case "POST":
                    return RequestMethodEnum.POST;
                case "PUT":
                    return RequestMethodEnum.PUT;
                case "DELETE":
                    return RequestMethodEnum.DELETE;
                case "PATCH":
                    return RequestMethodEnum.PATCH;
                default:
                    return RequestMethodEnum.DEFAULT;
            }
        }

        private enum RequestMethodEnum
        {
            GET = 0,
            POST = 1,
            PUT = 2,
            DELETE = 3,
            PATCH = 4,
            DEFAULT = 999
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUrl, HttpContent iContent)
        {
            Uri requestUri = new Uri(requestUrl);
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = iContent
            };

            HttpResponseMessage response = new HttpResponseMessage();
            return await client.SendAsync(request);
        }
    }
}