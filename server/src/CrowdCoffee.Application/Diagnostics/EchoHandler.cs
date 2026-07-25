using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Diagnostics;

public class EchoHandler
{
    public Result<string> Handle(EchoRequest request) => Result.Success($"echo: {request.Message}");
}
