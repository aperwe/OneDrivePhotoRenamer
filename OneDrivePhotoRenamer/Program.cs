using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace OneDrivePhotoRenamer
{
    class Program
    {
        //Typical file name by onedrive and new name pattern.
        //20190420_114155.jpg => 2019-04-20 11.41.55.jpg
        //20190414_104305.mp4 => 2019-04-14 10.43.05.mp4
        static void Main(string[] args)
        {

            if (args.Length < 1)
            {
                Console.WriteLine("Too few arguments.");
                Console.WriteLine("Recommmended syntax:");
                Console.WriteLine("PhotoFileRenamer <dir> [<SearchPattern>]");
                return;
            }
            string dir = args[0]; //Directory to process
            string pattern = "*.jpg"; //Default pattern
            //Handle optional pattern
            if (args.Length > 1)
            {
                pattern = args[1];
            }
            Console.WriteLine("Processing {0} files in {1} directory", pattern, dir);
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Specified directory {0} does not exist.", dir);
                return;
            }
            DirectoryInfo di = new DirectoryInfo(dir);
            FileInfo[] files = di.GetFiles(pattern, SearchOption.TopDirectoryOnly);
            Console.WriteLine("Found {0} files.", files.Length);

            //Dictionary of <original name>, <new name>
            Dictionary<string, string> renames = new Dictionary<string, string>();
            int pictureIndex = 1; //Index names from 1
            #region Test
            string inputName = "20190420_114155.jpg";
            Regex rPattern = new Regex(@"(?'year'^\d{4})(?'month'\d{2})(?'day'\d{2})_(?'hh'\d{2})(?'mm'\d{2})(?'ss'\d{2})\s*(?'remainder'.*\Z)");
            var match = rPattern.Match(inputName);
            string newName = string.Format("{0}-{1}-{2} {3}.{4}.{5}",
                match.Groups["year"],
                match.Groups["month"],
                match.Groups["day"],
                match.Groups["hh"],
                match.Groups["mm"],
                match.Groups["ss"]);
            #endregion

            foreach (FileInfo file in files)
            {
                var nameNoExtension = new string(file.Name.Take(file.Name.Length - file.Extension.Length).ToArray());
                match = rPattern.Match(nameNoExtension);
                //newName = string.Format(nameFormat, pictureIndex, file.Extension.ToLower());
                if (match.Groups["year"].Length != 4)
                {
                    Console.WriteLine("Skipping {0} because it does not follow OneDrive photo name pattern.", file.Name);
                    continue;
                }
                newName = string.Format("{0}-{1}-{2} {3}.{4}.{5}",
                        match.Groups["year"],
                        match.Groups["month"],
                        match.Groups["day"],
                        match.Groups["hh"],
                        match.Groups["mm"],
                        match.Groups["ss"]);
                //Handle remainder, if one exists.
                var remainder = match.Groups["remainder"];
                if (remainder.Length > 0)
                {
                    newName = string.Format("{0} {1}", newName, remainder);
                }
                //Add extension
                newName = string.Format("{0}{1}", newName, file.Extension.ToLower());

                string newFullName = string.Format("{0}\\{1}", file.DirectoryName, newName);
                renames.Add(file.FullName, newFullName);
                Console.WriteLine("{0} ==> {1}", file.Name, newName);
                pictureIndex++;
            }
            Console.WriteLine("If you want to proceed with the above rename operation, enter \"y\"<Enter>.");
            Console.WriteLine("Any other string to cancel.");
            Console.Write("Are you sure (y/n)?: ");
            string decision = Console.In.ReadLine().ToLower();
            if (!decision.Equals("y"))
            {
                Console.WriteLine("Operation cancelled. No changes to file names have been made.");
                return;
            }
            //We have the user's agreement to proceed
            string myNamespace = "arturp";
            XmlDocument renameLog = new XmlDocument();
            XmlDeclaration declaration = renameLog.CreateXmlDeclaration("1.0", Encoding.UTF8.WebName, "yes");
            renameLog.AppendChild(declaration);
            XmlNode rootXmlNode = renameLog.CreateNode(XmlNodeType.Element, "OneDrivePhotoRenamer", myNamespace);
            renameLog.AppendChild(rootXmlNode);
            XmlNode renamesNode = renameLog.CreateNode(XmlNodeType.Element, "renames", myNamespace);
            rootXmlNode.AppendChild(renamesNode);
            foreach (string oldFile in renames.Keys)
            {
                if (renames.TryGetValue(oldFile, out string newFile))
                {
                    XmlNode renamedFileXmlNode = renameLog.CreateNode(XmlNodeType.Element, "renamedFile", myNamespace);
                    XmlAttribute attribute = renameLog.CreateAttribute("oldFile");
                    attribute.Value = Path.GetFileName(oldFile);
                    renamedFileXmlNode.Attributes.Append(attribute);
                    attribute = renameLog.CreateAttribute("newFile");
                    attribute.Value = Path.GetFileName(newFile);
                    renamedFileXmlNode.Attributes.Append(attribute);
                    File.Move(oldFile, newFile);
                    renamesNode.AppendChild(renamedFileXmlNode);
                }
                else
                {
                    Console.WriteLine("Name lookup error! Should never happen.");
                }
            }
            string renameLogFile = Path.Combine(dir, "OneDrivePhotoRenamer.xml");
            renameLog.Save(renameLogFile);
        }
    }
}
