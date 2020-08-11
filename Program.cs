using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ExifDateExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            var workingPath = Environment.CurrentDirectory;
            if (args.Length == 1) {
                workingPath = args[0];
            }

            Console.WriteLine($"Recherche des images à traiter dans le répertoire \"{workingPath}\" ...");

			var imagesDirectory = new DirectoryInfo(workingPath);
            var images = GetImages(imagesDirectory);

            Console.WriteLine($"Images à traiter : {images.Count}");

            var nbProcessed = 0;
            var nbErrors = 0;
            foreach(var image in images)
            {
                if (!TryFixLastWriteTime(image))
                {
                    nbErrors++;
                }

                nbProcessed++;

                if (nbProcessed % 100 == 0)
                {
                    var percent = Convert.ToDecimal(nbProcessed) / Convert.ToDecimal(images.Count);
                    Console.WriteLine($"{percent:P} des images traitées ({nbProcessed} avec {nbErrors} erreur)");
                }
            }

            Console.WriteLine($"Traitement terminé avec {nbErrors} erreur(s).");
        }
        
        static IReadOnlyCollection<FileInfo> GetImages(DirectoryInfo directory)
        {
            var images = directory.GetFiles("*.jpg").ToList();
            var subDirectories = directory.GetDirectories();

            foreach(var subdirectory in subDirectories)
            {
                var subImages = GetImages(subdirectory);
                images.AddRange(subImages);
            }

            return images;
        }

        static bool TryFixLastWriteTime(FileInfo imagePath)
        {
            try
            {
                var metadata = ImageMetadataReader.ReadMetadata(imagePath.FullName);
                var exifMetadata = metadata.OfType<ExifIfd0Directory>().FirstOrDefault();
                
                if (exifMetadata != null && exifMetadata.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var datetime))
                {
                    imagePath.LastWriteTime = datetime;
                    return true;
                };
            }
            catch
            {
            }

            return false;
        }
    }
}
