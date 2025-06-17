namespace LotComWatcher.Models.Exceptions;

/// <summary>
/// Special exception type to indicate an error adding an entry to a Table within the Database.
/// </summary>
/// <param name="Message"></param>
public class InsertionException(string Message, Exception? InnerException = null) : DatabaseException(Message: Message, InnerException: InnerException)
{

}