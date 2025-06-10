namespace LotComWatcher.Models.Exceptions;

/// <summary>
/// Special exception type to indicate an error accessing the Scan Output file.
/// </summary>
/// <param name="Message"></param>
public sealed class OutputFileAccessException(string Message) : Exception(message: Message)
{

}