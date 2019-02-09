using System;
using System.Collections.Generic;
using IISLogParser;
using UAParser;
using System.Linq;
using System.IO;

namespace iislogsanalyser
{
    class Program
    {
        private static Parser AgentParser = Parser.GetDefault();

        static int Main(string[] args)
        {
            var (status, source, destination) = ParseInput(args);

            if (status != 0)
                return status;

            using (var stream = File.CreateText(destination))
            {
                stream.WriteLine("Timestamp,OS Family,OS,Browser Family,Browser,Request,Method,Status,Duration");
                foreach (var file in Directory.EnumerateFiles(source, "*.log").OrderBy(x => x))
                {
                    Console.WriteLine($"Parsing ${file} ...");
                    ParseContentsInto(stream, file);
                }
            }

            return 0;
        }

        private static (int status, string source, string destination) ParseInput(string[] args)
        {
            if (args.Length != 2)
            {
                ShowHelp();
                return (1, "", "");
            }

            if (!Directory.Exists(args[0]))
            {
                Show("Source folder does not exist");
                return (1, "", "");
            }

            if (!Directory.EnumerateFiles(args[0], "*.log").Any())
            {
                Show("Source folder does not contain any log files");
                return (1, "", "");
            }

            if (File.Exists(args[1]))
            {
                Show("Destination path already exists");
                return (1, "", "");
            }

            return (0, args[0], args[1]);
        }

        private static void ParseContentsInto(StreamWriter stream, string filePath)
        {
            using (var parser = new ParserEngine(filePath))
            {
                while (parser.MissingRecords)
                {
                    foreach(var log in parser.ParseLog())
                    {
                        try
                        {
                            var ((osFamily, os), (browserFamily, browser)) = GetUserAgent(log.csUserAgent);
                            stream.WriteLine($"{log.DateTimeEvent.ToString("yyyy-MM-dd HH:mm:ss")},{osFamily},{os},{browserFamily},{browser},{log.csUriStem},{log.csMethod},{log.scStatus},{log.timeTaken}");
                        }
                        catch (Exception e)
                        {
                            Show($"Error - {e.Message}");
                        }
                    }
                }
            }
        }

        private static void ShowHelp()
        {
            const string Help =
@"iisloganalyser SOURCE_FOLDER DESTINATION_PATH

  Where:
    SOURCE_FOLDER       Folder containing one or more log files (e.g. u_ex181201.log)
    DESTINATION_PATH    CSV File to write parsed logs to. (e.g. output.csv).
                        Must not exist.
";
            Show(Help);
        }

        private static void Show(string msg)
        {
            Console.WriteLine(msg);
        }

        private static ((string osFamily, string os), (string browserFamily, string browser)) GetUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return (("Uknown", "Unknown"), ("Uknown", "Unknown"));
            var clientInfo = AgentParser.Parse(userAgent.Replace('+', ' '));
            return (GetOS(clientInfo.OS), GetBrowser(clientInfo.UA));
        }

        private static (string family, string os) GetOS(OS os)
        {
            switch (os.Family)
            {
                case "Windows":
                    switch (os.Major)
                    {
                        case "XP":
                            return (os.Family, "Windows XP");
                        case "Vista":
                            return (os.Family, "Windows Vista");
                        case "8":
                            return (os.Family, $"{os.Family} {os.Major}.{os.Minor}");
                        default:
                            return (os.Family, $"{os.Family} {os.Major}");
                    }
                case "Mac OS X":
                    return (os.Family, $"{os.Family} {os.Major}.{os.Minor}");
                case "Ubuntu":
                case "Windows NT 4.0":
                case "Other":
                    return (os.Family, os.Family);
                default:
                    return (os.Family, $"{os.Family} {os.Major}");
            }
        }

        private static (string family, string browser) GetBrowser(UserAgent ua)
        {
            switch (ua.Family)
            {
                case "Other":
                    return ("Other", "Other");
                default:
                    return (ua.Family, $"{ua.Family} {ua.Major}");
            }
        }
    }
}
