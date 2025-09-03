using System.Net;
using System.Net.Sockets;
using System.Text;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;
using LotComWatcher.Models.Extensions;

namespace LotComWatcher.Models.Services;

/// <summary>
/// Provides methods to communicate with Scanners over TCP network connections.
/// </summary>
public class NetworkService : INetworkService
{
    /// <summary>
    /// Sets the default communication port for sending messages to the Cognex Scanners.
    /// </summary>
    public int DefaultPort
    {
        get { return 23; }
        set { }
    }

    /// <summary>
    /// Pings an endpoint (generally a Scanner) for successful connection.
    /// </summary>
    /// <param name="EndPoint"></param>
    /// <returns>'true' if the Ping was able to connect successfully.</returns>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="SystemException"></exception>
    public async Task<bool> Ping(TcpClient Client, IPEndPoint EndPoint)
    {
        // attempt to connect to the EndPoint
        try
        {
            await Client.ConnectAsync(EndPoint);
        }
        // there was an error while accessing the Socket on the endpoint
        catch (SocketException)
        {
            throw new SocketException((int)SocketError.HostUnreachable);
        }
        // either the Host on the endpoint of the DefaultPort was invalid (null, out of range, etc.)
        catch (ArgumentException)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }
        // the TCP client was closed before or while the connection was established
        catch (ObjectDisposedException)
        {
            throw new SystemException("The TCP Client was closed unexpectedly. Cannot establish connection.");
        }
        // the operation's cancellation token was thrown internally
        catch (OperationCanceledException)
        {
            throw new SystemException("The operation terminated unexpectedly. Cannot establish connection.");
        }
        // connection was established without exceptions
        return true;
    }

    /// <summary>
    /// Attempts to establish a connection with a Scanner and send a message.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Message"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="SystemException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendMessage(IPAddress ScannerAddress, string Message)
    {
        TcpClient MessageClient = new TcpClient();
        // initialize a TCP endoint that connects to the targeted scanner
        IPEndPoint EndPoint = new IPEndPoint(ScannerAddress, DefaultPort);
        // ping the connection to the scanner to ensure messaging will occur
        bool Connected = false;
        try
        {
            Connected = await Ping(MessageClient, EndPoint);
        }
        // there was a connection error
        catch (SocketException _ex)
        {
            if (_ex.SocketErrorCode == SocketError.HostNotFound)
            {
                throw new ArgumentException("The requested Scanner address is unavailable.");
            }
            else if (_ex.SocketErrorCode == SocketError.HostUnreachable)
            {
                throw new ArgumentException("There is no Scanner at the requested address.");
            }
        }
        // there was an internal system error
        catch (SystemException)
        {
            throw;
        }
        // confirm connection
        if (!Connected)
        {
            throw new SystemException("The connection could not be established.");
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
    /// <exception cref="SystemException"></exception>
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
        catch (ArgumentException)
        {
            throw new SystemException("Could not establish a connection to the Scanner.");
        }
        catch (SystemException)
        {
            throw new SystemException("Failed to request a connection.");
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
    /// <exception cref="SystemException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendMissingPreviousScanError(IPAddress ScannerAddress, int Duration, IEnumerable<int> PreviousProcess)
    {
        // send Data Validation Failure and Send Alert DMCCs to the Scanner
        try
        {
            bool Sent = SendMessage(ScannerAddress, "||>OUTPUT.DATAVALID-FAIL\r\n").Result;
            if (Sent)
            {
                await SendMessage(ScannerAddress, $"||>UI.SEND-ALERT {Duration} 2 \"This Label was not scanned by {PreviousProcess.First()}. Basket is not valid for use.\"\r\n");
            }
        }
        catch (ArgumentException)
        {
            throw new SystemException("Could not establish a connection to the Scanner.");
        }
        catch (SystemException)
        {
            throw new SystemException("Failed to request a connection.");
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
    /// <exception cref="SystemException"></exception>
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
        catch (ArgumentException)
        {
            throw new SystemException("Could not establish a connection to the Scanner.");
        }
        catch (SystemException)
        {
            throw new SystemException("Failed to request a connection.");
        }
        return true;
    }

    /// <summary>
    /// Attempts to send an Invalid Part error code to ScannerAddress.
    /// Displays a notice on the Scanner's LCD screen for Duration seconds.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Duration"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="SystemException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> SendInvalidPartError(IPAddress ScannerAddress, int Duration)
    {
        // send Data Validation Failure and Send Alert DMCCs to the Scanner
        try
        {
            bool Sent = SendMessage(ScannerAddress, "||>OUTPUT.DATAVALID-FAIL\r\n").Result;
            if (Sent)
            {
                await SendMessage(ScannerAddress, $"||>UI.SEND-ALERT {Duration} 2 \"Invalid Part Label scanned.\"\r\n");
            }
        }
        catch (ArgumentException)
        {
            throw new SystemException("Could not establish a connection to the Scanner.");
        }
        catch (SystemException)
        {
            throw new SystemException("Failed to request a connection.");
        }
        return true;
    }

    /// <summary>
    /// Sends a Scan Validation Failure message to the Scanner that produced New.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="Duration"></param>
    /// <param name="Fault"></param>
    /// <returns></returns>
    /// <exception cref="SystemException"></exception>
    public async Task<bool> SendScanValidationError(ScanOutput New, int Duration, ValidationFailure Fault)
    {
        // send Data Validation Failure and Send Alert DMCCs to the Scanner
        try
        {
            bool Sent = await SendMessage(New.ScanAddress, "||>OUTPUT.DATAVALID-FAIL\r\n");
            if (Sent)
            {
                await SendMessage(New.ScanAddress, $"||>UI.SEND-ALERT {Duration} 2 \"{ValidationFailureExtensions.ToMessage(Fault)}\"\r\n");
            }
        }
        catch (ArgumentException)
        {
            throw new SystemException("Could not establish a connection to the Scanner.");
        }
        catch (SystemException)
        {
            throw new SystemException("Failed to request a connection.");
        }
        return true;
    }
}