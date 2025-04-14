using IISLogParser;
using UAParser;

namespace iislogsanalyser
{
    static class Program
    {
        private static readonly Parser AgentParser = Parser.GetDefault();

        static int Main(string[] args)
        {
            var (status, source, destination) = ParseInput(args);

            if (status != 0)
                return status;

            using var stream = File.CreateText(destination);
            stream.WriteLine("Timestamp,OS Family,OS,Browser Family,Browser,Request,Method,Status,Duration");
            foreach (var file in Directory.EnumerateFiles(source, "*.log").OrderBy(x => x))
            {
                Console.WriteLine($"Parsing ${file} ...");
                ParseContentsInto(stream, file);
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
            using var parser = new ParserEngine(filePath);
            while (parser.MissingRecords)
            {
                foreach(var log in parser.ParseLog())
                {
                    try
                    {
                        var ((osFamily, os), (browserFamily, browser)) = GetUserAgent(log.csUserAgent);
                        stream.WriteLine($"{log.DateTimeEvent:yyyy-MM-dd HH:mm:ss},{osFamily},{os},{browserFamily},{browser},{log.csUriStem},{log.csMethod},{log.scStatus},{log.timeTaken}");
                    }
                    catch (Exception e)
                    {
                        Show($"Error - {e.Message}");
                    }
                }
            }
        }

        private static void ShowHelp()
        {
            const string help =
@"iisloganalyser SOURCE_FOLDER DESTINATION_PATH

  Where:
    SOURCE_FOLDER       Folder containing one or more log files (e.g. u_ex181201.log)
    DESTINATION_PATH    CSV File to write parsed logs to. (e.g. output.csv).
                        Must not exist.
";
            Show(help);
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

        private static (string family, string os) GetOS(OS os) =>
            os.Family switch
            {
                "Windows" => os.Major switch
                {
                    "XP" => (os.Family, "Windows XP"),
                    "Vista" => (os.Family, "Windows Vista"),
                    "8" => (os.Family, $"{os.Family} {os.Major}.{os.Minor}"),
                    _ => (os.Family, $"{os.Family} {os.Major}")
                },
                "Mac OS X" => (os.Family, $"{os.Family} {os.Major}.{os.Minor}"),
                "Ubuntu" or "Windows NT 4.0" or "Other" => (os.Family, os.Family),
                _ => (os.Family, $"{os.Family} {os.Major}")
            };

        private static (string family, string browser) GetBrowser(UserAgent ua) =>
            ua.Family switch
            {
                "Other" => ("Other", "Other"),
                _ => (ua.Family, $"{ua.Family} {ua.Major}")
            };
    }
}
