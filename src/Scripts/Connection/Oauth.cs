using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace TD.Connection;

public abstract class Oauth
{
    private static async void KillAfter(HttpListener listener, int delayMs)
    {
        await Task.Delay(delayMs);

        if (listener.IsListening)
            listener.Stop();
    }

    public static async Task<string> Open(string url, string successUrl, string headerName)
    {
        TcpListener tcpListener = new(IPAddress.Loopback, 0);
        tcpListener.Start();
        int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();

        string localListenerUrl = $"http://localhost:{port}/";
        HttpListener listener = new();
        listener.Prefixes.Add(localListenerUrl);

        KillAfter(listener, 60 * 3 * 1000);

        listener.Start();

        OS.ShellOpen($"{url}?port={port}");

        HttpListenerContext context = await listener.GetContextAsync();

        string code = context.Request.QueryString[headerName];

        if (!string.IsNullOrEmpty(code))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.Response.RedirectLocation = successUrl;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            byte[] responseBuffer = Encoding.UTF8.GetBytes("Token not present!");
            context.Response.ContentLength64 = responseBuffer.Length;
            context.Response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
        }

        context.Response.Close();

        await Task.Delay(3000);

        listener.Close();

        return code;
    }
}