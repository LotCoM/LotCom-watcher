using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LotComWatcher.Models.Services;

public sealed class NetworkService
{
    /// <summary>
    /// Sets the default communication port for sending messages to the Cognex Scanners.
    /// </summary>
    private const int DefaultPort = 23;

    /// <summary>
    /// Pings an endpoint (generally a Scanner) for successful connection.
    /// </summary>
    /// <param name="EndPoint"></param>
    /// <returns>'true' if the Ping was able to connect successfully.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="SystemException"></exception>
    private async Task<bool> Ping(TcpClient Client, IPEndPoint EndPoint)
    {
        // attempt to connect to the EndPoint
        try
        {
            await Client.ConnectAsync(EndPoint);
        }
        // there was an error while accessing the Socket on the endpoint
        catch (SocketException)
        {
            throw;
        }
        // either the Host on the endpoint of the DefaultPort was invalid (null, out of range, etc.)
        catch (ArgumentException)
        {
            throw new ArgumentException("The Host is null or the Port Number is invalid. Cannot establish connection.");
        }
        // the TCP client was closed before or while the connection was established
        catch (ObjectDisposedException)
        {
            throw new SystemException("The TCP Client was closed unexpectedly. Cannot establish connection.");
        }
        // the operation's cancellation token was thrown internally
        catch (OperationCanceledException)
        {
            throw;
        }
        // connection was established without exceptions
        return true;
    }

    /// <summary>
    /// Creates a NetworkService object that enables communication over TCP networks.
    /// </summary>
    public NetworkService()
    {
        
    }

    /// <summary>
    /// Attempts to establish a connection with a Scanner and send a message.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Message"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendMessage(IPAddress ScannerAddress, string Message)
    {
        TcpClient MessageClient = new TcpClient();
        // initialize a TCP endoint that connects to the targeted scanner
        IPEndPoint EndPoint = new IPEndPoint(ScannerAddress, DefaultPort);
        // ping the connection to the scanner to ensure messaging will occur
        if (!await Ping(MessageClient, EndPoint))
        {
            throw new HttpRequestException(HttpRequestError.ConnectionError);
        }
        // create the message stream and encode the string Message onto the stream
        NetworkStream MessageStream = MessageClient.GetStream();
        byte[] EncodedMessage = Encoding.UTF8.GetBytes(Message);
        try
        {
            await MessageStream.WriteAsync(EncodedMessage);
        }
        catch
        {
            throw new ArgumentException($"Could not encode Message '{Message}' into a NetworkStream.");
        }
        // message was sent successfully
        MessageStream.Close();
        MessageClient.Close();
        return true;
    }

    /// <summary>
    /// Attempts to send a Data Validation error code to ScannerAddress.
    /// Displays LCDText on the Scanner's LCD screen for Duration seconds.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="LCDText"></param>
    /// <param name="Duration"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendDataValidationError(IPAddress ScannerAddress, string LCDText, int Duration)
    {
        // send Data Validation Failure and Send Alert DMCCs to the Scanner
        try
        {
            bool Sent = SendMessage(ScannerAddress, "||>OUTPUT.DATAVALID-FAIL\r\n").Result;
            if (Sent)
            {
                await SendMessage(ScannerAddress, $"||>UI.SEND-ALERT {Duration} 2 \"{LCDText}\"\r\n");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        return true;
    }

    /// <summary>
    /// Attempts to send a Missing Previous Scan error code to ScannerAddress.
    /// Displays a notice on the Scanner's LCD screen for Duration seconds.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Duration"></param>
    /// <param name="PreviousProcess"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendMissingPreviousScanError(IPAddress ScannerAddress, int Duration, List<string> PreviousProcess)
    {
        // send Data Validation Failure and Send Alert DMCCs to the Scanner
        try
        {
            bool Sent = SendMessage(ScannerAddress, "||>OUTPUT.DATAVALID-FAIL\r\n").Result;
            if (Sent)
            {
                await SendMessage(ScannerAddress, $"||>UI.SEND-ALERT {Duration} 2 \"This Label was not scanned by {PreviousProcess[0]}. Basket is not valid for use.\"\r\n");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        return true;
    }

    /// <summary>
    /// Attempts to send a Duplicate Scan error code to ScannerAddress.
    /// Displays a notice on the Scanner's LCD screen for Duration seconds.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Duration"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendDuplicateScanError(IPAddress ScannerAddress, int Duration)
    {
        // send Data Validation Failure and Send Alert DMCCs to the Scanner
        try
        {
            bool Sent = SendMessage(ScannerAddress, "||>OUTPUT.DATAVALID-FAIL\r\n").Result;
            if (Sent)
            {
                await SendMessage(ScannerAddress, $"||>UI.SEND-ALERT {Duration} 2 \"Duplicate Label scanned.\"\r\n");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        return true;
    }
}