using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace ProxyKit
{
    internal static class HttpContextExtensions
    {
        internal static HttpRequestMessage CreateProxyHttpRequest(this HttpRequest request)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers *except* x-forwarded-* headers.
            foreach (var header in request.Headers)
            {
                if(header.Key.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                // Removes malformed User-Agent that causes
                // IndexOutOfRangeException when sending the upstream request
                // https://github.com/damianh/ProxyKit/issues/53
                // https://github.com/dotnet/corefx/issues/34933 
                // Probably / hopefully fixed in netcore3. 
                try
                {
                    requestMessage.Headers.TryGetValues("User-Agent", out var _);
                }
                catch (IndexOutOfRangeException)
                {
                    request.Headers.Remove("User-Agent");
                }
            }

            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }
    }
}
