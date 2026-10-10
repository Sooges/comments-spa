namespace CommentsApi.Services;

public interface IFileStorage
{
    /// <summary>
    /// Saves the file under a new generated name and returns this name.
    /// </summary>
    Task<string> SaveAsync(byte[] content, string extension);

    /// <summary>
    /// Opens a saved file for reading. Returns null if there is no such file.
    /// </summary>
    Stream? Open(string storedFileName);

    void Delete(string storedFileName);
}
