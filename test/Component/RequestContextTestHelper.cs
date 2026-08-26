using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace MCPServerPS.Tests.Component;

// Builds a RequestContext<CallToolRequestParams> without a running server/transport, the same
// pattern the ModelContextProtocol SDK's own tests use: new Mock<McpServer>().Object.
internal static class RequestContextTestHelper
{
    internal static RequestContext<CallToolRequestParams> CreateRequest(string toolName, IDictionary<string, object?>? arguments = null)
    {
        Dictionary<string, JsonElement>? argDict = null;
        if (arguments is { })
        {
            argDict = arguments.ToDictionary(kvp => kvp.Key, kvp => JsonSerializer.SerializeToElement(kvp.Value));
        }

        var callParams = new CallToolRequestParams { Name = toolName, Arguments = argDict };
        var jsonRpcRequest = new JsonRpcRequest { Method = "tools/call", Id = new RequestId("test") };

        return new RequestContext<CallToolRequestParams>(new Mock<McpServer>().Object, jsonRpcRequest, callParams);
    }
}
