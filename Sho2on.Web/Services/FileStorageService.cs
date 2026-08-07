namespace Sho2on.Web.Services
{
    public class FileStorageService
    {
        private readonly string _basePath;

        public FileStorageService(IWebHostEnvironment env)
        {
            // برّا wwwroot تمامًا، جنب الـ Content Root
            _basePath = Path.Combine(env.ContentRootPath, "AppData", "Documents");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<(string StoredFileName, long FileSize)> SaveAsync(string subFolder, string originalFileName, Stream content)
        {
            var folder = Path.Combine(_basePath, subFolder);
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, storedFileName);

            using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await content.CopyToAsync(fileStream);
            }

            var fileInfo = new FileInfo(fullPath);
            return (Path.Combine(subFolder, storedFileName), fileInfo.Length);
        }

        public Stream OpenRead(string relativePath)
        {
            var fullPath = Path.Combine(_basePath, relativePath);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("الملف غير موجود");
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        }

        public void Delete(string relativePath)
        {
            var fullPath = Path.Combine(_basePath, relativePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}