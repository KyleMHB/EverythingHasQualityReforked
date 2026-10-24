using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace QualityEverything
{
    // This boundary owns only reusable snapshots, never the game's Saved or Active settings.
    internal sealed class QualityPresetStore
    {
        private readonly string directory;
        private readonly Func<string, string> sanitize;
        public QualityPresetStore(string directory, Func<string, string> sanitize)
        { this.directory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); this.sanitize = sanitize; }
        string Resolve(string name)
        {
            string clean = sanitize(name ?? "");
            string stem = clean.Split('.')[0];
            if (string.IsNullOrWhiteSpace(clean) || clean != clean.Trim() || clean.EndsWith(".") || clean == "." || clean == ".." ||
                clean.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || clean.IndexOfAny(new[] {'/', '\\', ':'}) >= 0 ||
                new[] {"CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"}.Contains(stem, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a usable preset name.");
            string path = Path.GetFullPath(Path.Combine(directory, clean + ".xml"));
            if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Preset must stay in its folder.");
            return path;
        }
        string Existing(string path)
        {
            if (!Directory.Exists(directory)) return null;
            return Directory.GetFiles(directory, "*.xml").FirstOrDefault(p => string.Equals(Path.GetFileName(p), Path.GetFileName(path), StringComparison.OrdinalIgnoreCase));
        }
        public string[] List() => Directory.Exists(directory) ? Directory.GetFiles(directory, "*.xml").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray() : new string[0];
        public QualityConfiguration Load(string name)
        {
            string path = Resolve(name);
            string found = Existing(path);
            if (found == null) throw new FileNotFoundException("Preset was moved or deleted. Refresh the list.");
            return QualityConfiguration.ParseSnapshot(File.ReadAllText(found));
        }
        public void Save(string name, QualityConfiguration snapshot, string expected = null)
        {
            string xml = snapshot.Serialize();
            QualityConfiguration.ParseSnapshot(xml);
            string path = Resolve(name), existing = Existing(path);
            if (existing != null && (expected == null || File.ReadAllText(existing) != expected)) throw new IOException("That name already exists or changed. Review it before overwriting.");
            if (existing == null && expected != null) throw new IOException("Preset was moved or deleted. Refresh the list.");
            Directory.CreateDirectory(directory);
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temporary, xml);
                if (existing == null) File.Move(temporary, path);
                else
                {
                    if (File.ReadAllText(existing) != expected) throw new IOException("Preset changed. Review it before overwriting.");
                    File.Replace(temporary, existing, null);
                }
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        public string Read(string name)
        {
            string path = Existing(Resolve(name));
            if (path == null) throw new FileNotFoundException("Preset was moved or deleted. Refresh the list.");
            return File.ReadAllText(path);
        }
        public void Delete(string name, string expected)
        {
            string path = Existing(Resolve(name));
            if (path == null) throw new FileNotFoundException("Preset was moved or deleted. Refresh the list.");
            if (expected == null || File.ReadAllText(path) != expected) throw new IOException("Preset changed. Review it before deleting.");
            File.Delete(path);
        }
    }
}
