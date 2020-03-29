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
            #region Test
            {
                string inputName = "20190420_114155.jpg";
                Regex rPattern = new Regex(@"(?'year'^\d{4})(?'month'\d{2})(?'day'\d{2})_(?'hh'\d{2})(?'mm'\d{2})(?'ss'\d{2})");
                var match = rPattern.Match(inputName);
                string newName = string.Format("{0}-{1}-{2} {3}.{4}.{5}",
                    match.Groups["year"],
                    match.Groups["month"],
                    match.Groups["day"],
                    match.Groups["hh"],
                    match.Groups["mm"],
                    match.Groups["ss"]);
            }
            #endregion


            if (args.Length < 2)
            {
                Console.WriteLine("Too few arguments.");
                Console.WriteLine("Recommmended syntax:");
                Console.WriteLine("PhotoFileRenamer <dir> <NewNameFormat> [<SearchPattern>]");
                return;
            }
            string dir = args[0]; //Directory to process
            string nameCore = args[1]; //How would you like your files to be named
            //Number part length will be computed automatically based on the number of pictures being renamed.
            string pattern = "*.jpg"; //Default pattern
            //Handle optional pattern
            if (args.Length > 2)
            {
                pattern = args[2];
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

            int numPart = files.Length.ToString().Length;
            Console.WriteLine("Num part will take {0} characters.", numPart);

            //Dictionary of <original name>, <new name>
            Dictionary<string, string> renames = new Dictionary<string, string>();
            int pictureIndex = 1; //Index names from 1
            //Need to escape it in a funny way
            string nameFormat = string.Format("{0} {2}{1}{3}{4}", nameCore, numPart, "{0:D", "}", "{1}");
            Console.WriteLine("Files will be renamed using this pattern: {0}", nameFormat);
            foreach (FileInfo file in files)
            {
                string newName = string.Format(nameFormat, pictureIndex, file.Extension.ToLower());
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
            XmlNode rootXmlNode = renameLog.CreateNode(XmlNodeType.Element, "PhotoFileRenamer", myNamespace);
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
            string renameLogFile = Path.Combine(dir, "PhotoFileRenamer.xml");
            renameLog.Save(renameLogFile);
        }
    }
}
