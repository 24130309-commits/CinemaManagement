namespace CinemaManagement.Core.Interfaces;

public interface IBackupService
{
    void Backup(string destinationPath);
    void Restore(string sourcePath);
}