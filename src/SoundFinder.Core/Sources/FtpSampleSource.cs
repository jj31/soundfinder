using System.Net;
using SoundFinder.Core.Models;

namespace SoundFinder.Core.Sources;

public sealed class FtpSampleSource : ISampleSource
{
    private readonly Uri _baseUri;
    private readonly NetworkCredential _credentials;

    public FtpSampleSource(string baseUri, string userName, string password)
    {
        _baseUri = new Uri(baseUri.EndsWith('/') ? baseUri : baseUri + "/");
        _credentials = new NetworkCredential(userName, password);
    }

    public SourceType Type => SourceType.Ftp;

    public IEnumerable<SourceFile> EnumerateFiles()
    {
        var files = new List<SourceFile>();
        Collect(_baseUri.AbsolutePath.TrimStart('/'), files);
        return files;
    }

    private void Collect(string remoteDir, List<SourceFile> output)
    {
        foreach (var entry in ListDirectory(remoteDir))
        {
            var childPath = remoteDir + "/" + entry.Name;
            if (entry.IsDirectory)
            {
                Collect(childPath, output);
            }
            else if (LocalFileSource.IsAudioFile(entry.Name))
            {
                output.Add(new SourceFile
                {
                    Path = childPath,
                    Length = entry.Size,
                    LastWriteTimeUtc = entry.ModifiedUtc
                });
            }
        }
    }

    private List<(string Name, bool IsDirectory, long Size, DateTime ModifiedUtc)> ListDirectory(string remoteDir)
    {
        var request = CreateRequest(remoteDir, WebRequestMethods.Ftp.ListDirectoryDetails);
        using var response = (FtpWebResponse)request.GetResponse();
        using var stream = response.GetResponseStream();
        using var reader = new StreamReader(stream!);
        var result = new List<(string, bool, long, DateTime)>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9)
            {
                continue;
            }
            var isDirectory = parts[0].StartsWith('d');
            var name = string.Join(" ", parts.Skip(8));
            if (name is "." or "..")
            {
                continue;
            }
            long size = 0;
            long.TryParse(parts[4], out size);
            var modified = DateTime.TryParse(parts[5] + " " + parts[6] + " " + parts[7], out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow;
            result.Add((name, isDirectory, size, modified));
        }
        return result;
    }

    public Stream OpenRead(string path)
    {
        var request = CreateRequest(path, WebRequestMethods.Ftp.DownloadFile);
        var response = (FtpWebResponse)request.GetResponse();
        return new FtpResponseStream(response, response.GetResponseStream()!);
    }

    public bool FileExists(string path)
    {
        try
        {
            var request = CreateRequest(path, WebRequestMethods.Ftp.GetFileSize);
            using var response = (FtpWebResponse)request.GetResponse();
            return response.ContentLength >= 0;
        }
        catch (WebException)
        {
            return false;
        }
    }

    private FtpWebRequest CreateRequest(string remotePath, string method)
    {
        var uri = new Uri(_baseUri, Uri.EscapeUriString(remotePath).Replace("%2F", "/"));
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = method;
        request.Credentials = _credentials;
        request.UseBinary = true;
        request.UsePassive = true;
        return request;
    }

    private sealed class FtpResponseStream : Stream
    {
        private readonly FtpWebResponse _response;
        private readonly Stream _inner;
        public FtpResponseStream(FtpWebResponse response, Stream inner)
        {
            _response = response;
            _inner = inner;
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _response.ContentLength;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
