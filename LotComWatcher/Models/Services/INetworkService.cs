using System.Net;
using System.Net.Sockets;
using LotComWatcher.Models.Datatypes;
using LotComWatcher.Models.Enums;

namespace LotComWatcher.Models.Services;

public interface INetworkService
{
    /// <summary>
    /// Sets the default communication port for sending messages to the Cognex Scanners.
    /// </summary>
    int DefaultPort { get; set; }

    /// <summary>
    /// Pings an endpoint (generally a Scanner) for successful connection.
    /// </summary>
    /// <param name="EndPoint"></param>
    /// <returns>'true' if the Ping was able to connect successfully.</returns>
    /// <exception cref="SocketException"></exception>
    /// <exception cref="SystemException"></exception>
    Task<bool> Ping(TcpClient Client, IPEndPoint EndPoint);

    /// <summary>
    /// Attempts to establish a connection with a Scanner and send a message.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Message"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="SystemException"></exception>
    /// <exception cref="ArgumentException"></exception>
    Task<bool> SendMessage(IPAddress ScannerAddress, string Message);

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
    Task<bool> SendDataValidationError(IPAddress ScannerAddress, string LCDText, int Duration);

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
    Task<bool> SendMissingPreviousScanError(IPAddress ScannerAddress, int Duration, IEnumerable<int> PreviousProcess);

    /// <summary>
    /// Attempts to send a Duplicate Scan error code to ScannerAddress.
    /// Displays a notice on the Scanner's LCD screen for Duration seconds.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Duration"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="SystemException"></exception>
    /// <exception cref="ArgumentException"></exception>
    Task<bool> SendDuplicateScanError(IPAddress ScannerAddress, int Duration);

    /// <summary>
    /// Attempts to send an Invalid Part error code to ScannerAddress.
    /// Displays a notice on the Scanner's LCD screen for Duration seconds.
    /// </summary>
    /// <param name="ScannerAddress"></param>
    /// <param name="Duration"></param>
    /// <returns>'true' if Message was successfully sent to ScannerAddress.</returns>
    /// <exception cref="SystemException"></exception>
    /// <exception cref="ArgumentException"></exception>
    Task<bool> SendInvalidPartError(IPAddress ScannerAddress, int Duration);

    /// <summary>
    /// Sends a Scan Validation Failure message to the Scanner that produced New.
    /// </summary>
    /// <param name="New"></param>
    /// <param name="Duration"></param>
    /// <param name="Fault"></param>
    /// <returns></returns>
    /// <exception cref="SystemException"></exception>
    Task<bool> SendScanValidationError(ScanOutput New, int Duration, ValidationFailure Fault);
}