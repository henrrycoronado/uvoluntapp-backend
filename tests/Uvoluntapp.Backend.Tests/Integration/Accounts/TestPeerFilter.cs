using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

// TestServer has no network peer. Set the connection feature before the production pipeline.
internal sealed class TestPeerFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, continuation) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers["Test-Peer"].ToString());
            return continuation();
        });
        next(app);
    };
}
